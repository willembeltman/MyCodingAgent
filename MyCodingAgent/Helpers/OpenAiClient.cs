using MyCodingAgent.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyCodingAgent.Helpers;

/// <summary>
/// BE CAREFULL
/// This class is not used, and not working.
/// I just copyed it from OllamaClient and changed name and endpoints
///
/// To support OpenAi, we need to detach the 'Ollama'-models from the engine further.
/// I already tried to do this while developing the engine but I had trouble with the OllamaMessage.
/// Off the top of my head we need to duplicate all the 'Message'-models, and map them inside the OllamaService calls.
/// Then we can make a specific implementation
/// 
/// 1. Base URL voor ChatGPT API
/// De base url wordt:
/// https://api.openai.com/v1/
/// Dus bijvoorbeeld:
/// Ollama OpenAI
/// /api/chat	/v1/chat/completions
/// /api/tags	/v1/models
/// 
/// 2. API key toevoegen
/// Je moet een header toevoegen:
/// Authorization: Bearer YOUR_API_KEY
/// In C# kan dat zo:
/// private readonly HttpClient HttpClient = new()
/// {
/// Timeout = TimeSpan.FromSeconds(3600)
/// };
/// public OllamaClient(string apiKey)
/// {
/// HttpClient.DefaultRequestHeaders.Authorization =
///     new AuthenticationHeaderValue("Bearer", apiKey);
/// }
/// Je moet ook deze namespace toevoegen:
/// using System.Net.Http.Headers;
/// 
/// 3. Chat endpoint aanpassen
/// Bij OpenAI wordt dit endpoint:
/// POST https://api.openai.com/v1/chat/completions
/// En payload bijvoorbeeld:
/// {
///   "model": "gpt-5-mini",
///   "messages": [
///     {"role": "system", "content": "You are helpful"},
///     {"role": "user", "content": "Hello"}
///   ],
///   "tools": [...]
/// }
/// Dus jouw URL moet veranderen naar:
/// var url = "https://api.openai.com/v1/chat/completions";
/// 4.Groot verschil met Ollama
/// Ollama response:
/// response.message.content
/// OpenAI response:
/// choices[0].message.content
/// Dus je response model moet anders zijn.
/// 5. Models ophalen
/// Ollama:
/// GET / api / tags
/// OpenAI:
/// GET / v1 / models
/// 6.Handige truc(provider abstraction)
/// Omdat je al een client hebt, zou ik eigenlijk dit doen:
/// interface ILLMClient
/// {
///     Task<LLMResponse> ChatAsync(...);
///     Task<LLMModel[]> GetModels();
/// }
/// 
/// Dan implementaties:
/// OllamaClient
/// OpenAIClient
/// DeepSeekClient
/// 
/// Dan kun je alle providers pluggen.
/// Dit is precies hoe frameworks zoals LangChain het doen.
/// 💡 Belangrijk voor jouw agentsysteem
/// Omdat jij MCP tools gebruikt:
/// OpenAI gebruikt:
/// tools
/// tool_choice
/// tool_calls
/// 
/// Ollama gebruikt:
/// tools
/// tool_calls
/// 
/// Dus je tool JSON kan bijna 1-op-1 blijven.
/// 
/// </summary>
public class OpenAiClient : IDisposable
{
    private readonly Uri OpenAiServerUrl;
    private readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3600)
    };

    public OpenAiClient(string apiKey, Uri? openAiServerUrl = null)
    {
        OpenAiServerUrl = openAiServerUrl ?? new Uri("http://localhost:11434");
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<OllamaModel[]> GetModels(CancellationToken ct = default)
    {
        var url = new Uri(OpenAiServerUrl, "/v1/models");
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
        var url = new Uri(OpenAiServerUrl, "/api/pull");
        var response = await HttpClient.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<OllamaResponse> ChatAsync(OllamaModel model, OllamaPrompt prompt, CancellationToken ct = default)
    {
        var payload = $@"{{
  ""model"": ""{model.Name}"",
  ""messages"": {JsonSerializer.Serialize(prompt.messages, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented)},
  ""stream"": false,
  ""tools"": [{GetToolsJson(prompt.tools)}]
}}";

        var url = new Uri(OpenAiServerUrl, "/v1/chat/completions");
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

    public static string GetToolsJson(Tool[] tools)
    {
        return string.Join(",", tools.Select(tool => $@"
  {{
    ""type"": ""function"",
    ""function"": {{
      ""name"": ""{tool.Name}"",
      ""description"": ""{tool.Desciption}"",
      ""parameters"": {{
        ""type"": ""object"",
        ""properties"": {{{string.Join(",", tool.Parameters.Select(parameter => $@"
          ""{parameter.Name}"": {{
            ""type"": ""{parameter.Type}"",
            ""description"": ""{parameter.Description}""
          }}"))}
        }},
        ""required"": [{string.Join(",", tool.Parameters.Select(parameter => $@"""{parameter.Name}"""))}]
      }}
    }}
  }}"));
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
