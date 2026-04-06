using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IClient : IDisposable
{
    Task<LlmResponse> ChatAsync(Model model, LlmRequest apiCall, CancellationToken ct = default);
    Task<Model[]> GetModels(CancellationToken ct = default);
    Task InitializeModelAsync(Model model, CancellationToken ct = default);
    Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default);
    string CreateToolsJson(Tool[] tools);
    string CreateMessagesJson(Message[] messages);
    string CreateRequestJson(Model model, LlmRequest apiCall);
}