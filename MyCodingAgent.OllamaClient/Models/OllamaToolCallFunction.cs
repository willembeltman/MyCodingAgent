namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaToolCallFunction(
    //int? index,
    string name,
    OllamaToolCallFunctionArguments arguments);