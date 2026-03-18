using Aelena.FileApi.Core.Abstractions;
using Aelena.FileApi.Core.Models;
using Aelena.FileApi.Core.Services.Common;
using Aelena.FileApi.Core.Services.Jobs;
using Aelena.FileApi.Core.Services.Llm;
using Aelena.FileApi.Core.Services.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Aelena.FileApi.Core;

/// <summary>
/// Registers all FileApi Core services into the DI container.
/// Call <c>services.AddFileApiCore()</c> from your host application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all FileApi Core services (shared utilities, LLM, persistence, job stores) to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureLlm">Optional callback to configure LLM endpoints.</param>
    /// <param name="maxJobs">Maximum in-memory jobs for comparison/summarization stores.</param>
    /// <param name="maxBatches">Maximum in-memory batch jobs.</param>
    /// <param name="shareDbPath">Path to the SQLite share links database.</param>
    public static IServiceCollection AddFileApiCore(
        this IServiceCollection services,
        Action<LlmConfig>? configureLlm = null,
        int maxJobs = 1000,
        int maxBatches = 500,
        string shareDbPath = "data/shares.db")
    {
        // ── LLM ──────────────────────────────────────────────────────────
        var llmConfig = new LlmConfig();
        configureLlm?.Invoke(llmConfig);

        services.AddSingleton(llmConfig);
        services.AddSingleton<ILlmClientFactory, LlmClientFactory>();

        // ── Prompt rendering ─────────────────────────────────────────────
        services.AddSingleton<PromptRenderer>();

        // ── Webhook ──────────────────────────────────────────────────────
        services.AddSingleton<WebhookService>();

        // ── Job stores ───────────────────────────────────────────────────
        services.AddSingleton(new InMemoryJobStore<ComparisonReport>(maxJobs));
        services.AddSingleton(new InMemoryJobStore<SummarizeJobReport>(maxJobs));
        services.AddSingleton(new InMemoryJobStore<BatchJobResponse>(maxBatches));

        // ── Persistence ──────────────────────────────────────────────────
        services.AddSingleton(new ShareRepository(shareDbPath));

        return services;
    }
}
