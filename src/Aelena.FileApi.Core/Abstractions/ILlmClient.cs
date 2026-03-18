namespace Aelena.FileApi.Core.Abstractions;

/// <summary>
/// Abstraction over an LLM chat completion API.
/// Implementations may target OpenAI, local Ollama, Azure OpenAI, etc.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Send a single-turn chat completion request.
    /// </summary>
    /// <param name="systemPrompt">System message defining the LLM's role.</param>
    /// <param name="userMessage">User message with the content to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response text.</returns>
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a vision chat completion request (text + image).
    /// </summary>
    /// <param name="systemPrompt">System message.</param>
    /// <param name="userMessage">Text prompt.</param>
    /// <param name="imageBase64">Base64-encoded image data.</param>
    /// <param name="mediaType">MIME type of the image (e.g. "image/png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response text.</returns>
    Task<string> CompleteVisionAsync(string systemPrompt, string userMessage, string imageBase64, string mediaType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory that creates <see cref="ILlmClient"/> instances routed by confidentiality tier.
/// </summary>
public interface ILlmClientFactory
{
    /// <summary>
    /// Get an LLM client for the given confidentiality level.
    /// </summary>
    /// <param name="confidentiality">Routing tier: Public (cloud), Private (local), or AirGapped (none).</param>
    /// <param name="vision">If true, returns a vision-capable client.</param>
    /// <returns>An LLM client, or <c>null</c> if <paramref name="confidentiality"/> is AirGapped.</returns>
    ILlmClient? GetClient(Enums.Confidentiality confidentiality, bool vision = false);
}
