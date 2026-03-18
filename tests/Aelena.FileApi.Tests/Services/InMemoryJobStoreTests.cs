using Aelena.FileApi.Core.Services.Jobs;
using FluentAssertions;
using Xunit;

namespace Aelena.FileApi.Tests.Services;

public class InMemoryJobStoreTests
{
    [Fact]
    public void Set_And_Get_RoundTrips()
    {
        var store = new InMemoryJobStore<string>();
        store.Set("j1", "hello");
        store.Get("j1").Should().Be("hello");
    }

    [Fact]
    public void Get_NonExistent_ReturnsNull()
    {
        var store = new InMemoryJobStore<string>();
        store.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Contains_ReturnsTrueForExisting()
    {
        var store = new InMemoryJobStore<string>();
        store.Set("j1", "val");
        store.Contains("j1").Should().BeTrue();
        store.Contains("j2").Should().BeFalse();
    }

    [Fact]
    public void Remove_DeletesJob()
    {
        var store = new InMemoryJobStore<string>();
        store.Set("j1", "val");
        store.Remove("j1").Should().BeTrue();
        store.Get("j1").Should().BeNull();
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        var store = new InMemoryJobStore<string>();
        store.Remove("missing").Should().BeFalse();
    }

    [Fact]
    public void Count_TracksEntries()
    {
        var store = new InMemoryJobStore<string>();
        store.Count.Should().Be(0);
        store.Set("a", "1");
        store.Set("b", "2");
        store.Count.Should().Be(2);
    }

    [Fact]
    public void Trim_RemovesOldestWhenOverCapacity()
    {
        var store = new InMemoryJobStore<string>(maxItems: 3);
        store.Set("a", "1");
        store.Set("b", "2");
        store.Set("c", "3");
        store.Set("d", "4"); // triggers trim

        store.Count.Should().BeLessThanOrEqualTo(3);
        store.Get("d").Should().Be("4"); // newest should survive
    }

    [Fact]
    public void Set_UpdatesExistingJob()
    {
        var store = new InMemoryJobStore<string>();
        store.Set("j1", "v1");
        store.Set("j1", "v2");
        store.Get("j1").Should().Be("v2");
        store.Count.Should().Be(1);
    }

    [Fact]
    public void UnlimitedStore_NeverTrims()
    {
        var store = new InMemoryJobStore<string>(maxItems: 0);
        for (var i = 0; i < 100; i++)
            store.Set($"j{i}", $"v{i}");

        store.Count.Should().Be(100);
    }
}
