using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Shared.Interfaces;

public interface ILlmClient : IDisposable
{
    Task<Response> ChatAsync(Model model, Prompt prompt, CancellationToken ct = default);
    Task<Model[]> GetModels(CancellationToken ct = default);
    Task InitializeModelAsync(Model model, CancellationToken ct = default);
    Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default);
    string CreateToolsJson(Tool[] tools);
    string CreateMessagesJson(Message[] messages);
}