using MyCodingAgent.Enums;
using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class WorkspaceEvent
{
    [Required]
    public int Id { get; init; }

    [Required]
    public Actor Actor { get; init; } = default!;

    [Required]
    // Welke gespreks-geschiedenis valt het event onder?
    public Conversation Conversation { get; init; } 

    [Required]
    public long TaskId { get; init; }

    [Required]
    // Null bij planning, codereview en system events (bij starten task)
    public long? SubTaskId { get; init; }

    [Required]
    public CompileResult CompileResult { get; init; } = default!;

    public DateTime TimeStamp { get; init; } = DateTime.Now;
    public WorkspaceEventFlags Flags { get; init; } = new WorkspaceEventFlags();

    // System message, de (gegenereerde) chat geschiedenis en de beschikbare tools zoals gestuurd naar LLM
    public LlmRequest? Request { get; set; }

    // Response van de LLM met tool calls
    public LlmResponse? Response { get; set; }

    public ToolCallResult[]? ToolCallResults { get; set; }
}