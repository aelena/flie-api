using Aelena.FileApi.Core.Services.Jobs;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Async document comparison endpoints.</summary>
public static class CompareEndpoints
{
    public static RouteGroupBuilder MapCompareEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (IFormFile file_a, IFormFile file_b,
            string? mode, string? alignment, string? confidentiality,
            string? output, string? tone, string? language,
            bool? ignoreDiacritics, string? callbackUrl,
            InMemoryJobStore<ComparisonReport> store) =>
        {
            // Full comparison pipeline requires LLM integration — Phase 6 async job.
            // For now, create a stub job.
            var jobId = Guid.NewGuid().ToString("N")[..16];
            var report = new ComparisonReport(jobId, "processing",
                file_a.FileName, file_b.FileName,
                mode ?? "semantic", alignment ?? "page",
                confidentiality ?? "private", output ?? "structured", tone);
            store.Set(jobId, report);
            return Results.Accepted($"/compare/{jobId}", new JobCreatedResponse(jobId));
        }).WithName("CompareDocuments").DisableAntiforgery();

        group.MapGet("/{jobId}", (string jobId, InMemoryJobStore<ComparisonReport> store) =>
        {
            var report = store.Get(jobId);
            if (report is null) throw new FileApiException(404, $"Job {jobId} not found.");
            return report.Status == "processing" ? Results.Accepted(value: report) : Results.Ok(report);
        }).WithName("GetCompareResult");

        return group;
    }
}
