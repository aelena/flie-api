using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for all endpoint tests.
/// All test classes using this fixture are grouped into the same collection
/// to avoid parallel WebApplicationFactory creation conflicts.
/// </summary>
[CollectionDefinition("FileApi")]
public class FileApiCollection : ICollectionFixture<WebApplicationFactory<Program>>;

[Collection("FileApi")]
public abstract class FileApiFixture
{
    protected readonly HttpClient Client;
    protected readonly WebApplicationFactory<Program> Factory;

    protected FileApiFixture(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
