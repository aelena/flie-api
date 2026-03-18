namespace Aelena.FileApi.Core.Abstractions;

/// <summary>
/// Abstraction over an LLM chat completion API.
/// Implementations may target OpenAI, local Ollama, Azure OpenAI, etc.
/// </summary>
public interface ILlmClient
{
    /// <summary>Send a single-turn chat completion request.</summary>
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>Send a vision chat completion request (text + image).</summary>
    Task<string> CompleteVisionAsync(string systemPrompt, string userMessage, string imageBase64, string mediaType, CancellationToken cancellationToken = default);
}
