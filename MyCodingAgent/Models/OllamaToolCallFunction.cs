namespace MyCodingAgent.Models;

public record OllamaToolCallFunction(
    //int? index,
    string name,
    OllamaToolCallFunctionArguments arguments);