using Aelena.FileApi.Core.Services.Jobs;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Batch processing endpoints — parallel multi-file operations.</summary>
public static class BatchEndpoints
{
    private static readonly HashSet<string> ValidOps = ["summarize", "classify", "pii", "hash", "text", "qa", "search"];

    public static RouteGroupBuilder MapBatchEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{operation}", (string operation, IFormFileCollection files,
            string? callbackUrl, string? confidentiality, string? question, string? query, string? pattern,
            InMemoryJobStore<BatchJobResponse> store) =>
        {
            if (!ValidOps.Contains(operation))
                throw new FileApiException(400, $"Unknown batch operation: {operation}. Valid: {string.Join(", ", ValidOps)}");
            if (files.Count == 0)
                throw new FileApiException(400, "No files provided.");

            var batchId = Guid.NewGuid().ToString("N")[..16];
            var results = files.Select(f => new BatchFileResult(batchId, f.FileName, "pending")).ToList();
            var batch = new BatchJobResponse(batchId, "processing", operation, files.Count, 0, 0, results);
            store.Set(batchId, batch);

            // TODO: Launch background processing with _dispatch per file
            return Results.Accepted($"/batch/{batchId}", new { batch_id = batchId, status = "processing" });
        }).WithName("BatchProcess").DisableAntiforgery();

        group.MapGet("/{batchId}", (string batchId, InMemoryJobStore<BatchJobResponse> store) =>
        {
            var batch = store.Get(batchId);
            if (batch is null) throw new FileApiException(404, $"Batch {batchId} not found.");
            return batch.Status == "processing" ? Results.Accepted(value: batch) : Results.Ok(batch);
        }).WithName("GetBatchResult");

        return group;
    }
}
