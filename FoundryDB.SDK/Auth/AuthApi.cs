using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Auth;

/// <summary>
/// Operations for auth-as-a-service on a managed app service.
/// Auth provides a hosted identity layer (OIDC / JWT) backed by the service's
/// PostgreSQL database, with optional social login via configurable identity providers.
/// </summary>
public class AuthApi
{
    private readonly FoundryDBClient _client;

    internal AuthApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Enables auth-as-a-service for a managed app service and returns the active
    /// configuration together with the initial signing keys.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    /// <param name="req">Enable parameters (SMTP, theme, identity providers, issuer domain).</param>
    public async Task<AuthEnableResponse> EnableAsync(string serviceId, AuthEnableRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));
        ArgumentNullException.ThrowIfNull(req);

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/enable", req, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthEnableResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth enable payload.");
    }

    /// <summary>
    /// Returns the current auth configuration and signing keys for a managed app service.
    /// Throws <see cref="FoundryDBException"/> with status 404 when auth has not been enabled.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    public async Task<AuthGetResponse> GetAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.GetAsync($"/app-services/{serviceId}/auth", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthGetResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth configuration.");
    }

    /// <summary>
    /// Disables auth-as-a-service for a managed app service. All active sessions
    /// are invalidated and the signing keys are revoked.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    public async Task<AuthDisableResponse> DisableAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/disable", payload: null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthDisableResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth disable payload.");
    }

    /// <summary>
    /// Rotates the active JWT signing key for a managed app service.
    /// The previous key is retained for token verification until it expires.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    public async Task<AuthRotateKeyResponse> RotateKeyAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/rotate-key", payload: null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthRotateKeyResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a signing key.");
    }

    /// <summary>
    /// Revokes a specific session by ID, immediately invalidating its tokens.
    /// Returns HTTP 202 Accepted; the revocation is completed asynchronously.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    /// <param name="sessionId">Session identifier to revoke.</param>
    public async Task<AuthRevokeSessionResponse> RevokeSessionAsync(string serviceId, string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));
        if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Session ID must not be empty.", nameof(sessionId));

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/sessions/{sessionId}/revoke", payload: null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthRevokeSessionResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a revoke session payload.");
    }

    // ----- helpers -----

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, FoundryDBClient.JsonOptions);
    }
}
