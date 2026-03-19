using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IClient : IDisposable
{
    Task<Response> ChatAsync(Model model, Prompt prompt, CancellationToken ct = default);
    Task<Model[]> GetModels(CancellationToken ct = default);
    Task InitializeModelAsync(Model model, CancellationToken ct = default);
    Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default);
    string CreateToolsJson(Tool[] tools);
    string CreateMessagesJson(Message[] messages);
    string CreateRequestJson(Model model, Prompt prompt);
}