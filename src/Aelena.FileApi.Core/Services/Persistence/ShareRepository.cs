using Dapper;
using Microsoft.Data.Sqlite;

namespace Aelena.FileApi.Core.Services.Persistence;

/// <summary>A stored share link, as persisted in the <c>shares</c> table.</summary>
/// <param name="Token">Opaque share token; the primary key.</param>
/// <param name="JobId">Job whose report this share exposes.</param>
/// <param name="Report">The report body, as JSON.</param>
/// <param name="AccessType">Who may open the link (e.g. <c>anyone</c>).</param>
/// <param name="AllowedEmails">JSON array of permitted addresses, when restricted.</param>
/// <param name="PasswordHash">Hash of the link password, when one is set.</param>
/// <param name="CreatedAt">Round-trip ("o") UTC creation timestamp.</param>
/// <param name="ExpiresAt">Round-trip ("o") UTC expiry, or <c>null</c> to never expire.</param>
/// <param name="AccessCount">How many times the link has been opened.</param>
public sealed record ShareRecord(
    string Token,
    string JobId,
    string Report,
    string AccessType,
    string? AllowedEmails,
    string? PasswordHash,
    string CreatedAt,
    string? ExpiresAt,
    long AccessCount)
{
    /// <summary>True when <see cref="ExpiresAt"/> is set and already in the past.</summary>
    public bool IsExpired =>
        ExpiresAt is not null
        && DateTimeOffset.TryParse(ExpiresAt, out var expiry)
        && expiry <= DateTimeOffset.UtcNow;
}

/// <summary>Share metadata without the report body, for listing a job's links.</summary>
public sealed record ShareSummary(
    string Token,
    string AccessType,
    string? AllowedEmails,
    string? PasswordHash,
    string CreatedAt,
    string? ExpiresAt,
    long AccessCount);

/// <summary>
/// SQLite persistence layer for comparison share links.
/// Uses WAL mode for concurrent reads and a lazily opened single connection.
/// </summary>
/// <remarks>
/// Queries return records rather than <c>dynamic</c>. The previous signatures pushed
/// every column name into callers as an unchecked late-bound lookup: a renamed column
/// compiled cleanly and threw at runtime, and nothing could be found by search.
/// </remarks>
public sealed class ShareRepository : IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS shares (
            token          TEXT PRIMARY KEY,
            job_id         TEXT NOT NULL,
            report         TEXT NOT NULL,
            access_type    TEXT NOT NULL DEFAULT 'anyone',
            allowed_emails TEXT,
            password_hash  TEXT,
            created_at     TEXT NOT NULL,
            expires_at     TEXT,
            access_count   INTEGER NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS idx_shares_job_id ON shares(job_id);
        """;

    private readonly Lazy<SqliteConnection> _connection;

    /// <summary>
    /// Creates a new repository pointing at the given database path.
    /// The database file and directory are created automatically.
    /// </summary>
    public ShareRepository(string dbPath = "data/shares.db")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var connectionString = $"Data Source={dbPath}";

        // Lazy with the default thread-safety mode, rather than hand-rolled
        // double-checked locking on a non-volatile field: that pattern let a second
        // thread observe a non-null _conn that the first thread had not finished
        // opening or creating the schema on.
        _connection = new Lazy<SqliteConnection>(() =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            connection.Execute("PRAGMA journal_mode=WAL");
            connection.Execute(CreateTableSql);
            return connection;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private SqliteConnection Connection => _connection.Value;

    /// <summary>Insert a new share record.</summary>
    public void Create(
        string token, string jobId, string reportJson, string accessType,
        string? allowedEmailsJson, string? passwordHash, string? expiresAt) =>
        Connection.Execute(
            """
            INSERT INTO shares (token, job_id, report, access_type, allowed_emails, password_hash, created_at, expires_at)
            VALUES (@Token, @JobId, @Report, @AccessType, @AllowedEmails, @PasswordHash, @CreatedAt, @ExpiresAt)
            """,
            new
            {
                Token = token,
                JobId = jobId,
                Report = reportJson,
                AccessType = accessType,
                AllowedEmails = allowedEmailsJson,
                PasswordHash = passwordHash,
                CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
                ExpiresAt = expiresAt
            });

    /// <summary>Fetch a share by token. Returns <c>null</c> if not found.</summary>
    /// <remarks>
    /// Expired shares are returned; check <see cref="ShareRecord.IsExpired"/>. Expiry is
    /// the caller's decision because it determines the status code — 410 Gone for an
    /// expired link reads differently from 404 for one that never existed.
    /// </remarks>
    public ShareRecord? GetByToken(string token) =>
        Connection.QuerySingleOrDefault<ShareRecord>(
            """
            SELECT token AS Token, job_id AS JobId, report AS Report,
                   access_type AS AccessType, allowed_emails AS AllowedEmails,
                   password_hash AS PasswordHash, created_at AS CreatedAt,
                   expires_at AS ExpiresAt, access_count AS AccessCount
            FROM shares WHERE token = @Token
            """,
            new { Token = token });

    /// <summary>Bump the access counter for a share.</summary>
    public void IncrementAccessCount(string token) =>
        Connection.Execute(
            "UPDATE shares SET access_count = access_count + 1 WHERE token = @Token",
            new { Token = token });

    /// <summary>Delete a share. Returns <c>true</c> if a row was deleted.</summary>
    public bool Delete(string token) =>
        Connection.Execute(
            "DELETE FROM shares WHERE token = @Token",
            new { Token = token }) > 0;

    /// <summary>Return lightweight metadata for all shares of a given job (no report body).</summary>
    public IReadOnlyList<ShareSummary> ListForJob(string jobId) =>
        [.. Connection.Query<ShareSummary>(
            """
            SELECT token AS Token, access_type AS AccessType, allowed_emails AS AllowedEmails,
                   password_hash AS PasswordHash, created_at AS CreatedAt,
                   expires_at AS ExpiresAt, access_count AS AccessCount
            FROM shares WHERE job_id = @JobId
            ORDER BY created_at DESC
            """,
            new { JobId = jobId })];

    /// <summary>Remove every share whose expiry has passed. Returns the number deleted.</summary>
    public int DeleteExpired() =>
        Connection.Execute(
            "DELETE FROM shares WHERE expires_at IS NOT NULL AND expires_at <= @Now",
            new { Now = DateTimeOffset.UtcNow.ToString("o") });

    public void Dispose()
    {
        if (_connection.IsValueCreated)
            _connection.Value.Dispose();
    }
}
