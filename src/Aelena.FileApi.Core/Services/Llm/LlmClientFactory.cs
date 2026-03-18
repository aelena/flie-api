using Aelena.FileApi.Core.Abstractions;
using Aelena.FileApi.Core.Enums;

namespace Aelena.FileApi.Core.Services.Llm;

/// <summary>
/// Configuration record for a single LLM endpoint.
/// </summary>
public sealed record LlmEndpointConfig(string BaseUrl, string ApiKey, string Model);

/// <summary>
/// Configuration for all LLM endpoints, grouped by confidentiality tier.
/// </summary>
public sealed record LlmConfig
{
    public LlmEndpointConfig Public { get; init; } = new("https://api.openai.com/v1", "", "gpt-4o");
    public LlmEndpointConfig Private { get; init; } = new("http://localhost:3000/api/v1", "", "ComparisonModel");
    public LlmEndpointConfig PublicVision { get; init; } = new("https://api.openai.com/v1", "", "gpt-4o");
    public LlmEndpointConfig PrivateVision { get; init; } = new("http://localhost:11434/v1", "", "llama3.2-vision:11b");
}

/// <summary>
/// Creates <see cref="ILlmClient"/> instances routed by confidentiality tier.
/// <list type="bullet">
///   <item><c>Public</c> → cloud API (e.g. OpenAI GPT-4o)</item>
///   <item><c>Private</c> → local model (OpenWebUI / Ollama)</item>
///   <item><c>AirGapped</c> → returns <c>null</c> (no LLM calls)</item>
/// </list>
/// </summary>
public sealed class LlmClientFactory(LlmConfig config, IHttpClientFactory httpClientFactory) : ILlmClientFactory
{
    public ILlmClient? GetClient(Confidentiality confidentiality, bool vision = false) =>
        confidentiality switch
        {
            Confidentiality.AirGapped => null,
            Confidentiality.Public => CreateClient(vision ? config.PublicVision : config.Public),
            Confidentiality.Private => CreateClient(vision ? config.PrivateVision : config.Private),
            _ => null
        };

    private OpenAiCompatibleClient CreateClient(LlmEndpointConfig endpoint) =>
        new(endpoint, httpClientFactory.CreateClient("llm"));
}
