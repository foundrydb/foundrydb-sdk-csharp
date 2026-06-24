using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.EdgeGateway;

/// <summary>
/// Operations on app service custom domains and the edge tier: settings (cache, rate limit,
/// WAF, access/auth, security hardening, rules engine), cache purge, analytics, log drains,
/// config versions, rollback, and staged rollouts.
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
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/domains", orgId: null, ct).ConfigureAwait(false);
        return DeserializeList<EdgeDomain>(json, "domains");
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
        RequireId(appServiceId, nameof(appServiceId));
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
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(domainId, nameof(domainId));

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
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(domainId, nameof(domainId));

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
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeStatus>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge status object.");
    }

    /// <summary>
    /// Returns the customer-tunable edge settings currently stored for an app service, plus the
    /// desired-state config version the fleet converges on. Basic Auth password hashes are never
    /// returned (only the enabled flag and the usernames); signed_urls and api_key_auth are
    /// returned in their non-secret view shapes.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeSettings> GetAppEdgeSettingsAsync(string appServiceId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge/settings", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeSettings>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge settings object.");
    }

    /// <summary>
    /// Replaces the customer-tunable edge settings for an app service (cache rules, rate limit,
    /// WAF mode and custom rules, IP allow/deny, redirects, header rules, CORS, maintenance,
    /// compression, max body, allowed methods, basic auth, blocked paths, HSTS, request id,
    /// canary, health check, origin pool, the ordered rules engine, JWT/signed-URL/API-key
    /// access control, and DDoS/bot/ATO security hardening). Domains and origin are
    /// platform-derived and cannot be changed here. Returns the updated settings and the config
    /// version the fleet will converge on.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="settings">New edge settings.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeSettings> UpdateAppEdgeSettingsAsync(string appServiceId, EdgeSettingsRequest settings, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        ArgumentNullException.ThrowIfNull(settings);

        var json = await _client.PutAsync($"/app-services/{appServiceId}/edge/settings", settings, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeSettings>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge settings object.");
    }

    // ----- Cache purge -----

    /// <summary>
    /// Flushes the app's edge cache across its serving PoP nodes, either entirely
    /// (<c>request.All</c>) or for the listed absolute paths (<c>request.Paths</c>); set exactly
    /// one. The purge rolls across nodes one at a time in the background, so the response reports
    /// the plan (planned node count and ids) rather than the completed result.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="request">The purge request (all or specific paths).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeCachePurgeResponse> PurgeAppEdgeCacheAsync(string appServiceId, EdgeCachePurgeRequest request, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        ArgumentNullException.ThrowIfNull(request);

        var json = await _client.PostAsync($"/app-services/{appServiceId}/edge/cache/purge", request, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeCachePurgeResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a cache purge object.");
    }

    // ----- Analytics -----

    /// <summary>
    /// Returns the account-scoped edge analytics summary for an app over <paramref name="windowMinutes"/>
    /// (pass 0 to use the server default of 60 minutes). The summary covers the request status
    /// breakdown, error rate, cache hit ratio, latency percentiles, rate-limited and WAF detection
    /// counts, top paths, and a suspicious-path threat summary, folded across the app's PoPs with a
    /// per-PoP breakdown.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="windowMinutes">Window length in minutes (0 = server default).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeAnalytics> GetAppEdgeAnalyticsAsync(string appServiceId, int windowMinutes = 0, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        var path = $"/app-services/{appServiceId}/edge/analytics";
        if (windowMinutes > 0)
            path += $"?window_minutes={windowMinutes}";

        var json = await _client.GetAsync(path, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeAnalytics>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge analytics object.");
    }

    // ----- Log drains -----

    /// <summary>
    /// Lists the app's edge access-log drains.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<EdgeLogDrain>> ListEdgeLogDrainsAsync(string appServiceId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge/log-drains", orgId: null, ct).ConfigureAwait(false);
        return DeserializeList<EdgeLogDrain>(json, "drains");
    }

    /// <summary>
    /// Creates a new edge access-log drain for the app.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="request">The drain to create.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeLogDrain> CreateEdgeLogDrainAsync(string appServiceId, CreateEdgeLogDrainRequest request, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        ArgumentNullException.ThrowIfNull(request);

        var json = await _client.PostAsync($"/app-services/{appServiceId}/edge/log-drains", request, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeLogDrain>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a log drain object.");
    }

    /// <summary>
    /// Returns one edge access-log drain.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="drainId">Log drain UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeLogDrain> GetEdgeLogDrainAsync(string appServiceId, string drainId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(drainId, nameof(drainId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge/log-drains/{drainId}", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeLogDrain>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a log drain object.");
    }

    /// <summary>
    /// Partially updates an edge access-log drain; omitted fields keep their value.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="drainId">Log drain UUID.</param>
    /// <param name="request">The partial update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeLogDrain> UpdateEdgeLogDrainAsync(string appServiceId, string drainId, UpdateEdgeLogDrainRequest request, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(drainId, nameof(drainId));
        ArgumentNullException.ThrowIfNull(request);

        var json = await _client.PutAsync($"/app-services/{appServiceId}/edge/log-drains/{drainId}", request, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeLogDrain>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a log drain object.");
    }

    /// <summary>
    /// Deletes an edge access-log drain, stopping all future exports for it.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="drainId">Log drain UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteEdgeLogDrainAsync(string appServiceId, string drainId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(drainId, nameof(drainId));

        await _client.DeleteAsync($"/app-services/{appServiceId}/edge/log-drains/{drainId}", orgId: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies connectivity to the drain's destination without sending real log data.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="drainId">Log drain UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeLogDrainTestResult> TestEdgeLogDrainAsync(string appServiceId, string drainId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        RequireId(drainId, nameof(drainId));

        var json = await _client.PostAsync($"/app-services/{appServiceId}/edge/log-drains/{drainId}/test", null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeLogDrainTestResult>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a log drain test result.");
    }

    // ----- Config versions and rollback -----

    /// <summary>
    /// Returns the append-only version history of an app service's edge configuration, newest
    /// first, plus the live active version. Use it to find a version to roll back to with
    /// <see cref="RollbackAppEdgeConfigAsync"/>.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeConfigVersions> ListAppEdgeConfigVersionsAsync(string appServiceId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge/versions", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeConfigVersions>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an edge config versions object.");
    }

    /// <summary>
    /// Rolls an app service's edge configuration back to a prior version. Supply exactly one of
    /// <c>request.ToVersion</c> or <c>request.To = "previous"</c>. The rollback restores the
    /// target version's customer-settable subset onto the live configuration as a NEW forward
    /// version (keeping the current platform-derived domains and origin); it never mutates the
    /// history. The edge fleet converges on the new version asynchronously (poll
    /// <see cref="GetAppEdgeStatusAsync"/>). The response returns the new active version.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="request">Which version to roll back to.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeRollbackResponse> RollbackAppEdgeConfigAsync(string appServiceId, EdgeRollbackRequest request, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));
        ArgumentNullException.ThrowIfNull(request);

        var json = await _client.PostAsync($"/app-services/{appServiceId}/edge/rollback", request, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeRollbackResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a rollback object.");
    }

    // ----- Staged rollouts -----

    /// <summary>
    /// Returns the app service's current staged config rollout (the active one, or the most
    /// recent terminal one), or <c>Active = false</c> with a null rollout when the app has never
    /// had one. Canary rollouts are opened by the platform when the app's edge settings enable
    /// <c>CanaryRolloutEnabled</c> and a new config version is produced.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<EdgeRolloutStatus> GetAppEdgeRolloutAsync(string appServiceId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/edge/rollout", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<EdgeRolloutStatus>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a rollout status object.");
    }

    /// <summary>
    /// Promotes a holding canary rollout so the platform fans the canary version out to the rest
    /// of the fleet. Only an active rollout in the canary phase can be promoted.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task PromoteAppEdgeRolloutAsync(string appServiceId, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        await _client.PostAsync($"/app-services/{appServiceId}/edge/rollout/promote", null, orgId: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Aborts an active rollout. The rest of the fleet was never given the target version, so it
    /// keeps serving the prior version; the canary subset can be recovered with
    /// <see cref="RollbackAppEdgeConfigAsync"/>. The request reason is an optional operator note.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="request">Optional abort note.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AbortAppEdgeRolloutAsync(string appServiceId, EdgeRolloutAbortRequest? request = null, CancellationToken ct = default)
    {
        RequireId(appServiceId, nameof(appServiceId));

        await _client.PostAsync($"/app-services/{appServiceId}/edge/rollout/abort", request, orgId: null, ct).ConfigureAwait(false);
    }

    // ----- Helpers -----

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", paramName);
    }

    private static List<T> DeserializeList<T>(string json, string wrapperKey)
    {
        var items = new List<T>();
        if (string.IsNullOrWhiteSpace(json)) return items;

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(wrapperKey, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var item = JsonSerializer.Deserialize<T>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (item is not null) items.Add(item);
            }
        }

        return items;
    }

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
