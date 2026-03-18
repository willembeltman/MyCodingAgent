namespace MyCodingAgent.OllamaClient;

public record OllamaToolCallFunction(
    //int? index,
    string name,
    OllamaToolCallFunctionArguments arguments);