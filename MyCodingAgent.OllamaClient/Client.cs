using MyCodingAgent.Models;
using MyCodingAgent.Shared;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.OllamaClient;

public class Client(
    Uri? ollamaServerUrl = null) 
    : IDisposable
    , ILlmClient
{
    private readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3600)
    };
    private readonly Uri OllamaServerUrl = ollamaServerUrl ?? new Uri("http://localhost:11434");
    private readonly Dictionary<Language, Dictionary<string, string>> Dictionaries = new Dictionary<Language, Dictionary<string, string>>();

    public async Task<Model[]> GetModels(CancellationToken ct = default)
    {
        var url = new Uri(OllamaServerUrl, "/api/tags");
        var response = await HttpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<OllamaModelRawCollection>(stream, cancellationToken: ct);

        if (data?.models == null)
            return [];

        var models = new List<Model>();
        foreach (var model in data.models
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.name) &&
                a.size != null &&
                a.modified_at != null))
        {
            var maxTokenSize = await GetContextSize(model.name!, ct);
            models.Add(new Model(
                model.name!,
                model.size!,
                maxTokenSize,
                model.modified_at!));
        }
        return [.. models];
    }
    private async Task<int?> GetContextSize(string model, CancellationToken ct = default)
    {
        var url = new Uri(OllamaServerUrl, "/api/show");

        var body = JsonSerializer.Serialize(new { name = model });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        var data = await JsonSerializer.DeserializeAsync<OllamaShowResponse>(stream, cancellationToken: ct);

        return data?.parameters?.num_ctx;
    }

    public async Task InitializeModelAsync(Model model, CancellationToken ct = default)
    {
        var request = new { model = model.Name };
        var url = new Uri(OllamaServerUrl, "/api/pull");
        var response = await HttpClient.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Response> ChatAsync(Model model, Prompt prompt, CancellationToken ct = default)
    {
        OllamaMessage[] messages =
        [
            ..prompt.messages.Select(a =>
                new OllamaMessage(
                    a.role,
                    a.tool_call_id,
                    a.content,
                    a.thinking,
                    a.tool_calls == null? null : [..a.tool_calls.Select(b =>
                        new OllamaToolCall(
                            b.id,
                            new OllamaToolCallFunction(
                                b.function.name,
                                new OllamaToolCallFunctionArguments()
                                {
                                    action = b.function.arguments.action,
                                    content = b.function.arguments.content,
                                    id = b.function.arguments.id,
                                    lineNumber = b.function.arguments.lineNumber,
                                    newPath = b.function.arguments.newPath,
                                    path = b.function.arguments.path,
                                    query = b.function.arguments.query,
                                    replaceText = b.function.arguments.replaceText
                                })))
                    ]))
        ];

        var tools = CreateToolsJson(prompt.tools);
        var payload = $@"{{
  ""model"": ""{model.Name}"",
  ""messages"": {JsonSerializer.Serialize(messages, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented)},
  ""stream"": false,
  ""tools"": [{tools}]
}}";

        var url = new Uri(OllamaServerUrl, "/api/chat");
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload)
        };

        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var agentResponseString =
            await response.Content.ReadAsStringAsync();

        var agentResponse =
            JsonSerializer.Deserialize<OllamaResponse>(agentResponseString)
            ?? throw new Exception("Something is not right");

        return new Response(
            agentResponse.model,
            agentResponse.created_at,
            new Message(
                agentResponse.message.role,
                agentResponse.message.tool_call_id,
                agentResponse.message.content,
                agentResponse.message.thinking,
                agentResponse.message.tool_calls == null
                ? (ToolCall[]?)null
                :
                [
                    ..agentResponse.message.tool_calls.Select(a =>
                        new ToolCall(a.id, new ToolCallFunction(a.function.name, new ToolCallFunctionArguments()
                        {
                            action = a.function.arguments.action,
                            content = a.function.arguments.content,
                            id = a.function.arguments.id,
                            lineNumber = a.function.arguments.lineNumber,
                            newPath = a.function.arguments.newPath,
                            path = a.function.arguments.path,
                            query = a.function.arguments.query,
                            replaceText = a.function.arguments.replaceText,
                        })))
                ]));
    }

    public async Task<string> Translate(Model model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default)
    {
        if (!Dictionaries.TryGetValue(toLanguage, out var dictionary))
        {
            dictionary = new Dictionary<string, string>();
            Dictionaries[toLanguage] = dictionary;
        }

        if (!overwrite && dictionary.TryGetValue(content, out var translation))
        {
            return translation;
        }

        var messages = new[]
        {
            new
            {
                role = "system",
                content = $"You are a translator. Translate everything to {Enum.GetName(toLanguage)}. Only return the translated text, nothing else."
            },
            new
            {
                role = "user",
                content = content
            }
        };

        var payloadObject = new
        {
            model = model.Name,
            messages = messages,
            stream = false
        };

        var payload = JsonSerializer.Serialize(
            payloadObject,
            DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);

        var url = new Uri(OllamaServerUrl, "/api/chat");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        var result = JsonSerializer.Deserialize<OllamaResponse>(json)
            ?? throw new Exception("Invalid response from Ollama");

        // 🔥 Dit is wat je zoekt
        var agentTranslation = result.message?.content?.Trim()
            ?? throw new Exception("No content returned");

        dictionary[content] = agentTranslation;

        return agentTranslation;
    }

    public string CreateToolsJson(Tool[] tools)
    {
        OllamaTool[] ollamaTools =
        [
            ..tools.Select(a =>
                new OllamaTool(
                    a.Name,
                    a.Desciption,
                    [ ..a.Parameters.Select(b =>
                        new OllamaToolParameter(
                            b.Name,
                            b.Type,
                            b.Description,
                            b.Enum,
                            b.Optional))
                    ]))
        ];
        return string.Join(",", ollamaTools.Select(tool => $@"
  {{
    ""type"": ""function"",
    ""function"": {{
      ""name"": ""{JsonEscape(tool.Name)}"",
      ""description"": ""{JsonEscape(tool.Desciption)}"",
      ""parameters"": {{
        ""type"": ""object"",
        ""properties"": {{{string.Join(",", tool.Parameters.Select(parameter => $@"
          ""{JsonEscape(parameter.Name)}"": {{
            ""type"": ""{parameter.Type}"",
            ""description"": ""{JsonEscape(parameter.Description)}""{(parameter.Enum == null ? "" : $@",
            ""enum"": [{string.Join(", ", parameter.Enum.Select(e => $@"""{JsonEscape(e)}"""))}]")}
          }}"))}
        }},
        ""required"": [{string.Join(", ", tool.Parameters.Where(p => p.Optional == false).Select(parameter => $@"""{JsonEscape(parameter.Name)}"""))}]
      }}
    }}
  }}"));
    }
    private string? JsonEscape(string? value)
    {
        if (value == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append("\\u" + ((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
