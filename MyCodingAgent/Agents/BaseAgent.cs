using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using System.Text.Json;

namespace MyCodingAgent.Agents;

public abstract class BaseAgent(Current Current)
{
    protected abstract IEnumerable<WorkspaceEvent> History { get; }
    protected abstract IToolCall[] Tools { get; }

    protected Current Current { get; } = Current;
    protected IClient CurrentClient => Current.Client;
    protected Model CurrentModel => Current.Model;
    protected Workspace CurrentWorkspace => Current.Workspace;
    protected WorkspaceTask CurrentTask => Current.Task;
    protected WorkspaceSubTask? CurrentSubTask => Current.SubTask;

    protected void AddHistoryToMessageList(List<Message> messageList, Tool[] tools, int additionalSizeInBytes = 0)
    {
        var notNullHistory = History
            .Where(a =>
                string.IsNullOrWhiteSpace(a.Response?.message.Content) == false ||
                (a.Response?.message.ToolCalls != null && a.Response.message.ToolCalls.Length > 0))
            .ToList();

        var messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var messagesJsonLength = messagesJson.Length;
        var toolsJson = CurrentClient.CreateToolsJson(tools);
        var toolsJsonLength = toolsJson.Length;
        var maxHistory = 0;
        int maxLongDesciptionPrompt = 0;
        var totalLength = messagesJsonLength + toolsJsonLength + additionalSizeInBytes;

        var useShortContent = false;

        HashSet<CacheMessage> shownMessages = [];

        foreach (var e in notNullHistory.ToArray().Reverse())
        {
            var response = CleanMessage(e.Response.message);

            var responseJson = JsonSerializer.Serialize(response, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
            totalLength += responseJson.Length;

            // TOOL CALLS REPLIES
            if (e.ToolCallResults.Length > 0)
            {
                foreach (var toolCall in e.ToolCallResults)
                {
                    var cacheMessage = new CacheMessage(
                        toolCall.ToolCall.Function.Name,
                        toolCall.ToolCall.Function.Arguments.Id,
                        toolCall.ToolCall.Function.Arguments.Action,
                        toolCall.ToolCall.Function.Arguments.Path,
                        toolCall.ToolCall.Function.Arguments.NewPath,
                        toolCall.ToolCall.Function.Arguments.Query,
                        toolCall.ToolCall.Function.Arguments.Content,
                        toolCall.ToolCall.Function.Arguments.LineNumber);
                    if (!shownMessages.Add(cacheMessage)) // Todo, als het model ooit meerdere actions gaat uitvoeren
                    {
                        notNullHistory.Remove(e);
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

            if (totalLength < (CurrentModel.MaxTokenSize ?? 4096) * 3)
                maxLongDesciptionPrompt++;
            else
            {
                useShortContent = true;
            }

            if (totalLength < (CurrentModel.MaxTokenSize ?? 4096) * 4)
                maxHistory++;
            else
            {
                break;
            }
        }

        var i = notNullHistory.Count; // Dan terug tellen
        foreach (var e in notNullHistory)
        {
            if (i > maxHistory)
            {
                i--;
                continue;
            }

            // AGENT RESPONSE 
            messageList.Add(CleanMessage(e.Response.message));

            // TOOL CALLS REPLIES
            if (e.ToolCallResults.Length > 0)
            {
                foreach (var toolCall in e.ToolCallResults)
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
            toolCall?.ToolCall.Id,
            toolCall == null ? "Error: no tool_calls found" : useShortContent ? toolCall.Result.ShortContent : toolCall.Result.Content,
            null,
            null);
    }

    public async Task<ToolCallResult[]> ProcessResponse(LlmRequest request, LlmResponse response)
    {
        var toolCallResults = new List<ToolCallResult>();
        if (response.message.ToolCalls != null)
        {
            foreach (var tool_call in response.message.ToolCalls)
            {
                var toolName = tool_call.Function.Name;
                var toolArguments = tool_call.Function.Arguments;

                var tool = Tools.FirstOrDefault(a => a.Name == toolName);
                if (tool == null)
                {
                    toolCallResults.Add(new ToolCallResult(
                        tool_call,
                        new ToolResult(
                            $"Could not find tool '{toolName}'",
                            $"Could not find tool",
                            null,
                            true)));
                    continue;
                }
                else
                {
                    var toolResult = await tool.Invoke(tool_call);
                    toolCallResults.Add(new ToolCallResult(
                        tool_call,
                        toolResult));
                }
            }
        }

        return [.. toolCallResults];
    }
}
