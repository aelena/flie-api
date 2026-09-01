using System.Text;
using System.Text.Json;
using Aelena.FileApi.Api.Logging;

namespace Aelena.FileApi.Api.Services;

/// <summary>
/// Fire-and-forget HTTP POST for webhook callbacks.
/// Infrastructure concern — lives in the Api layer, not in Core.
/// </summary>
public sealed class WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> log)
{
    /// <summary>
    /// Send a JSON payload to the specified webhook URL.
    /// Errors are logged but never thrown — webhook delivery is best-effort.
    /// </summary>
    public async Task SendAsync(string url, object payload, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("webhook");
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(url, content, ct);

            LogMessages.WebhookDelivered(log, url, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            LogMessages.WebhookFailed(log, ex, url);
        }
    }
}
