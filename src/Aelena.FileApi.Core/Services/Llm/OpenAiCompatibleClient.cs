using System.Net.Http.Headers;
using System.Text;
using Aelena.FileApi.Core.Abstractions;

namespace Aelena.FileApi.Core.Services.Llm;

/// <summary>
/// LLM client that speaks the OpenAI Chat Completions API.
/// Works with OpenAI, Azure OpenAI, Ollama, OpenWebUI, and any compatible endpoint.
/// </summary>
public sealed class OpenAiCompatibleClient(LlmEndpointConfig config, HttpClient http) : ILlmClient
{
    public async Task<string> CompleteAsync(
        string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var payload = new
        {
            model = config.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.1
        };

        return await SendRequest(payload, ct);
    }

    public async Task<string> CompleteVisionAsync(
        string systemPrompt, string userMessage, string imageBase64, string mediaType, CancellationToken ct = default)
    {
        var payload = new
        {
            model = config.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userMessage },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mediaType};base64,{imageBase64}" }
                        }
                    }
                }
            },
            temperature = 0.1
        };

        return await SendRequest(payload, ct);
    }

    private async Task<string> SendRequest(object payload, CancellationToken ct)
    {
        var baseUrl = config.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/chat/completions";

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
