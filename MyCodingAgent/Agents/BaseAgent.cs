using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text.Json;

namespace MyCodingAgent.Agents;

public abstract class BaseAgent(Workspace Workspace, OllamaClient Client)
{
    protected abstract List<PromptResponseResults> History { get; }
    protected abstract IToolCall[] Tools { get; }

    protected Workspace Workspace { get; } = Workspace;
    protected OllamaClient Client { get; } = Client;

    protected static void AddHistoryAndToolCalls(List<OllamaMessage> messageList, List<PromptResponseResults> history, Tool[] tools, int maxTokens, int additionalSizeInBytes)
    {
        var notNullHistory = history
            .Where(a =>
                string.IsNullOrWhiteSpace(a.Response.message.content) == false ||
                (a.Response.message.tool_calls != null && a.Response.message.tool_calls.Any()))
            .ToList();

        var messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var messagesJsonLength = messagesJson.Length;
        var toolsJson = OllamaClient.CreateToolsJson(tools);
        var toolsJsonLength = toolsJson.Length;
        var maxHistory = 0;
        int maxLongDesciptionPrompt = 0;
        var totalLength = messagesJsonLength + toolsJsonLength + additionalSizeInBytes;

        var useShortContent = false;

        HashSet<CacheMessage> shownMessages = [];

        foreach (var responseResult in notNullHistory.ToArray().Reverse())
        {
            var response = CleanMessage(responseResult.Response.message);

            var responseJson = JsonSerializer.Serialize(response, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
            totalLength += responseJson.Length;

            // TOOL CALLS REPLIES
            if (responseResult.ToolCallResults.Any())
            {
                foreach (var toolCall in responseResult.ToolCallResults)
                {
                    var cacheMessage = new CacheMessage(
                        toolCall.tool_call.function.name,
                        toolCall.tool_call.function.arguments.id,
                        toolCall.tool_call.function.arguments.action,
                        toolCall.tool_call.function.arguments.path,
                        toolCall.tool_call.function.arguments.newPath,
                        toolCall.tool_call.function.arguments.query,
                        toolCall.tool_call.function.arguments.content,
                        toolCall.tool_call.function.arguments.replaceText,
                        toolCall.tool_call.function.arguments.lineNumber);
                    if (!shownMessages.Add(cacheMessage)) // Todo, als het model ooit meerdere actions gaat uitvoeren
                    {
                        notNullHistory.Remove(responseResult);
                    }

                    var message = CreateToolCallbackMessage(useShortContent, toolCall);
                    var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                    totalLength += messageJson.Length;
                }
            }
            else
            {
                var message = CreateToolCallbackMessage(useShortContent, null);
                var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                totalLength += messageJson.Length;
            }

            if (totalLength < maxTokens * 3)
                maxLongDesciptionPrompt++;
            else
            {
                useShortContent = true;
            }

            if (totalLength < maxTokens * 3)
                maxHistory++;
            else
            {
                break;
            }
        }

        var i = notNullHistory.Count; // Dan terug tellen
        foreach (var responseResult in notNullHistory)
        {
            if (i > maxHistory)
            {
                i--;
                continue;
            }

            // AGENT RESPONSE 
            messageList.Add(CleanMessage(responseResult.Response.message));

            // TOOL CALLS REPLIES
            if (responseResult.ToolCallResults.Any())
            {
                foreach (var toolCall in responseResult.ToolCallResults)
                {
                    messageList.Add(CreateToolCallbackMessage(i > maxLongDesciptionPrompt, toolCall));
                }
            }
            else
            {
                messageList.Add(
                    CreateToolCallbackMessage(i > maxLongDesciptionPrompt, null));
            }

            i--;
        }

        messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        messagesJsonLength = messagesJson.Length;
        totalLength = messagesJsonLength + toolsJsonLength + additionalSizeInBytes;

    }

    private static OllamaMessage CleanMessage(OllamaMessage message)
    {
        var content = "Use tool_calls";
        if (message.tool_calls?.Any() == true)
        {
            content = string.Join(", ", message.tool_calls.Select(a => a.id));
        }
        if (!string.IsNullOrWhiteSpace(message.content))
        {
            content = message.content;
        }

        return new OllamaMessage(
            message.role,
            message.tool_call_id,
            content,
            null,
            message.tool_calls);
    }

    private static OllamaMessage CreateToolCallbackMessage(bool useShortContent, ToolCallResult? toolCall)
    {
        return new OllamaMessage(
            nameof(OllamaAgentRole.Tool).ToLower(),
            toolCall?.tool_call.id,
            toolCall == null ? "Error: no tool_calls found" : useShortContent ? toolCall.result.shortContent : toolCall.result.content,
            null,
            null);
    }

    protected async Task<PromptResponseResults> GetAgentResponseResult(OllamaPrompt prompt, OllamaResponse response, IToolCall[] tools)
    {
        var list = new List<ToolCallResult>();
        if (response.message.tool_calls != null)
        {
            foreach (var tool_call in response.message.tool_calls)
            {
                var toolName = tool_call.function.name;
                var toolArguments = tool_call.function.arguments;

                var tool = tools.FirstOrDefault(a => a.Name == toolName);
                if (tool == null)
                {
                    list.Add(new ToolCallResult(
                        tool_call,
                        new ToolResult(
                            $"Could not find tool '{toolName}'",
                            $"Could not find tool",
                            true)));
                    continue;
                }
                else
                {
                    var toolResult = await tool.Invoke(tool_call);
                    list.Add(new ToolCallResult(
                        tool_call,
                        toolResult));
                }
            }
        }
        return new PromptResponseResults(
            prompt,
            response,
            [.. list]);
    }
}
