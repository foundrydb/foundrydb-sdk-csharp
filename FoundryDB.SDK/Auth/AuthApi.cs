using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Auth;

/// <summary>
/// Auth-as-a-service operations on a managed app service.
/// Auth provides a hosted OIDC / JWT identity layer backed by the service's
/// PostgreSQL database, with optional social login via Google and GitHub.
/// All endpoints are under <c>/app-services/{serviceId}/auth</c>.
/// </summary>
public class AuthApi
{
    private readonly FoundryDBClient _client;

    internal AuthApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Enables auth-as-a-service for a managed app service and returns the auth
    /// configuration together with the initial signing keys. The named attachment
    /// must reference a PostgreSQL service; the platform provisions the identity
    /// schema in the customer database and stands up the OIDC issuer. SMTP
    /// credentials and any social-login client secrets are stored in the platform
    /// secret store and never returned.
    /// </summary>
    /// <param name="serviceId">App service UUID.</param>
    /// <param name="req">Enable parameters (attachment, SMTP, theme, identity providers, issuer domain).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Auth configuration and initial signing keys.</returns>
    public async Task<AuthConfigurationWithKeys> EnableAsync(string serviceId, AuthEnableRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));
        ArgumentNullException.ThrowIfNull(req);

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/enable", req, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthConfigurationWithKeys>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth enable payload.");
    }

    /// <summary>
    /// Returns the current auth configuration and signing key records for a
    /// managed app service. Throws <see cref="FoundryDBException"/> with HTTP
    /// status 404 when auth has not been enabled on this service.
    /// </summary>
    /// <param name="serviceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Auth configuration and signing keys.</returns>
    public async Task<AuthConfigurationWithKeys> GetAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.GetAsync($"/app-services/{serviceId}/auth", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthConfigurationWithKeys>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth configuration.");
    }

    /// <summary>
    /// Disables auth-as-a-service for a managed app service. The end-user
    /// identity data in the customer's database is left untouched; only the
    /// platform-managed issuer and enablement state are torn down.
    /// </summary>
    /// <param name="serviceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Disable result with a status string.</returns>
    public async Task<AuthDisableResponse> DisableAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/disable", payload: null, orgId: null, ct).ConfigureAwait(false);
        return Deserialize<AuthDisableResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an auth disable payload.");
    }

    /// <summary>
    /// Rotates the JWT signing key and returns the newly minted key record.
    /// Rotation is dual-kid: the new key is published alongside the outgoing
    /// one so tokens signed by the previous key keep validating until it retires.
    /// </summary>
    /// <param name="serviceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly minted signing key record.</returns>
    public async Task<AuthSigningKey> RotateKeyAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.PostAsync($"/app-services/{serviceId}/auth/rotate-key", payload: null, orgId: null, ct).ConfigureAwait(false);
        var result = Deserialize<AuthRotateKeyResponse>(json)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a signing key.");
        return result.SigningKey
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response signing_key was null.");
    }

    /// <summary>
    /// Revokes one end-user session by ID. Revocation is dispatched
    /// asynchronously to the backing database's primary VM. The API returns
    /// HTTP 202 Accepted with a task ID that can be used to poll completion.
    /// </summary>
    /// <param name="serviceId">App service UUID.</param>
    /// <param name="sessionId">Session identifier to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Revoke response with the asynchronous task ID.</returns>
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
