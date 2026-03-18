using Dapper;
using Microsoft.Data.Sqlite;

namespace Aelena.FileApi.Core.Services.Persistence;

/// <summary>
/// SQLite persistence layer for comparison share links.
/// Uses WAL mode for concurrent reads and a lazy singleton connection.
/// </summary>
public sealed class ShareRepository : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _conn;
    private readonly object _lock = new();

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

    /// <summary>
    /// Creates a new repository pointing at the given database path.
    /// The database file and directory are created automatically.
    /// </summary>
    public ShareRepository(string dbPath = "data/shares.db")
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath}";
    }

    private SqliteConnection GetConnection()
    {
        if (_conn is not null) return _conn;

        lock (_lock)
        {
            if (_conn is not null) return _conn;

            _conn = new SqliteConnection(_connectionString);
            _conn.Open();
            _conn.Execute("PRAGMA journal_mode=WAL");
            _conn.Execute(CreateTableSql);
            return _conn;
        }
    }

    /// <summary>Insert a new share record.</summary>
    public void Create(
        string token, string jobId, string reportJson, string accessType,
        string? allowedEmailsJson, string? passwordHash, string? expiresAt)
    {
        GetConnection().Execute(
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
    }

    /// <summary>Fetch a share by token. Returns <c>null</c> if not found.</summary>
    public dynamic? GetByToken(string token) =>
        GetConnection().QuerySingleOrDefault(
            "SELECT * FROM shares WHERE token = @Token",
            new { Token = token });

    /// <summary>Bump the access counter for a share.</summary>
    public void IncrementAccessCount(string token) =>
        GetConnection().Execute(
            "UPDATE shares SET access_count = access_count + 1 WHERE token = @Token",
            new { Token = token });

    /// <summary>Delete a share. Returns <c>true</c> if a row was deleted.</summary>
    public bool Delete(string token) =>
        GetConnection().Execute(
            "DELETE FROM shares WHERE token = @Token",
            new { Token = token }) > 0;

    /// <summary>Return lightweight metadata for all shares of a given job (no report body).</summary>
    public IEnumerable<dynamic> ListForJob(string jobId) =>
        GetConnection().Query(
            """
            SELECT token, access_type, allowed_emails, password_hash, created_at, expires_at, access_count
            FROM shares WHERE job_id = @JobId
            ORDER BY created_at DESC
            """,
            new { JobId = jobId });

    public void Dispose() => _conn?.Dispose();
}
