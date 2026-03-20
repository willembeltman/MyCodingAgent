using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.OpenAiClient;

public class ChatGpt_Client : IDisposable, IClient
{
    private readonly HttpClient HttpClient;
    private readonly string OpenAiApiKey;
    private readonly Dictionary<Language, Dictionary<string, string>> Dictionaries = [];

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
            dictionary = [];
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
            .GetString()
            ?? throw new Exception("No content returned from OpenAI");

        dictionary[content] = translated;
        return translated;
    }

    public async Task<Response> ChatAsync(Model model, ApiCall apiCall, CancellationToken ct = default)
    {
        string payloadJson = CreateRequestJson(model, apiCall);

        var responseJson = await DoCall(payloadJson, ct);
        var doc = JsonDocument.Parse(responseJson);

        var choice = doc.RootElement.GetProperty("choices")[0];
        var msg = choice.GetProperty("message");

        string? content = msg.TryGetProperty("content", out var c) ? c.GetString() : null;
        ToolCall[]? toolCalls = null;

        if (msg.TryGetProperty("function_call", out var fc))
        {
            toolCalls =
            [
                new ToolCall(
                    Guid.NewGuid().ToString(), // unique id
                    new ToolCallFunction(
                        fc.GetProperty("name").GetString()!,
                        JsonSerializer.Deserialize<ToolCallFunctionArguments>(
                            fc.GetProperty("arguments").GetRawText()
                        )!
                    )
                )
            ];
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

    // Let op: onderdeel van de interface
    public string CreateMessagesJson(Message[] messages)
    {
        var openAiMessages = messages.Select(m => new
        {
            role = m.Role,
            tool_call_id = m.ToolCallId,
            content = m.Content,
            thinking = m.Thinking,
            tool_calls = m.ToolCalls?.Select(tc => new
            {
                id = tc.Id,
                function = new
                {
                    name = tc.Function.Name,
                    arguments = tc.Function.Arguments
                }
            }).ToArray()
        }).ToArray();

        return JsonSerializer.Serialize(
            openAiMessages,
            DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }
    // Let op: onderdeel van de interface
    public string CreateToolsJson(Tool[] tools)
    {
        var openAiTools = tools.Select(tool => new
        {
            name = tool.Name,
            Description = tool.Desciption,
            Parameters = tool.Parameters.Select(p => new
            {
                name = p.Name,
                type = p.Type,
                description = p.Description,
                @enum = p.Enum,
                optional = p.Optional
            }).ToArray()
        }).ToArray();

        return JsonSerializer.Serialize(
            openAiTools,
            DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }

    // Let op: onderdeel van de interface
    public string CreateRequestJson(Model model, ApiCall apiCall)
    {
        var payload = new
        {
            model = model.Name,
            messages = apiCall.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            functions = apiCall.Tools.Select(tool => new
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
            })
        };

        var payloadJson = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        return payloadJson;
    }
}
