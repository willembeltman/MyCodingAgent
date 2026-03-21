using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using System.Text.Json;

namespace MyCodingAgent;

public abstract class BaseAgent(IClient Client, Workspace Workspace, Model model)
{
    protected abstract List<ResponseResults> History { get; }
    protected abstract IToolCall[] Tools { get; }

    protected IClient Client { get; } = Client;
    protected Workspace Workspace { get; } = Workspace;
    protected Model Model { get; } = model;

    protected void AddHistoryAndToolCalls(List<Message> messageList, List<ResponseResults> history, Tool[] tools, int additionalSizeInBytes)
    {
        var notNullHistory = history
            .Where(a =>
                string.IsNullOrWhiteSpace(a.Response.message.Content) == false ||
                (a.Response.message.ToolCalls != null && a.Response.message.ToolCalls.Length > 0))
            .ToList();

        var messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var messagesJsonLength = messagesJson.Length;
        var toolsJson = Client.CreateToolsJson(tools);
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
            if (responseResult.ToolCallResults.Count > 0)
            {
                foreach (var toolCall in responseResult.ToolCallResults)
                {
                    var cacheMessage = new CacheMessage(
                        toolCall.tool_call.Function.Name,
                        toolCall.tool_call.Function.Arguments.Id,
                        toolCall.tool_call.Function.Arguments.Action,
                        toolCall.tool_call.Function.Arguments.Path,
                        toolCall.tool_call.Function.Arguments.NewPath,
                        toolCall.tool_call.Function.Arguments.Query,
                        toolCall.tool_call.Function.Arguments.Content,
                        toolCall.tool_call.Function.Arguments.ReplaceText,
                        toolCall.tool_call.Function.Arguments.LineNumber);
                    if (!shownMessages.Add(cacheMessage)) // Todo, als het model ooit meerdere actions gaat uitvoeren
                    {
                        notNullHistory.Remove(responseResult);
                        continue;
                    }

                    if (useShortContent) { }
                    var message = CreateToolCallbackMessage(false, toolCall);
                    var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                    totalLength += messageJson.Length;
                }
            }
            else
            {
                var message = CreateToolCallbackMessage(false, null);
                var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                totalLength += messageJson.Length;
            }

            if (totalLength < (Model.MaxTokenSize ?? 4096) * 5 / 2)
                maxLongDesciptionPrompt++;
            else
            {
                useShortContent = true;
            }

            if (totalLength < (Model.MaxTokenSize ?? 4096) * 7 / 2)
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
            if (responseResult.ToolCallResults.Count > 0)
            {
                foreach (var toolCall in responseResult.ToolCallResults)
                {
                    messageList.Add(CreateToolCallbackMessage(false, toolCall));// i > maxLongDesciptionPrompt, toolCall));
                }
            }
            else
            {
                messageList.Add(
                    CreateToolCallbackMessage(false, null));//i > maxLongDesciptionPrompt, null));
            }

            i--;
        }

        messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        messagesJsonLength = messagesJson.Length;
        totalLength = messagesJsonLength + toolsJsonLength + additionalSizeInBytes;

    }

    private static Message CleanMessage(Message message)
    {
        var content = "Use tool_calls";
        if (message.ToolCalls?.Length > 0 == true)
        {
            content = string.Join(", ", message.ToolCalls.Select(a => a.Id));
        }
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            content = message.Content;
        }
        var toolCalls = (ToolCall[]?)null;
        if (message.ToolCalls != null)
        {
            toolCalls =
            [
                .. message.ToolCalls.Select(a =>
                    new ToolCall(
                        a.Id,
                        new ToolCallFunction(
                            a.Function.Name,
                            new ToolCallFunctionArguments()
                            {
                                Action = a.Function.Arguments.Action,
                                Id = a.Function.Arguments.Id,
                                LineNumber = a.Function.Arguments.LineNumber,
                                NewPath = a.Function.Arguments.NewPath,
                                Path = a.Function.Arguments.Path,
                                Query = a.Function.Arguments.Query,
                                //replaceText = a.function.arguments.replaceText,
                                //content = a.function.arguments.content
                            })))
            ];
        }


        return new Message(
            message.Role,
            null,
            content,
            null,
            toolCalls);
    }

    private static Message CreateToolCallbackMessage(bool useShortContent, ToolCallResult? toolCall)
    {
        return new Message(
            nameof(AgentRole.Tool).ToLower(),
            toolCall?.tool_call.Id,
            toolCall == null ? "Error: no tool_calls found" : useShortContent ? toolCall.result.shortContent : toolCall.result.content,
            null,
            null);
    }

    public async Task<ResponseResults> ProcessResponse(ApiCall apiCall, Response response)
    {
        var list = new List<ToolCallResult>();
        if (response.message.ToolCalls != null)
        {
            foreach (var tool_call in response.message.ToolCalls)
            {
                var toolName = tool_call.Function.Name;
                var toolArguments = tool_call.Function.Arguments;

                var tool = Tools.FirstOrDefault(a => a.Name == toolName);
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
        var results = new ResponseResults(
            apiCall,
            response,
            [.. list]);

        History.Add(results);
        return results;
    }
}
