using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.Clients;

// TODO Tools anders regelen, structure in system message uitleggen.
public class LlamaCpp_Client : IDisposable, IClient
{
    private readonly HttpClient HttpClient;
    private readonly string BaseUrl;
    private readonly Dictionary<Language, Dictionary<string, string>> Dictionaries = [];

    public LlamaCpp_Client(string baseUrl = "http://localhost:8080", HttpClient? httpClient = null)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        HttpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromHours(1) };
    }

    public Task<Model[]> GetModels(CancellationToken ct = default)
    {
        // llama.cpp heeft geen echte model listing
        return Task.FromResult(new[]
        {
            new Model("local-llama", 0, 8192, DateTime.UtcNow)
        });
    }

    public Task InitializeModelAsync(Model model, CancellationToken ct = default)
    {
        // model wordt geladen bij server start
        return Task.CompletedTask;
    }

    public async Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default)
    {
        if (!Dictionaries.TryGetValue(toLanguage, out var dictionary))
        {
            dictionary = [];
            Dictionaries[toLanguage] = dictionary;
        }

        if (!overwrite && dictionary.TryGetValue(content, out var translation))
            return translation;

        var messages = new[]
        {
            new { role = "system", content = $"Translate everything to {Enum.GetName(toLanguage)}. Only return the translated text." },
            new { role = "user", content }
        };

        var payload = new
        {
            model = model.Name,
            messages,
            max_tokens = 8192,
            temperature = 0.2
        };

        var json = await DoCall(payload, ct);

        var doc = JsonDocument.Parse(json);
        var translated = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new Exception("No content returned");

        dictionary[content] = translated;
        return translated;
    }

    public async Task<Response> ChatAsync(Model model, Prompt prompt, CancellationToken ct = default)
    {
        var payload = new
        {
            model = model.Name,
            messages = prompt.messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }),
            tools = prompt.tools.Select(tool => new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Desciption,
                    parameters = new
                    {
                        type = "object",
                        properties = tool.Parameters.ToDictionary(p => p.Name, p => new
                        {
                            type = p.Type,
                            description = p.Description,
                            @enum = p.Enum
                        }),
                        required = tool.Parameters.Where(p => !p.Optional).Select(p => p.Name).ToArray()
                    }
                }
            }),
            tool_choice = "auto"
        };

        var json = await DoCall(payload, ct);
        var doc = JsonDocument.Parse(json);

        var msg = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        string? content = msg.TryGetProperty("content", out var c) ? c.GetString() : null;

        ToolCall[]? toolCalls = null;

        // llama.cpp gebruikt vaak "tool_calls" i.p.v. function_call
        if (msg.TryGetProperty("tool_calls", out var tcArray))
        {
            toolCalls = tcArray.EnumerateArray().Select(tc => new ToolCall(
                tc.GetProperty("id").GetString()!,
                new ToolCallFunction(
                    tc.GetProperty("function").GetProperty("name").GetString()!,
                    JsonSerializer.Deserialize<ToolCallFunctionArguments>(
                        tc.GetProperty("function").GetProperty("arguments").GetRawText()
                    )!
                )
            )).ToArray();
        }

        return new Response(
            model.Name,
            DateTime.UtcNow,
            new Message(
                "assistant",
                toolCalls?.FirstOrDefault()?.Id,
                content,
                null,
                toolCalls
            )
        );
    }

    private async Task<string> DoCall(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);

        var response = await HttpClient.PostAsync(
            $"{BaseUrl}/v1/chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json"),
            ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // deze kun je exact hergebruiken
    public string CreateMessagesJson(Message[] messages)
    {
        return JsonSerializer.Serialize(messages, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }

    public string CreateToolsJson(Tool[] tools)
    {
        return JsonSerializer.Serialize(tools, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }

    public string CreateRequestJson(Model model, Prompt prompt)
    {
        var payload = new
        {
            model = model.Name,
            messages = prompt.messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = 0.1,
            top_p = 0.9,
            repeat_penalty = 1.1,
            max_tokens = 8192,
            tools = prompt.tools
        };

        return JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }
}
