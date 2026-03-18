using System.Collections.Concurrent;

namespace Aelena.FileApi.Core.Services.Jobs;

/// <summary>
/// Generic bounded in-memory job store with automatic trimming of oldest entries.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Used for async comparison, summarization, and batch jobs.
/// </summary>
/// <typeparam name="T">The job report type (e.g. ComparisonReport, SummarizeJobReport).</typeparam>
public sealed class InMemoryJobStore<T> where T : class
{
    private readonly ConcurrentDictionary<string, (T Value, DateTimeOffset Created)> _store = new();
    private readonly int _maxItems;

    /// <summary>
    /// Creates a new job store with the specified capacity.
    /// When the store exceeds this capacity, the oldest entries are trimmed.
    /// </summary>
    /// <param name="maxItems">Maximum number of jobs to retain (0 = unlimited).</param>
    public InMemoryJobStore(int maxItems = 1000) => _maxItems = maxItems;

    /// <summary>Store or update a job.</summary>
    public void Set(string jobId, T value)
    {
        _store[jobId] = (value, DateTimeOffset.UtcNow);
        TrimIfNeeded();
    }

    /// <summary>Retrieve a job by ID. Returns <c>null</c> if not found.</summary>
    public T? Get(string jobId) =>
        _store.TryGetValue(jobId, out var entry) ? entry.Value : null;

    /// <summary>Check if a job exists.</summary>
    public bool Contains(string jobId) => _store.ContainsKey(jobId);

    /// <summary>Remove a job by ID.</summary>
    public bool Remove(string jobId) => _store.TryRemove(jobId, out _);

    /// <summary>Current number of stored jobs.</summary>
    public int Count => _store.Count;

    private void TrimIfNeeded()
    {
        if (_maxItems <= 0 || _store.Count <= _maxItems) return;

        // Remove oldest entries to get back under the limit
        var excess = _store.Count - _maxItems;
        var oldest = _store
            .OrderBy(kv => kv.Value.Created)
            .Take(excess)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in oldest)
            _store.TryRemove(key, out _);
    }
}
