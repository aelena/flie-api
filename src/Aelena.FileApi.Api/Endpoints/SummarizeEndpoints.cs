using Aelena.FileApi.Core.Services.Jobs;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Async document summarization endpoints.</summary>
public static class SummarizeEndpoints
{
    public static RouteGroupBuilder MapSummarizeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", (IFormFile file, string? confidentiality,
            string? callbackUrl, string? output, string? tone, string? instructions,
            InMemoryJobStore<SummarizeJobReport> store) =>
        {
            var jobId = Guid.NewGuid().ToString("N")[..16];
            var report = new SummarizeJobReport(jobId, "processing", file.FileName);
            store.Set(jobId, report);
            // TODO: Launch background task with LLM summarization pipeline
            return Results.Accepted($"/summarize/{jobId}", new JobCreatedResponse(jobId));
        }).WithName("SummarizeDocument").DisableAntiforgery();

        group.MapGet("/{jobId}", (string jobId, InMemoryJobStore<SummarizeJobReport> store) =>
        {
            var report = store.Get(jobId);
            if (report is null) throw new FileApiException(404, $"Job {jobId} not found.");
            return report.Status == "processing" ? Results.Accepted(value: report) : Results.Ok(report);
        }).WithName("GetSummarizeResult");

        return group;
    }
}
