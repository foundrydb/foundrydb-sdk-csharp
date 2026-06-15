using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

// ----- Request models -----

/// <summary>
/// Request body for enabling auth-as-a-service on a managed app service.
/// </summary>
public class AuthEnableRequest
{
    /// <summary>SMTP configuration for sending transactional auth emails.</summary>
    [JsonPropertyName("smtp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SmtpConfiguration? Smtp { get; set; }

    /// <summary>UI theme customisation for the hosted auth portal.</summary>
    [JsonPropertyName("theme")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthTheme? Theme { get; set; }

    /// <summary>Social / enterprise identity providers to enable.</summary>
    [JsonPropertyName("idp_providers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<IdpProviderConfig>? IdpProviders { get; set; }

    /// <summary>
    /// Controls where the issuer domain is sourced from.
    /// Accepted values: "service_domain", "custom_domain".
    /// </summary>
    [JsonPropertyName("issuer_domain_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IssuerDomainChoice { get; set; }

    /// <summary>UUID of a custom domain attachment to use as the issuer origin.</summary>
    [JsonPropertyName("attachment_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AttachmentId { get; set; }
}

/// <summary>
/// SMTP settings used for sending transactional auth emails (magic links, OTPs, etc.).
/// </summary>
public class SmtpConfiguration
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

    /// <summary>SMTP account password.</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Sender email address shown in the From header.</summary>
    [JsonPropertyName("from_address")]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Sender display name shown in the From header.</summary>
    [JsonPropertyName("from_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromName { get; set; }

    /// <summary>When true, TLS certificate verification is skipped. Not recommended for production.</summary>
    [JsonPropertyName("insecure_skip_verify")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InsecureSkipVerify { get; set; }
}

/// <summary>
/// UI theme settings for the hosted auth portal.
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
/// Configuration for a social or enterprise identity provider (request shape).
/// Includes the client secret, which is write-only and never returned by the API.
/// </summary>
public class IdpProviderConfig
{
    /// <summary>Provider identifier (e.g. "google", "github", "microsoft", "saml").</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>OAuth2 / OIDC client ID issued by the identity provider.</summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 / OIDC client secret issued by the identity provider.
    /// Write-only: this value is accepted on create/update but never returned by the API.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Human-readable label shown on the sign-in button.</summary>
    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}

// ----- Response models -----

/// <summary>
/// Response returned when auth is enabled on a service.
/// </summary>
public class AuthEnableResponse
{
    /// <summary>Active auth configuration for this service.</summary>
    [JsonPropertyName("auth")]
    public AuthConfiguration Auth { get; set; } = new();

    /// <summary>Active signing keys. The first key is the current signing key.</summary>
    [JsonPropertyName("signing_keys")]
    public List<AuthSigningKey> SigningKeys { get; set; } = new();
}

/// <summary>
/// Response returned when querying the auth configuration for a service.
/// Returns 404 when auth has not been enabled.
/// </summary>
public class AuthGetResponse
{
    /// <summary>Active auth configuration for this service.</summary>
    [JsonPropertyName("auth")]
    public AuthConfiguration Auth { get; set; } = new();

    /// <summary>Active signing keys. The first key is the current signing key.</summary>
    [JsonPropertyName("signing_keys")]
    public List<AuthSigningKey> SigningKeys { get; set; } = new();
}

/// <summary>
/// Response returned when auth is disabled on a service.
/// </summary>
public class AuthDisableResponse
{
    /// <summary>Human-readable status message.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response returned when a signing key is rotated.
/// </summary>
public class AuthRotateKeyResponse
{
    /// <summary>The newly active signing key.</summary>
    [JsonPropertyName("signing_key")]
    public AuthSigningKey SigningKey { get; set; } = new();
}

/// <summary>
/// Response returned when a session is revoked.
/// </summary>
public class AuthRevokeSessionResponse
{
    /// <summary>Task ID for the asynchronous revocation operation.</summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;
}

/// <summary>
/// Auth configuration attached to a managed app service.
/// </summary>
public class AuthConfiguration
{
    /// <summary>Unique identifier for the auth configuration.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Service this auth configuration belongs to.</summary>
    [JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>OIDC issuer URL (e.g. "https://auth.example.com").</summary>
    [JsonPropertyName("issuer_url")]
    public string? IssuerUrl { get; set; }

    /// <summary>Whether the auth service is currently enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>UI theme applied to the hosted auth portal.</summary>
    [JsonPropertyName("theme")]
    public AuthTheme? Theme { get; set; }

    /// <summary>
    /// Configured identity providers (response shape, client_secret omitted).
    /// </summary>
    [JsonPropertyName("idp_providers")]
    public List<IdpProviderInfo>? IdpProviders { get; set; }

    /// <summary>ISO-8601 timestamp when the auth configuration was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Identity provider information as returned by the API.
/// The client_secret is never included in responses.
/// </summary>
public class IdpProviderInfo
{
    /// <summary>Provider identifier (e.g. "google", "github", "microsoft", "saml").</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>OAuth2 / OIDC client ID registered with the identity provider.</summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown on the sign-in button.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// A JWT signing key used by the auth service.
/// </summary>
public class AuthSigningKey
{
    /// <summary>Unique key identifier (kid).</summary>
    [JsonPropertyName("key_id")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Key algorithm (e.g. "RS256", "ES256").</summary>
    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }

    /// <summary>Whether this key is currently active for signing new tokens.</summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    /// <summary>ISO-8601 timestamp when this key was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp when this key was rotated out (null if still active).</summary>
    [JsonPropertyName("rotated_at")]
    public DateTimeOffset? RotatedAt { get; set; }
}
