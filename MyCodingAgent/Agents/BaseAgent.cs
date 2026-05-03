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
        var messagesJson = JsonSerializer.Serialize(messageList, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var messagesJsonLength = messagesJson.Length;
        var toolsJson = CurrentClient.CreateToolsJson(tools);
        var toolsJsonLength = toolsJson.Length;
        var totalLength = messagesJsonLength + toolsJsonLength + additionalSizeInBytes;

        var useShortContent = false;

        HashSet<CacheMessage> shownMessages = [];
        List<Message> tempMessages = new List<Message>();

        foreach (var workspaceEvent in History.ToArray().Reverse())
        {
            if (workspaceEvent.Response == null) continue;

            // TOOL CALLS REPLIES
            if (workspaceEvent.Result != null && 
                workspaceEvent.Result.Events.Length > 0 &&
                workspaceEvent.Result.Events.Any(a => a.tool_call != null))
            {
                foreach (var toolCall in workspaceEvent.Result.Events)
                {
                    var call = toolCall.tool_call!; // Staat al in de if
                    var cacheMessage = new CacheMessage(
                        call.Function.Name,
                        call.Function.Arguments.Id,
                        call.Function.Arguments.Action,
                        call.Function.Arguments.Path,
                        call.Function.Arguments.NewPath,
                        call.Function.Arguments.Query,
                        call.Function.Arguments.Content,
                        call.Function.Arguments.LineNumber);
                    if (!shownMessages.Add(cacheMessage))
                    {
                        continue;
                    }

                    var message = CreateToolCallbackMessage(useShortContent, toolCall);
                    tempMessages.Add(message);
                    var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                    totalLength += messageJson.Length;
                }
            }
            else
            {
                var message = CreateToolCallbackMessage(false, null); // No toolcall message
                tempMessages.Add(message);  
                var messageJson = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
                totalLength += messageJson.Length;
            }

            // Dan achteraf de response van de llm (die wordt zo omgedraait)
            var responseMessage = CleanMessage(workspaceEvent.Response.Message);
            tempMessages.Add(responseMessage);
            var responseMessageJson = JsonSerializer.Serialize(responseMessage, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
            totalLength += responseMessageJson.Length;

            if (totalLength > (CurrentModel.MaxTokenSize ?? 4096) * 3)
                useShortContent = true;

            if (totalLength > (CurrentModel.MaxTokenSize ?? 4096) * 4)
                break;
        }

        tempMessages.Reverse();
        messageList.AddRange(tempMessages);
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

    private static Message CreateToolCallbackMessage(bool useShortContent, SystemResultEvent? toolCall)
    {
        return new Message(
            nameof(AgentRole.Tool).ToLower(),
            toolCall?.tool_call?.Id,
            toolCall == null ? "Error: no tool_calls found" : useShortContent ? toolCall.result.ShortContent : toolCall.result.Content,
            null,
            null);
    }

    public async Task<SystemResult> ProcessResponse(LlmRequest request, LlmResponse response)
    {
        var toolCallResults = new List<SystemResultEvent>();
        if (response.Message.ToolCalls != null)
        {
            foreach (var tool_call in response.Message.ToolCalls)
            {
                var toolName = tool_call.Function.Name;
                var toolArguments = tool_call.Function.Arguments;

                var tool = Tools.FirstOrDefault(a => a.Name == toolName);
                if (tool == null)
                {
                    toolCallResults.Add(new SystemResultEvent(
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

                    if (toolResult.Flags.PlanningIsDoneFlag)
                        Current.Task.Flags.PlanningIsDoneFlag = true;
                    if (toolResult.Flags.DebuggingIsDoneFlag)
                        Current.Task.Flags.IsDebuggingFlag = false;
                    if (toolResult.Flags.TaskIsDoneFlag)
                        Current.Task.Flags.TaskIsDoneFlag = true;

                    toolCallResults.Add(new SystemResultEvent(
                        tool_call,
                        toolResult));
                }
            }
        }
        return new SystemResult([.. toolCallResults]);
    }
}
