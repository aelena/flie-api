namespace Aelena.FileApi.Api.Configuration;

/// <summary>
/// Application configuration bound from environment variables and appsettings.json.
/// Mirrors the Python <c>Settings</c> class for full compatibility.
/// </summary>
public sealed record AppSettings
{
    // ── Public LLM (cloud API — confidentiality=public) ──────────────────

    /// <summary>Base URL for the public cloud LLM API (e.g. OpenAI).</summary>
    public string PublicLlmBaseUrl { get; init; } = "https://api.openai.com/v1";
    public string PublicLlmApiKey { get; init; } = "";
    public string PublicLlmModel { get; init; } = "gpt-4o";

    // ── Private LLM (local — confidentiality=private) ────────────────────

    /// <summary>Base URL for local LLM (OpenWebUI / Ollama). No trailing slash.</summary>
    public string PrivateLlmBaseUrl { get; init; } = "http://host.docker.internal:3000/api/v1";
    public string PrivateLlmApiKey { get; init; } = "";
    public string PrivateLlmModel { get; init; } = "ComparisonModel";

    // ── Vision models (multimodal) ───────────────────────────────────────

    public string PublicVisionModel { get; init; } = "gpt-4o";
    public string PrivateVisionModel { get; init; } = "llama3.2-vision:11b";
    public string PrivateVisionBaseUrl { get; init; } = "http://host.docker.internal:11434/v1";

    // ── Frontend / Share links ───────────────────────────────────────────

    public string FrontendBaseUrl { get; init; } = "http://localhost:9600";

    // ── JWT authentication ───────────────────────────────────────────────

    public string JwtSecretKey { get; init; } = "your-secret-key-change-in-production";
    public string JwtAlgorithm { get; init; } = "HS256";
    public int JwtExpirationDays { get; init; } = 7;

    // ── CORS ─────────────────────────────────────────────────────────────

    /// <summary>Comma-separated list of allowed origins.</summary>
    public string CorsOrigins { get; init; } = "http://localhost:9600";

    /// <summary>Parsed list of allowed origins.</summary>
    public string[] GetCorsOriginList() =>
        CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // ── Rate limits (0 or -1 = unlimited) ────────────────────────────────

    public int MaxFilesPerBatch { get; init; }
    public long MaxFileSizeBytes { get; init; }
    public long MaxBatchSizeBytes { get; init; }
    public int MaxRequestsPerDay { get; init; }
    public int MaxInMemoryJobs { get; init; } = 1000;
    public int MaxInMemoryBatches { get; init; } = 500;

    /// <summary>Returns true when the given limit value means "unlimited".</summary>
    public static bool IsUnlimited(long value) => value <= 0;
}
