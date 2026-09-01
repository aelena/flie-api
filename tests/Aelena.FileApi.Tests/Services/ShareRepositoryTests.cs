using Aelena.FileApi.Core.Services.Persistence;
using AwesomeAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public sealed class ShareRepositoryTests : IDisposable
{
    private readonly ShareRepository _repo;
    private readonly string _dbPath;

    public ShareRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fileapi_test_{Guid.NewGuid():N}.db");
        _repo = new ShareRepository(_dbPath);
    }

    [Fact]
    public void Create_And_GetByToken_RoundTrips()
    {
        _repo.Create("tok1", "job1", """{"status":"ok"}""", "anyone", null, null, null);

        var row = _repo.GetByToken("tok1")!;
        row.Token.Should().Be("tok1");
        row.JobId.Should().Be("job1");
        row.AccessType.Should().Be("anyone");
        row.Report.Should().Be("""{"status":"ok"}""");
    }

    [Fact]
    public void GetByToken_NonExistent_ReturnsNull()
    {
        (_repo.GetByToken("nope") is null).Should().BeTrue();
    }

    [Fact]
    public void IncrementAccessCount_Increments()
    {
        _repo.Create("tok1", "job1", "{}", "anyone", null, null, null);
        _repo.IncrementAccessCount("tok1");
        _repo.IncrementAccessCount("tok1");

        var row = _repo.GetByToken("tok1")!;
        row.AccessCount.Should().Be(2);
    }

    [Fact]
    public void Delete_ExistingToken_ReturnsTrue()
    {
        _repo.Create("tok1", "job1", "{}", "anyone", null, null, null);
        _repo.Delete("tok1").Should().BeTrue();
        (_repo.GetByToken("tok1") is null).Should().BeTrue();
    }

    [Fact]
    public void Delete_NonExistent_ReturnsFalse()
    {
        _repo.Delete("nope").Should().BeFalse();
    }

    [Fact]
    public void ListForJob_ReturnsMatchingShares()
    {
        _repo.Create("tok1", "job1", "{}", "anyone", null, null, null);
        _repo.Create("tok2", "job1", "{}", "restricted", """["a@b.com"]""", null, null);
        _repo.Create("tok3", "job2", "{}", "anyone", null, null, null);

        var shares = _repo.ListForJob("job1").ToList();
        shares.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithPasswordAndExpiry_StoresCorrectly()
    {
        _repo.Create("tok1", "job1", "{}", "anyone", null, "hashed_pw", "2030-01-01T00:00:00Z");

        var row = _repo.GetByToken("tok1")!;
        row.PasswordHash.Should().Be("hashed_pw");
        row.ExpiresAt.Should().Be("2030-01-01T00:00:00Z");
        row.IsExpired.Should().BeFalse();
    }

    // ── Expiry ───────────────────────────────────────────────────────────
    //
    // expires_at was stored at creation and never consulted again, so an expired
    // link kept serving its report indefinitely.

    [Fact]
    public void IsExpired_PastExpiry_IsTrue()
    {
        var past = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        _repo.Create("tok1", "job1", "{}", "anyone", null, null, past);

        _repo.GetByToken("tok1")!.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_NoExpiry_IsFalse()
    {
        _repo.Create("tok1", "job1", "{}", "anyone", null, null, null);

        _repo.GetByToken("tok1")!.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void DeleteExpired_RemovesOnlyThePastOnes()
    {
        var past = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        var future = DateTimeOffset.UtcNow.AddDays(1).ToString("o");

        _repo.Create("expired", "job1", "{}", "anyone", null, null, past);
        _repo.Create("live", "job1", "{}", "anyone", null, null, future);
        _repo.Create("forever", "job1", "{}", "anyone", null, null, null);

        _repo.DeleteExpired().Should().Be(1);

        _repo.GetByToken("expired").Should().BeNull();
        _repo.GetByToken("live").Should().NotBeNull();
        _repo.GetByToken("forever").Should().NotBeNull();
    }

    public void Dispose()
    {
        _repo.Dispose();
        try { File.Delete(_dbPath); } catch { /* cleanup */ }
    }
}
