using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.EdgeGateway;

/// <summary>
/// Operations on app service custom domains and edge settings (cache rules, rate limiting, WAF mode).
/// </summary>
public class EdgeGatewayApi
{
    private readonly FoundryDBClient _client;

    internal EdgeGatewayApi(FoundryDBClient client)
    {
        _client = client;
    }

    // ----- Custom domains -----

    /// <summary>
    /// Returns all custom domains attached to the given app service.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<EdgeDomain>> ListAppDomainsAsync(string appServiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/domains", orgId: null, ct).ConfigureAwait(false);
        var domains = new List<EdgeDomain>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("domains", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var d = JsonSerializer.Deserialize<EdgeDomain>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (d is not null) domains.Add(d);
            }
        }

        return domains;
    }

    /// <summary>
    /// Adds a custom domain to an app service. The domain is created in
    /// <c>PendingVerification</c> status. Call <see cref="VerifyAppDomainAsync"/> to trigger
    /// an immediate DNS check, or wait for the background worker to pick it up.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="domain">Fully-qualified domain name to add (e.g. "www.example.com").</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeDomain> CreateAppDomainAsync(string appServiceId, string domain, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain must not be empty.", nameof(domain));

        var body = new { domain };
        var json = await _client.PostAsync($"/app-services/{appServiceId}/domains", body, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeDomain>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a domain object.");
    }

    /// <summary>
    /// Requeues a pending or failed domain for an immediate DNS verification pass.
    /// The platform responds with 202 Accepted; the domain status will update asynchronously.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="domainId">Domain UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeDomain> VerifyAppDomainAsync(string appServiceId, string domainId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID must not be empty.", nameof(domainId));

        var json = await _client.PostAsync($"/app-services/{appServiceId}/domains/{domainId}/verify", null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeDomain>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a domain object.");
    }

    /// <summary>
    /// Removes a custom domain from an app service. A 404 response is treated as success (idempotent).
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="domainId">Domain UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteAppDomainAsync(string appServiceId, string domainId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID must not be empty.", nameof(domainId));

        try
        {
            await _client.DeleteAsync($"/app-services/{appServiceId}/domains/{domainId}", orgId: null, ct).ConfigureAwait(false);
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            // Idempotent: not found is success.
        }
    }

    // ----- Edge status and settings -----

    /// <summary>
    /// Returns the edge overview for an app service: whether the edge tier is enabled, the home PoP,
    /// the CNAME target, the desired-state config version, and per-PoP convergence status.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeStatus> GetAppEdgeStatusAsync(string appServiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeStatus>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge status object.");
    }

    /// <summary>
    /// Replaces the customer-tunable edge settings (cache rules, rate limit, WAF mode) for an app service.
    /// Domains and origin are platform-derived and cannot be changed here.
    /// Returns the updated settings and the config version the fleet will converge on.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="settings">New edge settings.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeSettings> UpdateAppEdgeSettingsAsync(string appServiceId, EdgeSettingsRequest settings, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        ArgumentNullException.ThrowIfNull(settings);

        var json = await _client.PutAsync($"/app-services/{appServiceId}/edge/settings", settings, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeSettings>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge settings object.");
    }

    // ----- Helpers -----

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        using var doc = JsonDocument.Parse(json);

        // Prefer a named object wrapper key (value must be a JSON object to avoid
        // matching same-named scalar properties on the target type itself).
        foreach (var key in new[] { "domain", "edge_status", "edge_settings" })
        {
            if (doc.RootElement.TryGetProperty(key, out var el)
                && el.ValueKind == JsonValueKind.Object)
            {
                return JsonSerializer.Deserialize<T>(el.GetRawText(), FoundryDBClient.JsonOptions);
            }
        }

        return JsonSerializer.Deserialize<T>(json, FoundryDBClient.JsonOptions);
    }
}
