using MyCodingAgent.Shared.Helpers;
using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Shared.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.OpenAiClient;

public class ChatGpt_Client : IDisposable, ILlmClient
{
    private readonly HttpClient HttpClient;
    private readonly string OpenAiApiKey;
    private readonly Dictionary<Language, Dictionary<string, string>> Dictionaries = new();

    public ChatGpt_Client(string apiKey, HttpClient? httpClient = null)
    {
        OpenAiApiKey = apiKey;
        HttpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(3600) };
    }

    public async Task<Model[]> GetModels(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OpenAiApiKey);

        var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        var models = doc.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => new Model(
                m.GetProperty("id").GetString() ?? "",
                0,
                4096,
                DateTime.UtcNow))
            .ToArray();

        return models;
    }

    public async Task InitializeModelAsync(Model model, CancellationToken ct = default)
    {
        // OpenAI-modellen hoeven niet gepulld te worden
        await Task.CompletedTask;
    }

    public async Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default)
    {
        if (!Dictionaries.TryGetValue(toLanguage, out var dictionary))
        {
            dictionary = new();
            Dictionaries[toLanguage] = dictionary;
        }

        if (!overwrite && dictionary.TryGetValue(content, out var translation))
            return translation;

        var messages = new[]
        {
            new { role = "system", content = $"You are a translator. Translate everything to {Enum.GetName(toLanguage)}. Only return the translated text." },
            new { role = "user", content }
        };

        var payload = new
        {
            model = model.Name,
            messages,
            max_tokens = 8192
        };

        var payloadJson = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var json = await DoCall(payloadJson, ct);
        var doc = JsonDocument.Parse(json);
        var translated = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (translated == null) throw new Exception("No content returned from OpenAI");

        dictionary[content] = translated;
        return translated;
    }

    public async Task<Response> ChatAsync(Model model, Prompt prompt, CancellationToken ct = default)
    {
        string payloadJson = CreateRequestJson(model, prompt);

        var responseJson = await DoCall(payloadJson, ct);
        var doc = JsonDocument.Parse(responseJson);

        var choice = doc.RootElement.GetProperty("choices")[0];
        var msg = choice.GetProperty("message");

        string? content = msg.TryGetProperty("content", out var c) ? c.GetString() : null;
        ToolCall[]? toolCalls = null;

        if (msg.TryGetProperty("function_call", out var fc))
        {
            toolCalls = new[]
            {
                new ToolCall(
                    Guid.NewGuid().ToString(), // unique id
                    new ToolCallFunction(
                        fc.GetProperty("name").GetString()!,
                        JsonSerializer.Deserialize<ToolCallFunctionArguments>(
                            fc.GetProperty("arguments").GetRawText()
                        )!
                    )
                )
            };
        }

        return new Response(
            model.Name,
            DateTime.UtcNow,
            new Message(
                "assistant",
                toolCalls?.FirstOrDefault()?.id,
                content,
                null,
                toolCalls
            )
        );
    }


    private async Task<string> DoCall(string payloadJson, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(
                payloadJson,
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OpenAiApiKey);

        var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public string CreateMessagesJson(Message[] messages)
    {
        var ollamaMessages = messages.Select(m => new
        {
            role = m.role,
            tool_call_id = m.tool_call_id,
            content = m.content,
            thinking = m.thinking,
            tool_calls = m.tool_calls?.Select(tc => new
            {
                id = tc.id,
                function = new
                {
                    name = tc.function.name,
                    arguments = tc.function.arguments
                }
            }).ToArray()
        }).ToArray();

        return JsonSerializer.Serialize(
            ollamaMessages,
            DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }
    public string CreateToolsJson(Tool[] tools)
    {
        var ollamaTools = tools.Select(tool => new
        {
            Name = tool.Name,
            Description = tool.Desciption,
            Parameters = tool.Parameters.Select(p => new
            {
                Name = p.Name,
                Type = p.Type,
                Description = p.Description,
                Enum = p.Enum,
                Optional = p.Optional
            }).ToArray()
        }).ToArray();

        return JsonSerializer.Serialize(
            ollamaTools,
            DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }

    public string CreateRequestJson(Model model, Prompt prompt)
    {
        var functions = prompt.tools.Select(tool => new
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
        }).ToArray();

        var payload = new
        {
            model = model.Name,
            messages = prompt.messages.Select(m => new { role = m.role, content = m.content }).ToArray(),
            functions
        };

        var payloadJson = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        return payloadJson;
    }
}

internal record OllamaMessage(
    string role,
    string? tool_call_id,
    string? content,
    string? thinking,
    OllamaToolCall[]? tool_calls);

internal record OllamaModelRaw(
    string? name,
    long? size,
    string? digest,
    DateTime? modified_at);

internal record OllamaModelRawCollection(
    OllamaModelRaw[]? models);

internal record OllamaPrompt(
    OllamaMessage[] messages,
    OllamaTool[] tools);

internal record OllamaResponse(
    string model,
    DateTime created_at,
    OllamaMessage message);

internal record OllamaTool(
    string Name,
    string Desciption,
    OllamaToolParameter[] Parameters);

internal record OllamaToolCall(
    string id,
    OllamaToolCallFunction function);

internal record OllamaToolCallFunction(
    //int? index,
    string name,
    OllamaToolCallFunctionArguments arguments);

internal record OllamaToolCallFunctionArguments(
    string? id,
    string? action,
    string? path,
    string? newPath,
    string? query,
    string? content,
    string? replaceText,
    int? lineNumber);

internal record OllamaToolParameter(
    string Name,
    string Type,
    string Description,
    string[]? Enum = null,
    bool Optional = false);