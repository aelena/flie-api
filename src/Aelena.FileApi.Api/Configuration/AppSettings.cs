using System.Text;

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

    /// <summary>
    /// The placeholder secret a fresh checkout starts with.
    /// </summary>
    /// <remarks>
    /// Anyone who has read this repository can forge a token signed with it, so
    /// <see cref="ThrowIfUnsafeForProduction"/> refuses to start outside Development
    /// while it is still in place.
    /// </remarks>
    public const string PlaceholderJwtSecret = "your-secret-key-change-in-production";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    private const int MinimumJwtSecretBytes = 32;

    public string JwtSecretKey { get; init; } = PlaceholderJwtSecret;

    /// <summary>JWA name of the signing algorithm. Only the HMAC family is accepted.</summary>
    public string JwtAlgorithm { get; init; } = "HS256";

    public int JwtExpirationDays { get; init; } = 7;

    /// <summary>
    /// The signing algorithms a token is allowed to declare.
    /// </summary>
    /// <remarks>
    /// Pinning this closes algorithm substitution: validation used to accept whatever
    /// <c>alg</c> the token itself carried, and <see cref="JwtAlgorithm"/> was
    /// configured but never read by anything.
    /// </remarks>
    public string[] AllowedJwtAlgorithms => [NormalisedJwtAlgorithm];

    private string NormalisedJwtAlgorithm => JwtAlgorithm.ToUpperInvariant() switch
    {
        "HS256" => "HS256",
        "HS384" => "HS384",
        "HS512" => "HS512",
        var other => throw new InvalidOperationException(
            $"JwtAlgorithm '{other}' is not supported. The signing key is symmetric, "
            + "so the algorithm must be HS256, HS384, or HS512.")
    };

    /// <summary>
    /// Refuse to start with credentials that are safe only on a developer's machine.
    /// </summary>
    /// <param name="isDevelopment">True when running in the Development environment.</param>
    /// <exception cref="InvalidOperationException">
    /// When the JWT secret is still the placeholder, or is too short to sign with,
    /// outside Development.
    /// </exception>
    public void ThrowIfUnsafeForProduction(bool isDevelopment)
    {
        // Checked once at startup rather than per request: a deployment that would
        // accept forged tokens should fail to boot, loudly, not serve traffic.
        _ = NormalisedJwtAlgorithm;

        if (isDevelopment) return;

        if (JwtSecretKey == PlaceholderJwtSecret)
            throw new InvalidOperationException(
                "AppSettings:JwtSecretKey is still the placeholder from the repository. "
                + "Anyone who has read the source can forge a session with it. "
                + "Set a random secret via the AppSettings__JwtSecretKey environment variable.");

        if (Encoding.UTF8.GetByteCount(JwtSecretKey) < MinimumJwtSecretBytes)
            throw new InvalidOperationException(
                $"AppSettings:JwtSecretKey must be at least {MinimumJwtSecretBytes} bytes "
                + $"for {JwtAlgorithm}; it is {Encoding.UTF8.GetByteCount(JwtSecretKey)}.");
    }

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
