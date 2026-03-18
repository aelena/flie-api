using Aelena.FileApi.Core.Services.Persistence;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class ShareRepositoryTests : IDisposable
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
        ((string)row.token).Should().Be("tok1");
        ((string)row.job_id).Should().Be("job1");
        ((string)row.access_type).Should().Be("anyone");
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
        ((long)row.access_count).Should().Be(2);
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
        ((string)row.password_hash).Should().Be("hashed_pw");
        ((string)row.expires_at).Should().Be("2030-01-01T00:00:00Z");
    }

    public void Dispose()
    {
        _repo.Dispose();
        try { File.Delete(_dbPath); } catch { /* cleanup */ }
    }
}
