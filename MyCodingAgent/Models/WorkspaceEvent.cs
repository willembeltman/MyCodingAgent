using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public record WorkspaceEvent(
    int Id,
    Actor Actor,
    Conversation Conversation,  // Welke gespreks-geschiedenis valt het event onder?
    long TaskId,
    long? SubTaskId)            // Null bij planning, codereview en system events (bij starten task)
{
    public DateTime TimeStamp { get; init; } = DateTime.Now;

    // Compile result voorgaande aan de request
    public CompileResult? CompileResult { get; init; }

    // System message, de (gegenereerde) chat geschiedenis en de beschikbare tools zoals gestuurd naar LLM
    public LlmRequest? Request { get; set; }

    // Response van de LLM met tool calls
    public LlmResponse? Response { get; set; }

    // Response van SYSTEM op Request en Response van LLM
    public SystemResult? Result { get; set; }
}