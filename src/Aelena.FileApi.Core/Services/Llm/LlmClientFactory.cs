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
/// Pure static factory — no DI, no ASP.NET dependencies.
/// Caller provides the <see cref="HttpClient"/> instance.
/// </summary>
public static class LlmClientFactory
{
    /// <summary>
    /// Create an LLM client for the given confidentiality level.
    /// Returns <c>null</c> for <see cref="Confidentiality.AirGapped"/>.
    /// </summary>
    /// <param name="config">LLM endpoint configuration.</param>
    /// <param name="http">HttpClient to use for requests.</param>
    /// <param name="confidentiality">Routing tier.</param>
    /// <param name="vision">If true, returns a vision-capable client.</param>
    public static ILlmClient? Create(
        LlmConfig config, HttpClient http,
        Confidentiality confidentiality, bool vision = false) =>
        confidentiality switch
        {
            Confidentiality.AirGapped => null,
            Confidentiality.Public => new OpenAiCompatibleClient(
                vision ? config.PublicVision : config.Public, http),
            Confidentiality.Private => new OpenAiCompatibleClient(
                vision ? config.PrivateVision : config.Private, http),
            _ => null
        };
}
