using MyCodingAgent.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.Helpers;

public class OllamaClient(
    Uri? ollamaServerUrl = null) : IDisposable
{
    private readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3600)
    };
    private readonly Uri OllamaServerUrl = ollamaServerUrl ?? new Uri("http://localhost:11434");
    private readonly Dictionary<Language, Dictionary<string, string>> Dictionaries = new Dictionary<Language, Dictionary<string, string>>();

    public async Task<OllamaModel[]> GetModels(CancellationToken ct = default)
    {
        var url = new Uri(OllamaServerUrl, "/api/tags");
        var response = await HttpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<OllamaModelRawCollection>(stream, cancellationToken: ct);

        if (data?.models == null)
            return [];

        return [.. data.models
            .Where(a => !string.IsNullOrWhiteSpace(a.name) && a.size != null && a.modified_at != null)
            .Select(a => new OllamaModel(a.name!, a.size!,a.modified_at!))];
    }

    public async Task InitializeModelAsync(OllamaModel model, CancellationToken ct = default)
    {
        var request = new { model = model.Name };
        var url = new Uri(OllamaServerUrl, "/api/pull");
        var response = await HttpClient.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<OllamaResponse> ChatAsync(OllamaModel model, OllamaPrompt prompt, CancellationToken ct = default)
    {
        //var messages = prompt.messages
        //    .Select(message => 
        //        new OllamaMessage(
        //            message.role, 
        //            message.tool_call_id,
        //            message.content,
        //            null, 
        //            message.tool_calls));

        var tools = CreateToolsJson(prompt.tools);
        var payload = $@"{{
  ""model"": ""{model.Name}"",
  ""messages"": {JsonSerializer.Serialize(prompt.messages, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented)},
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

        return agentResponse;
    }

    public async Task<string> Translate(OllamaModel model, Language toLanguage, string content, bool overwrite, CancellationToken ct = default)
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

    public static string CreateToolsJson(Tool[] tools)
    {
        return string.Join(",", tools.Select(tool => $@"
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
    public static string? JsonEscape(string? s)
    {
        if (s == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var c in s)
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
