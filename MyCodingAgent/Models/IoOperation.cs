using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public record IoOperation(
    IoOperationType Type,
    string? Path,
    string? NewPath,
    string? Query,
    string? Content,
    int? LineNumber);
