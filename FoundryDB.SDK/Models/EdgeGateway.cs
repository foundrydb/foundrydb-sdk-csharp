using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// Lifecycle status of a custom domain attached to an app service.
/// </summary>
public enum EdgeDomainStatus
{
    /// <summary>DNS ownership has not yet been confirmed.</summary>
    PendingVerification,
    /// <summary>DNS verification is in progress.</summary>
    Verifying,
    /// <summary>A TLS certificate is being issued.</summary>
    IssuingCertificate,
    /// <summary>The certificate is propagating across PoPs.</summary>
    Propagating,
    /// <summary>The domain is live and serving traffic.</summary>
    Active,
    /// <summary>Verification or certificate issuance failed. Check ErrorMessage.</summary>
    Failed,
    /// <summary>The domain is being removed.</summary>
    Deleting
}

/// <summary>
/// Web application firewall mode for one app service.
/// </summary>
public enum EdgeWAFMode
{
    /// <summary>WAF is disabled; all requests pass through unfiltered.</summary>
    Off,
    /// <summary>WAF inspects requests and logs matches but does not block.</summary>
    Detect
}

/// <summary>
/// Selects what a rate-limit bucket is keyed on.
/// </summary>
public enum EdgeRateLimitKey
{
    /// <summary>One bucket per client IP address.</summary>
    Ip,
    /// <summary>One bucket per API key presented in the request.</summary>
    ApiKey
}

/// <summary>
/// A custom domain attached to an app service, served through the edge tier.
/// </summary>
public class EdgeDomain
{
    /// <summary>Unique identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>App service this domain belongs to.</summary>
    [JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Owner user ID.</summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>The customer-supplied hostname (e.g. "www.example.com").</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    [JsonPropertyName("status")]
    public EdgeDomainStatus Status { get; set; }

    /// <summary>TLS certificate ID once a certificate has been issued.</summary>
    [JsonPropertyName("certificate_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CertificateId { get; set; }

    /// <summary>ISO-8601 timestamp of the last DNS verification probe.</summary>
    [JsonPropertyName("verification_checked_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerificationCheckedAt { get; set; }

    /// <summary>Human-readable error detail when Status is Failed.</summary>
    [JsonPropertyName("error_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }

    /// <summary>Platform hostname the customer must point their CNAME record at.</summary>
    [JsonPropertyName("cname_target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CnameTarget { get; set; }

    /// <summary>ISO-8601 timestamp when this domain was added.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp of the last change.</summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Caches responses for requests whose path begins with PathPrefix for a fixed duration.
/// </summary>
public class EdgeCacheRule
{
    /// <summary>URL path prefix to cache (e.g. "/static/").</summary>
    [JsonPropertyName("path_prefix")]
    public string PathPrefix { get; set; } = string.Empty;

    /// <summary>How long matched responses are kept in the edge cache, in seconds.</summary>
    [JsonPropertyName("ttl_seconds")]
    public int TtlSeconds { get; set; }
}

/// <summary>
/// Token-bucket rate limit enforced per PoP at the edge.
/// </summary>
public class EdgeRateLimit
{
    /// <summary>Sustained request rate allowed per key per second.</summary>
    [JsonPropertyName("requests_per_second")]
    public int RequestsPerSecond { get; set; }

    /// <summary>Maximum burst size above the sustained rate.</summary>
    [JsonPropertyName("burst")]
    public int Burst { get; set; }

    /// <summary>What each rate-limit bucket is keyed on.</summary>
    [JsonPropertyName("key")]
    public EdgeRateLimitKey Key { get; set; }
}

/// <summary>
/// Customer-tunable edge settings sent to PUT /app-services/{id}/edge/settings.
/// Domains and origin are platform-derived and cannot be set here.
/// </summary>
public class EdgeSettingsRequest
{
    /// <summary>Path-prefix cache rules to apply. Replaces the previous set when provided.</summary>
    [JsonPropertyName("cache_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeCacheRule>? CacheRules { get; set; }

    /// <summary>Rate-limit policy. Set to null to remove an existing policy.</summary>
    [JsonPropertyName("rate_limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRateLimit? RateLimit { get; set; }

    /// <summary>WAF mode. Omit to leave the current setting unchanged.</summary>
    [JsonPropertyName("waf_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeWAFMode? WafMode { get; set; }
}

/// <summary>
/// Per-PoP convergence state for one app service.
/// </summary>
public class EdgeApplicationStatusItem
{
    /// <summary>UpCloud zone slug (e.g. "se-sto1").</summary>
    [JsonPropertyName("zone")]
    public string Zone { get; set; } = string.Empty;

    /// <summary>Config version currently applied in this PoP.</summary>
    [JsonPropertyName("applied_version")]
    public long AppliedVersion { get; set; }

    /// <summary>Short status string (e.g. "converged", "updating", "error").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Error detail when this PoP failed to converge.</summary>
    [JsonPropertyName("error_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Edge overview for an app service: where it is served and how far the fleet has converged.
/// </summary>
public class EdgeStatus
{
    /// <summary>Whether the edge tier is enabled for this app service.</summary>
    [JsonPropertyName("edge_enabled")]
    public bool EdgeEnabled { get; set; }

    /// <summary>Primary PoP zone slug (e.g. "se-sto1").</summary>
    [JsonPropertyName("home_pop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HomePop { get; set; }

    /// <summary>Platform hostname that custom domains must CNAME to.</summary>
    [JsonPropertyName("cname_target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CnameTarget { get; set; }

    /// <summary>Desired-state config version the fleet is converging on.</summary>
    [JsonPropertyName("config_version")]
    public long ConfigVersion { get; set; }

    /// <summary>Per-PoP convergence entries.</summary>
    [JsonPropertyName("applications")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeApplicationStatusItem>? Applications { get; set; }
}

/// <summary>
/// Customer-tunable edge settings as returned after an update.
/// </summary>
public class EdgeSettings
{
    /// <summary>Active path-prefix cache rules.</summary>
    [JsonPropertyName("cache_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeCacheRule>? CacheRules { get; set; }

    /// <summary>Active rate-limit policy, or null when none is configured.</summary>
    [JsonPropertyName("rate_limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRateLimit? RateLimit { get; set; }

    /// <summary>WAF mode currently in effect.</summary>
    [JsonPropertyName("waf_mode")]
    public EdgeWAFMode WafMode { get; set; }

    /// <summary>Config version the fleet will converge on after this update.</summary>
    [JsonPropertyName("config_version")]
    public long ConfigVersion { get; set; }
}
