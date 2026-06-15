using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

// ----- Request models -----

/// <summary>
/// Request body for <c>POST /app-services/{id}/auth/enable</c>.
/// AttachmentId names one of the app's existing PostgreSQL attachments to back
/// the identity store. IssuerDomainChoice is "fallback" or "custom" and is
/// fixed at enable time. SMTP is mandatory. IdpProviders optionally enables
/// social login (Google and GitHub); an empty list enables magic-link login only.
/// </summary>
public class AuthEnableRequest
{
    /// <summary>
    /// UUID of one of the app's existing PostgreSQL attachments that will back
    /// the identity store.
    /// </summary>
    [JsonPropertyName("attachment_id")]
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>
    /// Where the issuer domain comes from. "fallback" uses an
    /// auth-&lt;id&gt;.foundrydb.com subdomain; "custom" uses the app's
    /// primary custom domain. Fixed at enable time.
    /// </summary>
    [JsonPropertyName("issuer_domain_choice")]
    public string IssuerDomainChoice { get; set; } = string.Empty;

    /// <summary>
    /// SMTP credentials used by the issuer to send magic-link emails.
    /// Write-only: stored in the platform secret store, never returned.
    /// </summary>
    [JsonPropertyName("smtp")]
    public AuthSmtpConfig Smtp { get; set; } = new();

    /// <summary>Branding applied to the hosted login pages.</summary>
    [JsonPropertyName("theme")]
    public AuthTheme Theme { get; set; } = new();

    /// <summary>
    /// Social-login providers to enable. Each entry's ClientSecret is
    /// write-only and never returned. Omit or leave empty for magic-link only.
    /// </summary>
    [JsonPropertyName("idp_providers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<IdpProviderRequest>? IdpProviders { get; set; }
}

/// <summary>
/// SMTP credentials the issuer uses to send magic-link emails. Write-only:
/// accepted on enable, stored in the platform secret store, and never returned.
/// </summary>
public class AuthSmtpConfig
{
    /// <summary>SMTP server hostname.</summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port (e.g. 587 for STARTTLS, 465 for implicit TLS).</summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>SMTP account username.</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP account password. Write-only.</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Sender email address shown in the From header.</summary>
    [JsonPropertyName("from_address")]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Sender display name shown in the From header.</summary>
    [JsonPropertyName("from_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Disables STARTTLS certificate verification. For test mail catchers only;
    /// never set in production.
    /// </summary>
    [JsonPropertyName("insecure_skip_verify")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool InsecureSkipVerify { get; set; }
}

/// <summary>
/// Non-PII branding applied to the hosted login pages.
/// </summary>
public class AuthTheme
{
    /// <summary>Application name shown in the auth UI.</summary>
    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Primary brand colour as a hex string (e.g. "#5B21B6").</summary>
    [JsonPropertyName("brand_color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrandColor { get; set; }

    /// <summary>URL of the application logo image.</summary>
    [JsonPropertyName("logo_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LogoUrl { get; set; }

    /// <summary>URL of a support page linked from error screens.</summary>
    [JsonPropertyName("support_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SupportUrl { get; set; }
}

/// <summary>
/// Enables one social-login provider. ClientSecret is write-only: stored in the
/// platform secret store and never returned by any response.
/// </summary>
public class IdpProviderRequest
{
    /// <summary>Provider identifier. Accepted values: "google", "github".</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>OAuth2 / OIDC client ID issued by the identity provider.</summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 / OIDC client secret. Write-only: accepted here, stored in the
    /// platform secret store, and never returned by any response.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Human-readable label shown on the sign-in button (optional).</summary>
    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}

// ----- Response models -----

/// <summary>
/// Response from <c>POST /app-services/{id}/auth/enable</c> and
/// <c>GET /app-services/{id}/auth</c>: the auth configuration plus its signing
/// key records.
/// </summary>
public class AuthConfigurationWithKeys
{
    /// <summary>Auth enablement configuration.</summary>
    [JsonPropertyName("auth")]
    public AuthConfiguration? Auth { get; set; }

    /// <summary>
    /// Signing key records for this configuration. Status follows the dual-kid
    /// rotation lifecycle: pending, active, retiring, retired, or revoked.
    /// </summary>
    [JsonPropertyName("signing_keys")]
    public List<AuthSigningKey> SigningKeys { get; set; } = new();
}

/// <summary>
/// Response from <c>POST /app-services/{id}/auth/disable</c>.
/// </summary>
public class AuthDisableResponse
{
    /// <summary>Human-readable status message (e.g. "Disabled").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response from <c>POST /app-services/{id}/auth/rotate-key</c>: the newly
/// minted signing key.
/// </summary>
public class AuthRotateKeyResponse
{
    /// <summary>The newly active signing key.</summary>
    [JsonPropertyName("signing_key")]
    public AuthSigningKey? SigningKey { get; set; }
}

/// <summary>
/// Response from <c>POST /app-services/{id}/auth/sessions/{sessionId}/revoke</c>.
/// Revocation is asynchronous; use the task ID to poll for completion.
/// </summary>
public class AuthRevokeSessionResponse
{
    /// <summary>Task ID for the asynchronous revocation operation.</summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;
}

/// <summary>
/// Auth enablement record for an app service. The identity data itself lives in
/// the customer's own PostgreSQL database; this record holds enablement state
/// only. Secret custody locations are never serialised.
/// </summary>
public class AuthConfiguration
{
    /// <summary>Unique identifier for this auth configuration.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Owner user ID.</summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Organisation this configuration belongs to (if any).</summary>
    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationId { get; set; }

    /// <summary>ID of the app service that owns this auth configuration.</summary>
    [JsonPropertyName("app_service_id")]
    public string AppServiceId { get; set; } = string.Empty;

    /// <summary>ID of the PostgreSQL service backing the identity store.</summary>
    [JsonPropertyName("database_service_id")]
    public string DatabaseServiceId { get; set; } = string.Empty;

    /// <summary>Attachment ID linking the app to the backing PostgreSQL service.</summary>
    [JsonPropertyName("attachment_id")]
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>OIDC issuer URL (e.g. "https://auth-&lt;id&gt;.foundrydb.com").</summary>
    [JsonPropertyName("issuer_url")]
    public string IssuerUrl { get; set; } = string.Empty;

    /// <summary>Fallback issuer domain (always an auth-&lt;id&gt;.foundrydb.com subdomain).</summary>
    [JsonPropertyName("fallback_domain")]
    public string FallbackDomain { get; set; } = string.Empty;

    /// <summary>Custom domain used as the issuer origin when IssuerDomainChoice is "custom".</summary>
    [JsonPropertyName("custom_domain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomDomain { get; set; }

    /// <summary>Enablement status (e.g. "Enabled", "Disabled", "Provisioning").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Version of the identity schema applied in the customer database.</summary>
    [JsonPropertyName("schema_version_applied")]
    public string SchemaVersionApplied { get; set; } = string.Empty;

    /// <summary>Human-readable reason when the configuration is in a failure state.</summary>
    [JsonPropertyName("failure_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FailureReason { get; set; }

    /// <summary>Branding applied to the hosted login pages.</summary>
    [JsonPropertyName("theme")]
    public AuthTheme Theme { get; set; } = new();

    /// <summary>
    /// Configured social-login providers. Client secrets are never included.
    /// An empty list means social login is not configured.
    /// </summary>
    [JsonPropertyName("idp_providers")]
    public List<IdpProviderInfo> IdpProviders { get; set; } = new();

    /// <summary>ID of the internal authd app service (platform-managed).</summary>
    [JsonPropertyName("auth_app_service_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuthAppServiceId { get; set; }

    /// <summary>Timestamp when the auth configuration was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Non-secret configuration of one social-login provider returned in
/// <see cref="AuthConfiguration.IdpProviders"/>. The client_secret is never
/// included in any response; it is custodied in the platform secret store.
/// </summary>
public class IdpProviderInfo
{
    /// <summary>Provider identifier (e.g. "google", "github").</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>OAuth2 / OIDC client ID registered with the identity provider.</summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown on the sign-in button.</summary>
    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Controller-side record of one JWT signing keypair. The key material is held
/// in the platform secret store; only the kid, algorithm, and lifecycle status
/// are exposed. Status follows the dual-kid rotation lifecycle: pending, active,
/// retiring, retired, or revoked.
/// </summary>
public class AuthSigningKey
{
    /// <summary>Unique identifier for this signing key record.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>ID of the auth configuration this key belongs to.</summary>
    [JsonPropertyName("auth_configuration_id")]
    public string AuthConfigurationId { get; set; } = string.Empty;

    /// <summary>JWK key ID (kid) embedded in signed tokens.</summary>
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    /// <summary>Signing algorithm (e.g. "RS256", "ES256").</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status: pending, active, retiring, retired, or revoked.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Timestamp when this key became the active signing key.</summary>
    [JsonPropertyName("activated_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>Timestamp when this key was retired (null while still active).</summary>
    [JsonPropertyName("retired_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RetiredAt { get; set; }

    /// <summary>Timestamp when this key record was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of the last update to this key record.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
