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
    Detect,
    /// <summary>WAF inspects, logs, and rejects matching requests with a 403 before they reach the origin.</summary>
    Block
}

/// <summary>
/// Action a matching custom WAF rule takes.
/// </summary>
public enum EdgeWAFRuleAction
{
    /// <summary>Deny a matching request with a 403 (only enforced when waf_mode is block).</summary>
    Block,
    /// <summary>Record a match without blocking.</summary>
    Log
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
/// Where the rate-limit token-bucket counter lives. Platform-set by the controller;
/// only echoed on a response.
/// </summary>
public enum EdgeRateLimitBackend
{
    /// <summary>The counter lives in the serving node's process.</summary>
    InProcess,
    /// <summary>The counter is shared across the fleet in a Valkey instance.</summary>
    Valkey
}

/// <summary>
/// Load-balancing policy used across the combined upstream set
/// (the primary auto origin plus the origin pool's additional origins).
/// </summary>
public enum EdgeOriginLBPolicy
{
    /// <summary>Distribute requests evenly in rotation.</summary>
    RoundRobin,
    /// <summary>Distribute requests proportionally to each origin's weight.</summary>
    Weighted,
    /// <summary>Send each request to the origin with the fewest active connections.</summary>
    LeastConn,
    /// <summary>Always prefer the first healthy origin (active/standby failover).</summary>
    First
}

/// <summary>
/// Action an ordered edge rule takes when its match holds. Terminal actions
/// (Redirect, Block, OriginOverride) short-circuit the rule chain; non-terminal
/// actions (SetHeader, Rewrite, Continue) fall through to later rules.
/// </summary>
public enum EdgeRuleActionType
{
    /// <summary>Redirect the request to a target URL.</summary>
    Redirect,
    /// <summary>Set or remove request/response headers.</summary>
    SetHeader,
    /// <summary>Rewrite the request path before it reaches the origin.</summary>
    Rewrite,
    /// <summary>Deny the request with a status code.</summary>
    Block,
    /// <summary>Override the upstream origin for this request.</summary>
    OriginOverride,
    /// <summary>Fall through to later rules and the fixed handler chain.</summary>
    Continue
}

/// <summary>
/// Where an inbound API key is read from on the request.
/// </summary>
public enum EdgeApiKeyLocation
{
    /// <summary>The key is read from a request header.</summary>
    Header,
    /// <summary>The key is read from a query-string parameter.</summary>
    Query
}

/// <summary>
/// Action the bot-management feature takes on a request classified as a bot.
/// </summary>
public enum EdgeBotAction
{
    /// <summary>Record the classification without blocking.</summary>
    Log,
    /// <summary>Deny the request.</summary>
    Block,
    /// <summary>Issue a challenge before allowing the request.</summary>
    Challenge
}

/// <summary>
/// Action account-takeover protection takes when a threshold is exceeded.
/// </summary>
public enum EdgeAtoAction
{
    /// <summary>Raise an alert only.</summary>
    Alert,
    /// <summary>Rate-limit the offending source.</summary>
    RateLimit,
    /// <summary>Lock out the offending source.</summary>
    Lock
}

/// <summary>
/// How a log drain transforms the client IP before a line leaves the platform.
/// </summary>
public enum EdgeIPRedactionMode
{
    /// <summary>Emit the full client IP.</summary>
    Full,
    /// <summary>Emit a network-truncated IP.</summary>
    Truncated,
    /// <summary>Emit a salted hash of the IP.</summary>
    Hashed,
    /// <summary>Omit the IP entirely.</summary>
    Omitted
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
/// Per-request vary dimensions that compose the cache key for a cache rule.
/// </summary>
public class EdgeCacheKey
{
    /// <summary>Query-string parameters that vary the cached entry.</summary>
    [JsonPropertyName("vary_query_params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? VaryQueryParams { get; set; }

    /// <summary>Request headers that vary the cached entry.</summary>
    [JsonPropertyName("vary_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? VaryHeaders { get; set; }

    /// <summary>Cookies that vary the cached entry.</summary>
    [JsonPropertyName("vary_cookies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? VaryCookies { get; set; }
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

    /// <summary>
    /// How long a stale entry may be served while a fresh copy is fetched in the
    /// background, in seconds.
    /// </summary>
    [JsonPropertyName("stale_while_revalidate_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StaleWhileRevalidateSeconds { get; set; }

    /// <summary>
    /// How long a stale entry may be served if the origin returns an error, in seconds.
    /// </summary>
    [JsonPropertyName("stale_if_error_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StaleIfErrorSeconds { get; set; }

    /// <summary>Per-request vary dimensions that compose the cache key.</summary>
    [JsonPropertyName("cache_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCacheKey? CacheKey { get; set; }

    /// <summary>
    /// Whether concurrent cache-miss requests for the same key are collapsed into a
    /// single origin fetch.
    /// </summary>
    [JsonPropertyName("request_collapsing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequestCollapsing { get; set; }
}

/// <summary>
/// Token-bucket rate limit enforced per PoP at the edge. RequestsPerSecond, Burst and
/// Key are customer-tunable; Backend, BackendAddress and NodeCount are platform-set and
/// only echoed on a response.
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

    /// <summary>Counter location. Empty is treated as in-process. Platform-set.</summary>
    [JsonPropertyName("backend")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRateLimitBackend? Backend { get; set; }

    /// <summary>Valkey host:port when Backend is Valkey; empty otherwise. Platform-set.</summary>
    [JsonPropertyName("backend_address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BackendAddress { get; set; }

    /// <summary>
    /// Number of serving nodes the in-process limit is spread across. Platform-set;
    /// empty/0 or 1 means the full limit applies per node.
    /// </summary>
    [JsonPropertyName("node_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NodeCount { get; set; }
}

/// <summary>
/// Matches a named request header's value against a regular expression for a custom WAF rule.
/// </summary>
public class EdgeWAFRuleHeaderMatch
{
    /// <summary>Header name to inspect.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>RE2 pattern the header value must match.</summary>
    [JsonPropertyName("value_pattern")]
    public string ValuePattern { get; set; } = string.Empty;
}

/// <summary>
/// A safe, structured per-app WAF rule. The customer supplies only opaque metadata and a
/// small set of match patterns, never raw SecRule text. All match fields are optional; at
/// least one is required, and when more than one is set they are ANDed.
/// </summary>
public class EdgeWAFRule
{
    /// <summary>Opaque rule name.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>Opaque rule description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>RE2 pattern the request URI must match.</summary>
    [JsonPropertyName("uri_pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UriPattern { get; set; }

    /// <summary>HTTP method the request must use.</summary>
    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; set; }

    /// <summary>Header match condition.</summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeWAFRuleHeaderMatch? Header { get; set; }

    /// <summary>CIDR the source IP must fall within.</summary>
    [JsonPropertyName("source_ip_cidr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceIpCidr { get; set; }

    /// <summary>What a matching request triggers.</summary>
    [JsonPropertyName("action")]
    public EdgeWAFRuleAction Action { get; set; }
}

/// <summary>
/// Redirects a request whose path exactly matches FromPath to ToUrl with an HTTP redirect
/// status (301, 302, 307, or 308; 0 means the default 302). It short-circuits at the edge
/// before WAF, cache, or origin.
/// </summary>
public class EdgeRedirectRule
{
    /// <summary>Exact request path that triggers the redirect.</summary>
    [JsonPropertyName("from_path")]
    public string FromPath { get; set; } = string.Empty;

    /// <summary>Target URL the client is redirected to.</summary>
    [JsonPropertyName("to_url")]
    public string ToUrl { get; set; } = string.Empty;

    /// <summary>Redirect status code (301, 302, 307, 308; 0 = default 302).</summary>
    [JsonPropertyName("status_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusCode { get; set; }
}

/// <summary>
/// One upstream the edge proxies an app's traffic to. FloatingIp is set only on the
/// platform-derived primary origin and is read-only.
/// </summary>
public class EdgeOrigin
{
    /// <summary>Floating IP of the platform-derived primary origin (read-only).</summary>
    [JsonPropertyName("floating_ip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FloatingIp { get; set; }

    /// <summary>Hostname or IP to dial.</summary>
    [JsonPropertyName("host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Host { get; set; }

    /// <summary>Upstream port.</summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>TLS SNI; defaults to the dial host when empty.</summary>
    [JsonPropertyName("sni")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sni { get; set; }

    /// <summary>Weight for weighted load balancing.</summary>
    [JsonPropertyName("weight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Weight { get; set; }

    /// <summary>Whether this is a backup origin used only when primaries fail.</summary>
    [JsonPropertyName("backup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Backup { get; set; }
}

/// <summary>
/// Matches a named request header for an ordered edge rule. Exactly one of Value (exact)
/// or Regex (RE2) is used; Value takes precedence when both are set.
/// </summary>
public class EdgeRuleHeaderMatch
{
    /// <summary>Header name to inspect.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Exact header value to match.</summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    /// <summary>RE2 pattern the header value must match.</summary>
    [JsonPropertyName("regex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Regex { get; set; }
}

/// <summary>
/// The ANDed set of conditions an ordered edge rule matches on. Every set condition must
/// hold; an empty match matches every request.
/// </summary>
public class EdgeRuleMatch
{
    /// <summary>Request path prefix to match.</summary>
    [JsonPropertyName("path_prefix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PathPrefix { get; set; }

    /// <summary>RE2 pattern the request path must match.</summary>
    [JsonPropertyName("path_regex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PathRegex { get; set; }

    /// <summary>HTTP methods to match.</summary>
    [JsonPropertyName("methods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Methods { get; set; }

    /// <summary>Header match condition.</summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRuleHeaderMatch? Header { get; set; }
}

/// <summary>
/// The closed-enum action a matched ordered edge rule takes. Only the fields relevant to
/// Type are used.
/// </summary>
public class EdgeRuleAction
{
    /// <summary>Which action this rule performs.</summary>
    [JsonPropertyName("type")]
    public EdgeRuleActionType Type { get; set; }

    /// <summary>Target URL for a Redirect action.</summary>
    [JsonPropertyName("redirect_to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RedirectTo { get; set; }

    /// <summary>Status code for a Redirect action.</summary>
    [JsonPropertyName("redirect_status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int RedirectStatus { get; set; }

    /// <summary>Request headers to set (SetHeader action).</summary>
    [JsonPropertyName("set_request_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? SetRequestHeaders { get; set; }

    /// <summary>Request headers to remove (SetHeader action).</summary>
    [JsonPropertyName("remove_request_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RemoveRequestHeaders { get; set; }

    /// <summary>Response headers to set (SetHeader action).</summary>
    [JsonPropertyName("set_response_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? SetResponseHeaders { get; set; }

    /// <summary>Response headers to remove (SetHeader action).</summary>
    [JsonPropertyName("remove_response_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RemoveResponseHeaders { get; set; }

    /// <summary>Rewritten request path (Rewrite action).</summary>
    [JsonPropertyName("rewrite")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Rewrite { get; set; }

    /// <summary>Status code for a Block action.</summary>
    [JsonPropertyName("block_status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BlockStatus { get; set; }

    /// <summary>Overriding upstream origin (OriginOverride action).</summary>
    [JsonPropertyName("origin_override")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOrigin? OriginOverride { get; set; }
}

/// <summary>
/// One entry in the additive, ordered, composable edge rules engine: a match plus a
/// closed-enum action. Rules are evaluated in ascending priority order (ties broken by
/// declared index).
/// </summary>
public class EdgeRule
{
    /// <summary>Opaque rule name.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>Evaluation priority (ascending).</summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Priority { get; set; }

    /// <summary>Match conditions.</summary>
    [JsonPropertyName("match")]
    public EdgeRuleMatch Match { get; set; } = new();

    /// <summary>Action taken on a match.</summary>
    [JsonPropertyName("action")]
    public EdgeRuleAction Action { get; set; } = new();
}

/// <summary>
/// Manipulates HTTP headers at the edge. RequestSet/RequestRemove apply to the request
/// forwarded to the origin; ResponseSet/ResponseRemove apply to the response returned to
/// the client.
/// </summary>
public class EdgeHeaderRules
{
    /// <summary>Request headers to set toward the origin.</summary>
    [JsonPropertyName("request_set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? RequestSet { get; set; }

    /// <summary>Request headers to remove before reaching the origin.</summary>
    [JsonPropertyName("request_remove")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RequestRemove { get; set; }

    /// <summary>Response headers to set toward the client.</summary>
    [JsonPropertyName("response_set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? ResponseSet { get; set; }

    /// <summary>Response headers to remove before returning to the client.</summary>
    [JsonPropertyName("response_remove")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ResponseRemove { get; set; }
}

/// <summary>
/// Per-app cross-origin resource sharing policy the edge enforces. AllowedOrigins is either
/// the single wildcard "*" (only when AllowCredentials is false) or a list of concrete
/// http(s) origins.
/// </summary>
public class EdgeCORS
{
    /// <summary>Origins allowed to make cross-origin requests.</summary>
    [JsonPropertyName("allowed_origins")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedOrigins { get; set; }

    /// <summary>HTTP methods allowed cross-origin.</summary>
    [JsonPropertyName("allowed_methods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedMethods { get; set; }

    /// <summary>Request headers allowed cross-origin.</summary>
    [JsonPropertyName("allowed_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedHeaders { get; set; }

    /// <summary>Response headers exposed to the browser.</summary>
    [JsonPropertyName("expose_headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ExposeHeaders { get; set; }

    /// <summary>Whether credentials (cookies, auth headers) are allowed cross-origin.</summary>
    [JsonPropertyName("allow_credentials")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AllowCredentials { get; set; }

    /// <summary>How long (seconds) a browser may cache the preflight response.</summary>
    [JsonPropertyName("max_age_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxAgeSeconds { get; set; }
}

/// <summary>
/// Puts an app behind a maintenance page at the edge. When Enabled, every client except
/// those whose connection IP is inside a BypassIp CIDR gets the maintenance response.
/// </summary>
public class EdgeMaintenance
{
    /// <summary>Whether maintenance mode is active.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Maintenance response status code (default 503).</summary>
    [JsonPropertyName("status_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusCode { get; set; }

    /// <summary>Maintenance response body.</summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; set; }

    /// <summary>CIDRs allowed to bypass the maintenance page.</summary>
    [JsonPropertyName("bypass_ips")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BypassIps { get; set; }
}

/// <summary>
/// Enables gzip response compression at the edge for one app. ExtraContentTypes adds
/// content-types beyond the runtime defaults.
/// </summary>
public class EdgeCompression
{
    /// <summary>Whether compression is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Additional content-types to compress.</summary>
    [JsonPropertyName("extra_content_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ExtraContentTypes { get; set; }
}

/// <summary>
/// Enables an HTTP Strict-Transport-Security response header at the edge for one app.
/// Preload requires IncludeSubdomains and a max-age of at least one year.
/// </summary>
public class EdgeHSTS
{
    /// <summary>Whether the HSTS header is sent.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>max-age directive value, in seconds.</summary>
    [JsonPropertyName("max_age_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxAgeSeconds { get; set; }

    /// <summary>Whether the includeSubDomains directive is set.</summary>
    [JsonPropertyName("include_subdomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IncludeSubdomains { get; set; }

    /// <summary>Whether the preload directive is set.</summary>
    [JsonPropertyName("preload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Preload { get; set; }
}

/// <summary>
/// Injects a per-request correlation id at the edge on both the request forwarded to the
/// origin and the response returned to the client. HeaderName empty defaults to X-Request-ID.
/// </summary>
public class EdgeRequestID
{
    /// <summary>Whether request-id injection is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Correlation-id header name (default X-Request-ID).</summary>
    [JsonPropertyName("header_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeaderName { get; set; }
}

/// <summary>
/// Routes a sticky subset of an app's traffic into a canary (B) arm at the edge. A request
/// is routed into the canary arm when it carries the cookie MatchCookie or the header
/// MatchHeader (exactly one is set) with the value MatchValue.
/// </summary>
public class EdgeCanary
{
    /// <summary>Whether canary routing is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Cookie name that routes a request into the canary arm.</summary>
    [JsonPropertyName("match_cookie")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MatchCookie { get; set; }

    /// <summary>Header name that routes a request into the canary arm.</summary>
    [JsonPropertyName("match_header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MatchHeader { get; set; }

    /// <summary>Value the match cookie or header must carry.</summary>
    [JsonPropertyName("match_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MatchValue { get; set; }

    /// <summary>Variant header name injected toward the origin (default X-Variant).</summary>
    [JsonPropertyName("variant_header_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VariantHeaderName { get; set; }

    /// <summary>Variant header value injected toward the origin (default canary).</summary>
    [JsonPropertyName("variant_header_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VariantHeaderValue { get; set; }
}

/// <summary>
/// Active (out-of-band) origin health probing. The edge issues a probe to Path every
/// IntervalSeconds, treating a probe that exceeds TimeoutSeconds or returns a status not
/// matching ExpectStatus as a failure.
/// </summary>
public class EdgeOriginHealthCheckActive
{
    /// <summary>Whether active probing is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Probe path.</summary>
    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    /// <summary>Probe interval, in seconds.</summary>
    [JsonPropertyName("interval_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int IntervalSeconds { get; set; }

    /// <summary>Probe timeout, in seconds.</summary>
    [JsonPropertyName("timeout_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int TimeoutSeconds { get; set; }

    /// <summary>Expected probe status code.</summary>
    [JsonPropertyName("expect_status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ExpectStatus { get; set; }
}

/// <summary>
/// Passive (in-band) origin health detection: an upstream that returns MaxFails responses
/// whose status is in UnhealthyStatus within a rolling FailDurationSeconds window is taken
/// out of rotation for that duration.
/// </summary>
public class EdgeOriginHealthCheckPassive
{
    /// <summary>Failures within the window before the origin is ejected.</summary>
    [JsonPropertyName("max_fails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxFails { get; set; }

    /// <summary>Rolling window and ejection duration, in seconds.</summary>
    [JsonPropertyName("fail_duration_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int FailDurationSeconds { get; set; }

    /// <summary>Status codes that count as failures.</summary>
    [JsonPropertyName("unhealthy_status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? UnhealthyStatus { get; set; }
}

/// <summary>
/// Per-app origin health-check policy the edge enforces on the upstream proxy. Either or
/// both of Active and Passive may be set.
/// </summary>
public class EdgeOriginHealthCheck
{
    /// <summary>Active probing config.</summary>
    [JsonPropertyName("active")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginHealthCheckActive? Active { get; set; }

    /// <summary>Passive detection config.</summary>
    [JsonPropertyName("passive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginHealthCheckPassive? Passive { get; set; }
}

/// <summary>
/// Per-app set of additional origins beyond the primary auto origin, with the load-balancing
/// policy and failover knobs.
/// </summary>
public class EdgeOriginPool
{
    /// <summary>Additional upstream origins.</summary>
    [JsonPropertyName("additional_origins")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeOrigin>? AdditionalOrigins { get; set; }

    /// <summary>Load-balancing policy across the combined upstream set.</summary>
    [JsonPropertyName("lb_policy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginLBPolicy? LBPolicy { get; set; }

    /// <summary>Total time to keep retrying an upstream request, in seconds.</summary>
    [JsonPropertyName("try_duration_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int TryDurationSeconds { get; set; }

    /// <summary>Number of retries on a failed upstream attempt.</summary>
    [JsonPropertyName("retries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Retries { get; set; }

    /// <summary>Upstream status codes that trigger a retry.</summary>
    [JsonPropertyName("retry_statuses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? RetryStatuses { get; set; }
}

/// <summary>
/// One inbound Basic Auth account on the settings request. Password is the PLAINTEXT
/// password the controller hashes and discards; it is write-only and never echoed. An empty
/// Password for an existing username keeps that account's stored hash.
/// </summary>
public class EdgeBasicAuthAccountRequest
{
    /// <summary>Account username.</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Plaintext password (write-only; never returned).</summary>
    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }
}

/// <summary>
/// Inbound Basic Auth setting on the settings request. Carries plaintext passwords the
/// controller hashes and discards; the stored document only ever carries the resulting hashes.
/// </summary>
public class EdgeBasicAuthRequest
{
    /// <summary>Whether Basic Auth is enforced.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>The inbound accounts.</summary>
    [JsonPropertyName("accounts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeBasicAuthAccountRequest>? Accounts { get; set; }
}

/// <summary>
/// One required JWT claim: the claim must be present and equal Value.
/// </summary>
public class EdgeJWTClaim
{
    /// <summary>Claim name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Required claim value.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// JWT bearer-token validation at the edge for the matched paths. Carries no secret: tokens
/// are validated against a JWKS URL or static public keys. Echoed verbatim on the response.
/// </summary>
public class EdgeJWTAuth
{
    /// <summary>Whether JWT validation is enforced.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Paths the validation applies to.</summary>
    [JsonPropertyName("paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Paths { get; set; }

    /// <summary>JWKS URL the signing keys are fetched from.</summary>
    [JsonPropertyName("jwks_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JwksUrl { get; set; }

    /// <summary>Static PEM-encoded public keys used to verify token signatures.</summary>
    [JsonPropertyName("public_keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? PublicKeys { get; set; }

    /// <summary>Required token issuer.</summary>
    [JsonPropertyName("issuer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Issuer { get; set; }

    /// <summary>Accepted token audiences.</summary>
    [JsonPropertyName("audiences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Audiences { get; set; }

    /// <summary>Claims that must be present with the given value.</summary>
    [JsonPropertyName("required_claims")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeJWTClaim>? RequiredClaims { get; set; }

    /// <summary>Header to forward validated claims to the origin under.</summary>
    [JsonPropertyName("forward_claims_header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ForwardClaimsHeader { get; set; }
}

/// <summary>
/// Signed-URL enforcement at the edge for the matched paths. SecretName is a reference to a
/// stored secret by name only; the secret value is never carried by this type. The response
/// view has the same shape (nothing is stripped because no secret value is ever stored here).
/// </summary>
public class EdgeSignedURLs
{
    /// <summary>Whether signed-URL enforcement is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Paths the enforcement applies to.</summary>
    [JsonPropertyName("paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Paths { get; set; }

    /// <summary>Name of the stored signing secret (reference only, never the value).</summary>
    [JsonPropertyName("secret_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecretName { get; set; }

    /// <summary>Validity window of a signed URL, in seconds.</summary>
    [JsonPropertyName("ttl_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int TtlSeconds { get; set; }

    /// <summary>Query parameter carrying the signature (default "sig").</summary>
    [JsonPropertyName("signature_param")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignatureParam { get; set; }

    /// <summary>Query parameter carrying the expiry (default "exp").</summary>
    [JsonPropertyName("expires_param")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpiresParam { get; set; }
}

/// <summary>
/// One inbound API key on the settings request. Key is the PLAINTEXT key the controller
/// hashes and discards; it is write-only and never echoed. RateTier optionally applies a
/// per-key rate limit.
/// </summary>
public class EdgeAPIKeyRequest
{
    /// <summary>Opaque key name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Plaintext API key (write-only; hashed server-side; never returned).</summary>
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    /// <summary>Optional per-key rate limit.</summary>
    [JsonPropertyName("rate_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRateLimit? RateTier { get; set; }
}

/// <summary>
/// API-key authentication at the edge for the matched paths. Carries plaintext keys the
/// controller hashes and discards; the stored document and response view never carry key
/// material.
/// </summary>
public class EdgeAPIKeyAuthRequest
{
    /// <summary>Whether API-key authentication is enforced.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Paths the enforcement applies to.</summary>
    [JsonPropertyName("paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Paths { get; set; }

    /// <summary>Where the key is read from (default Header).</summary>
    [JsonPropertyName("key_location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeApiKeyLocation? KeyLocation { get; set; }

    /// <summary>Header or query-parameter name the key is read from (default X-API-Key).</summary>
    [JsonPropertyName("key_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyName { get; set; }

    /// <summary>The inbound keys.</summary>
    [JsonPropertyName("keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeAPIKeyRequest>? Keys { get; set; }
}

/// <summary>
/// Non-secret view of one configured API key as echoed on the settings response. Carries no
/// hash and no plaintext.
/// </summary>
public class EdgeAPIKeyView
{
    /// <summary>Opaque key name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Per-key rate limit, if any.</summary>
    [JsonPropertyName("rate_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRateLimit? RateTier { get; set; }
}

/// <summary>
/// Non-secret view of the API-key authentication setting as echoed on the settings response.
/// Carries no key material.
/// </summary>
public class EdgeAPIKeyAuthView
{
    /// <summary>Whether API-key authentication is enforced.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Paths the enforcement applies to.</summary>
    [JsonPropertyName("paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Paths { get; set; }

    /// <summary>Where the key is read from.</summary>
    [JsonPropertyName("key_location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeApiKeyLocation? KeyLocation { get; set; }

    /// <summary>Header or query-parameter name the key is read from.</summary>
    [JsonPropertyName("key_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyName { get; set; }

    /// <summary>The configured keys, without key material.</summary>
    [JsonPropertyName("keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeAPIKeyView>? Keys { get; set; }
}

/// <summary>
/// One WAF rule exclusion: either suppress a managed rule by RuleId, suppress a Target, or
/// both. At least one field must be set.
/// </summary>
public class EdgeWAFExclusion
{
    /// <summary>Managed rule id to exclude.</summary>
    [JsonPropertyName("rule_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int RuleId { get; set; }

    /// <summary>Inspection target to exclude.</summary>
    [JsonPropertyName("target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Target { get; set; }
}

/// <summary>
/// Per-app DDoS mitigation profile applied at the edge.
/// </summary>
public class EdgeDDoSProfile
{
    /// <summary>Whether DDoS mitigation is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Per-source-IP request ceiling, in requests per second.</summary>
    [JsonPropertyName("per_ip_requests_per_second")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PerIpRequestsPerSecond { get; set; }

    /// <summary>Per-source-IP burst allowance above the sustained rate.</summary>
    [JsonPropertyName("per_ip_burst")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PerIpBurst { get; set; }

    /// <summary>Per-source-IP concurrent connection cap.</summary>
    [JsonPropertyName("per_ip_conn_cap")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PerIpConnCap { get; set; }
}

/// <summary>
/// Per-app bot-management policy applied at the edge.
/// </summary>
public class EdgeBotManagement
{
    /// <summary>Whether bot management is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Action taken on a request classified as a bot (default Log).</summary>
    [JsonPropertyName("action")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeBotAction? Action { get; set; }

    /// <summary>Whether known-bad-bot signatures are matched.</summary>
    [JsonPropertyName("known_bad_bots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool KnownBadBots { get; set; }

    /// <summary>Whether a rate-based heuristic classifier is applied.</summary>
    [JsonPropertyName("rate_based_heuristic")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RateBasedHeuristic { get; set; }
}

/// <summary>
/// Per-app account-takeover (ATO) protection applied to the configured auth paths.
/// </summary>
public class EdgeATOProtection
{
    /// <summary>Whether ATO protection is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Authentication paths the protection applies to.</summary>
    [JsonPropertyName("auth_paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AuthPaths { get; set; }

    /// <summary>Response status codes counted as auth failures (default [401, 403]).</summary>
    [JsonPropertyName("failure_status_codes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? FailureStatusCodes { get; set; }

    /// <summary>Per-source-IP failure threshold per minute.</summary>
    [JsonPropertyName("per_ip_threshold_per_min")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PerIpThresholdPerMin { get; set; }

    /// <summary>Per-username failure threshold per minute.</summary>
    [JsonPropertyName("per_username_threshold_per_min")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PerUsernameThresholdPerMin { get; set; }

    /// <summary>Form/JSON field carrying the username.</summary>
    [JsonPropertyName("username_field")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UsernameField { get; set; }

    /// <summary>Action taken when a threshold is exceeded (default Alert).</summary>
    [JsonPropertyName("action")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeAtoAction? Action { get; set; }
}

/// <summary>
/// Customer-tunable edge settings sent to PUT /app-services/{id}/edge/settings.
/// Domains and origin are platform-derived and cannot be set here. Each list/object field
/// replaces the stored value wholesale; an empty or null value clears the corresponding setting.
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

    /// <summary>Structured custom WAF rules.</summary>
    [JsonPropertyName("custom_waf_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeWAFRule>? CustomWAFRules { get; set; }

    /// <summary>CIDRs allowed access (an empty list clears the allow-list).</summary>
    [JsonPropertyName("ip_allow_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? IpAllowList { get; set; }

    /// <summary>CIDRs denied access.</summary>
    [JsonPropertyName("ip_deny_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? IpDenyList { get; set; }

    /// <summary>Exact-path redirect rules.</summary>
    [JsonPropertyName("redirects")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeRedirectRule>? Redirects { get; set; }

    /// <summary>Request/response header manipulation rules.</summary>
    [JsonPropertyName("header_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeHeaderRules? HeaderRules { get; set; }

    /// <summary>Cross-origin resource sharing policy.</summary>
    [JsonPropertyName("cors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCORS? CORS { get; set; }

    /// <summary>Maintenance-mode configuration.</summary>
    [JsonPropertyName("maintenance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeMaintenance? Maintenance { get; set; }

    /// <summary>Response compression configuration.</summary>
    [JsonPropertyName("compression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCompression? Compression { get; set; }

    /// <summary>Maximum allowed request body size, in bytes (0 = unset).</summary>
    [JsonPropertyName("max_request_body_bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long MaxRequestBodyBytes { get; set; }

    /// <summary>HTTP methods permitted at the edge.</summary>
    [JsonPropertyName("allowed_methods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedMethods { get; set; }

    /// <summary>Basic Auth configuration (carries plaintext passwords; write-only).</summary>
    [JsonPropertyName("basic_auth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeBasicAuthRequest? BasicAuth { get; set; }

    /// <summary>Exact paths blocked at the edge.</summary>
    [JsonPropertyName("blocked_paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BlockedPaths { get; set; }

    /// <summary>HTTP Strict-Transport-Security configuration.</summary>
    [JsonPropertyName("hsts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeHSTS? HSTS { get; set; }

    /// <summary>Per-request correlation-id injection.</summary>
    [JsonPropertyName("request_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRequestID? RequestID { get; set; }

    /// <summary>Sticky canary routing configuration.</summary>
    [JsonPropertyName("canary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCanary? Canary { get; set; }

    /// <summary>Origin health-check policy.</summary>
    [JsonPropertyName("health_check")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginHealthCheck? HealthCheck { get; set; }

    /// <summary>Additional origins and load-balancing/failover policy.</summary>
    [JsonPropertyName("origin_pool")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginPool? OriginPool { get; set; }

    /// <summary>
    /// Opt the app into staged per-node/per-PoP config rollouts instead of immediate
    /// fleet-wide dispatch.
    /// </summary>
    [JsonPropertyName("canary_rollout_enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CanaryRolloutEnabled { get; set; }

    /// <summary>
    /// Additive, ordered, composable rules engine list. Replaces the stored list wholesale;
    /// an empty or omitted list clears all rules.
    /// </summary>
    [JsonPropertyName("rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeRule>? Rules { get; set; }

    /// <summary>JWT bearer-token validation configuration.</summary>
    [JsonPropertyName("jwt_auth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeJWTAuth? JwtAuth { get; set; }

    /// <summary>Signed-URL enforcement configuration.</summary>
    [JsonPropertyName("signed_urls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeSignedURLs? SignedUrls { get; set; }

    /// <summary>API-key authentication configuration (carries plaintext keys; write-only).</summary>
    [JsonPropertyName("api_key_auth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeAPIKeyAuthRequest? ApiKeyAuth { get; set; }

    /// <summary>OWASP CRS paranoia level (1..4; 0 = platform default PL1).</summary>
    [JsonPropertyName("waf_paranoia_level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int WafParanoiaLevel { get; set; }

    /// <summary>Managed WAF rule exclusions.</summary>
    [JsonPropertyName("waf_rule_exclusions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeWAFExclusion>? WafRuleExclusions { get; set; }

    /// <summary>DDoS mitigation profile.</summary>
    [JsonPropertyName("ddos_profile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeDDoSProfile? DdosProfile { get; set; }

    /// <summary>Bot-management policy.</summary>
    [JsonPropertyName("bot_management")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeBotManagement? BotManagement { get; set; }

    /// <summary>Account-takeover protection.</summary>
    [JsonPropertyName("ato_protection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeATOProtection? AtoProtection { get; set; }
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
/// Customer-tunable edge settings as returned after an update (and on GET .../edge/settings).
/// Basic Auth password hashes are never echoed; only the enabled flag and usernames are
/// returned. signed_urls and api_key_auth are projected to their non-secret view shapes.
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

    /// <summary>Active custom WAF rules.</summary>
    [JsonPropertyName("custom_waf_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeWAFRule>? CustomWAFRules { get; set; }

    /// <summary>Active IP allow-list CIDRs.</summary>
    [JsonPropertyName("ip_allow_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? IpAllowList { get; set; }

    /// <summary>Active IP deny-list CIDRs.</summary>
    [JsonPropertyName("ip_deny_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? IpDenyList { get; set; }

    /// <summary>Active exact-path redirect rules.</summary>
    [JsonPropertyName("redirects")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeRedirectRule>? Redirects { get; set; }

    /// <summary>Active header manipulation rules.</summary>
    [JsonPropertyName("header_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeHeaderRules? HeaderRules { get; set; }

    /// <summary>Active CORS policy.</summary>
    [JsonPropertyName("cors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCORS? CORS { get; set; }

    /// <summary>Active maintenance-mode configuration.</summary>
    [JsonPropertyName("maintenance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeMaintenance? Maintenance { get; set; }

    /// <summary>Active compression configuration.</summary>
    [JsonPropertyName("compression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCompression? Compression { get; set; }

    /// <summary>Maximum allowed request body size, in bytes (0 = unset).</summary>
    [JsonPropertyName("max_request_body_bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long MaxRequestBodyBytes { get; set; }

    /// <summary>Active HTTP methods permitted at the edge.</summary>
    [JsonPropertyName("allowed_methods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedMethods { get; set; }

    /// <summary>Whether Basic Auth is enforced.</summary>
    [JsonPropertyName("basic_auth_enabled")]
    public bool BasicAuthEnabled { get; set; }

    /// <summary>Configured Basic Auth usernames (no passwords or hashes are returned).</summary>
    [JsonPropertyName("basic_auth_usernames")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BasicAuthUsernames { get; set; }

    /// <summary>Active exact blocked paths.</summary>
    [JsonPropertyName("blocked_paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BlockedPaths { get; set; }

    /// <summary>Active HSTS configuration.</summary>
    [JsonPropertyName("hsts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeHSTS? HSTS { get; set; }

    /// <summary>Active request-id injection configuration.</summary>
    [JsonPropertyName("request_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRequestID? RequestID { get; set; }

    /// <summary>Active canary routing configuration.</summary>
    [JsonPropertyName("canary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeCanary? Canary { get; set; }

    /// <summary>Active origin health-check policy.</summary>
    [JsonPropertyName("health_check")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginHealthCheck? HealthCheck { get; set; }

    /// <summary>Active origin pool.</summary>
    [JsonPropertyName("origin_pool")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeOriginPool? OriginPool { get; set; }

    /// <summary>Whether the app opts into staged per-node/per-PoP config rollouts.</summary>
    [JsonPropertyName("canary_rollout_enabled")]
    public bool CanaryRolloutEnabled { get; set; }

    /// <summary>Active ordered rules engine list; empty means no rules.</summary>
    [JsonPropertyName("rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeRule>? Rules { get; set; }

    /// <summary>Active JWT bearer-token validation configuration.</summary>
    [JsonPropertyName("jwt_auth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeJWTAuth? JwtAuth { get; set; }

    /// <summary>Active signed-URL enforcement (non-secret view).</summary>
    [JsonPropertyName("signed_urls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeSignedURLs? SignedUrls { get; set; }

    /// <summary>Active API-key authentication (non-secret view; no key material).</summary>
    [JsonPropertyName("api_key_auth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeAPIKeyAuthView? ApiKeyAuth { get; set; }

    /// <summary>Active OWASP CRS paranoia level (1..4; 0 = platform default PL1).</summary>
    [JsonPropertyName("waf_paranoia_level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int WafParanoiaLevel { get; set; }

    /// <summary>Active managed WAF rule exclusions.</summary>
    [JsonPropertyName("waf_rule_exclusions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeWAFExclusion>? WafRuleExclusions { get; set; }

    /// <summary>Active DDoS mitigation profile.</summary>
    [JsonPropertyName("ddos_profile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeDDoSProfile? DdosProfile { get; set; }

    /// <summary>Active bot-management policy.</summary>
    [JsonPropertyName("bot_management")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeBotManagement? BotManagement { get; set; }

    /// <summary>Active account-takeover protection.</summary>
    [JsonPropertyName("ato_protection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeATOProtection? AtoProtection { get; set; }

    /// <summary>Config version the fleet will converge on after this update.</summary>
    [JsonPropertyName("config_version")]
    public long ConfigVersion { get; set; }
}

/// <summary>
/// Body of POST /app-services/{id}/edge/cache/purge. Request exactly one form: All drops
/// every cached entry for the app on the fleet, or Paths invalidates the cached entries under
/// each listed absolute path.
/// </summary>
public class EdgeCachePurgeRequest
{
    /// <summary>Drop every cached entry for the app.</summary>
    [JsonPropertyName("all")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool All { get; set; }

    /// <summary>Absolute paths whose cached entries are invalidated.</summary>
    [JsonPropertyName("paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Paths { get; set; }
}

/// <summary>
/// The rolling purge plan a cache-purge request started. The purge flushes nodes one at a
/// time in the background, so the response reports the plan rather than the completed result.
/// </summary>
public class EdgeCachePurgeResponse
{
    /// <summary>Number of nodes the purge will roll across.</summary>
    [JsonPropertyName("planned_nodes")]
    public int PlannedNodes { get; set; }

    /// <summary>IDs of the nodes the purge will roll across.</summary>
    [JsonPropertyName("node_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? NodeIds { get; set; }

    /// <summary>Whether the purge rolls one node at a time.</summary>
    [JsonPropertyName("rolling")]
    public bool Rolling { get; set; }
}

/// <summary>
/// One (path, count) entry of a top-paths or suspicious-paths list in the edge analytics summary.
/// </summary>
public class EdgeMetricsTopPath
{
    /// <summary>Request path.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>Request count for the path over the window.</summary>
    [JsonPropertyName("count")]
    public long Count { get; set; }
}

/// <summary>
/// Request total broken down by HTTP status class.
/// </summary>
public class EdgeStatusClassCounts
{
    /// <summary>2xx response count.</summary>
    [JsonPropertyName("2xx")]
    public long C2xx { get; set; }

    /// <summary>3xx response count.</summary>
    [JsonPropertyName("3xx")]
    public long C3xx { get; set; }

    /// <summary>4xx response count.</summary>
    [JsonPropertyName("4xx")]
    public long C4xx { get; set; }

    /// <summary>5xx response count.</summary>
    [JsonPropertyName("5xx")]
    public long C5xx { get; set; }
}

/// <summary>
/// Cache hit/miss summary with the derived hit ratio.
/// </summary>
public class EdgeCacheCounts
{
    /// <summary>Cache-hit count.</summary>
    [JsonPropertyName("hit")]
    public long Hit { get; set; }

    /// <summary>Cache-miss count.</summary>
    [JsonPropertyName("miss")]
    public long Miss { get; set; }

    /// <summary>Hit ratio (hits / (hits + misses)).</summary>
    [JsonPropertyName("hit_ratio")]
    public double HitRatio { get; set; }
}

/// <summary>
/// Latency percentiles (milliseconds) estimated from the request latency histogram.
/// </summary>
public class EdgeLatencyPercentiles
{
    /// <summary>50th-percentile latency, in milliseconds.</summary>
    [JsonPropertyName("p50")]
    public double P50 { get; set; }

    /// <summary>95th-percentile latency, in milliseconds.</summary>
    [JsonPropertyName("p95")]
    public double P95 { get; set; }

    /// <summary>99th-percentile latency, in milliseconds.</summary>
    [JsonPropertyName("p99")]
    public double P99 { get; set; }
}

/// <summary>
/// Per-scope security/threat summary: the WAF detection total plus the observed top paths
/// matching credential-scanner shapes.
/// </summary>
public class EdgeAnalyticsThreat
{
    /// <summary>Total WAF detections over the window.</summary>
    [JsonPropertyName("waf_detections_total")]
    public long WafDetectionsTotal { get; set; }

    /// <summary>Top paths matching credential-scanner shapes.</summary>
    [JsonPropertyName("suspicious_paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeMetricsTopPath>? SuspiciousPaths { get; set; }
}

/// <summary>
/// Folded edge analytics for one scope (the app total or one PoP) over the window. Zone is
/// empty for the app-wide total.
/// </summary>
public class EdgeAnalyticsSummary
{
    /// <summary>Zone slug for a PoP scope; empty for the app-wide total.</summary>
    [JsonPropertyName("zone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zone { get; set; }

    /// <summary>Total requests over the window.</summary>
    [JsonPropertyName("requests_total")]
    public long RequestsTotal { get; set; }

    /// <summary>Request totals by HTTP status class.</summary>
    [JsonPropertyName("by_status_class")]
    public EdgeStatusClassCounts ByStatusClass { get; set; } = new();

    /// <summary>Error-rate percentage over the window.</summary>
    [JsonPropertyName("error_rate_pct")]
    public double ErrorRatePct { get; set; }

    /// <summary>Cache hit/miss summary.</summary>
    [JsonPropertyName("cache")]
    public EdgeCacheCounts Cache { get; set; } = new();

    /// <summary>Total rate-limited requests over the window.</summary>
    [JsonPropertyName("rate_limited_total")]
    public long RateLimitedTotal { get; set; }

    /// <summary>Total WAF detections over the window.</summary>
    [JsonPropertyName("waf_detections_total")]
    public long WafDetectionsTotal { get; set; }

    /// <summary>WAF detection counts keyed by rule id.</summary>
    [JsonPropertyName("waf_by_rule")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, long>? WafByRule { get; set; }

    /// <summary>Latency percentiles.</summary>
    [JsonPropertyName("latency_ms")]
    public EdgeLatencyPercentiles LatencyMs { get; set; } = new();

    /// <summary>Top requested paths.</summary>
    [JsonPropertyName("top_paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeMetricsTopPath>? TopPaths { get; set; }

    /// <summary>Security/threat summary.</summary>
    [JsonPropertyName("threat")]
    public EdgeAnalyticsThreat Threat { get; set; } = new();
}

/// <summary>
/// The GET /app-services/{id}/edge/analytics response: an account-scoped, server-aggregated
/// edge analytics summary for one app over a time window, folded across the app's PoPs with a
/// per-PoP breakdown.
/// </summary>
public class EdgeAnalytics
{
    /// <summary>Window length, in minutes.</summary>
    [JsonPropertyName("window_minutes")]
    public int WindowMinutes { get; set; }

    /// <summary>App-wide total summary.</summary>
    [JsonPropertyName("total")]
    public EdgeAnalyticsSummary Total { get; set; } = new();

    /// <summary>Per-PoP summaries.</summary>
    [JsonPropertyName("pops")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeAnalyticsSummary>? PoPs { get; set; }
}

/// <summary>
/// Per-drain privacy policy applied to every access log line before export. Authorization and
/// Cookie are always dropped regardless of HeaderAllowList.
/// </summary>
public class EdgeRedactionPolicy
{
    /// <summary>How the client IP is transformed before export.</summary>
    [JsonPropertyName("ip_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeIPRedactionMode? IpMode { get; set; }

    /// <summary>Salt used when IpMode is Hashed.</summary>
    [JsonPropertyName("ip_hash_salt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IpHashSalt { get; set; }

    /// <summary>Whether the query string is stripped from logged URLs.</summary>
    [JsonPropertyName("strip_query_string")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StripQueryString { get; set; }

    /// <summary>Request headers permitted in exported lines.</summary>
    [JsonPropertyName("header_allow_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? HeaderAllowList { get; set; }
}

/// <summary>
/// Streams an app's per-request edge access logs to a customer destination. The destination
/// configuration is write-only and never returned.
/// </summary>
public class EdgeLogDrain
{
    /// <summary>Unique identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>App service this drain belongs to.</summary>
    [JsonPropertyName("app_service_id")]
    public string AppServiceId { get; set; } = string.Empty;

    /// <summary>Drain name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Drain description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Destination type (e.g. "s3", "webhook").</summary>
    [JsonPropertyName("destination_type")]
    public string DestinationType { get; set; } = string.Empty;

    /// <summary>Privacy policy applied before export.</summary>
    [JsonPropertyName("redaction_policy")]
    public EdgeRedactionPolicy RedactionPolicy { get; set; } = new();

    /// <summary>Whether the drain is enabled.</summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>Export interval, in seconds.</summary>
    [JsonPropertyName("export_interval_seconds")]
    public int ExportIntervalSeconds { get; set; }

    /// <summary>ISO-8601 timestamp of the last successful export.</summary>
    [JsonPropertyName("last_export_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastExportAt { get; set; }

    /// <summary>Detail of the last export error, if any.</summary>
    [JsonPropertyName("last_export_error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastExportError { get; set; }

    /// <summary>Number of consecutive export failures.</summary>
    [JsonPropertyName("consecutive_failures")]
    public int ConsecutiveFailures { get; set; }

    /// <summary>ISO-8601 timestamp when this drain was created.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp of the last change.</summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Creates an edge access-log drain. Configuration is destination-specific (s3:
/// endpoint/region/bucket/prefix/access_key_id/secret_access_key; webhook:
/// url/auth_header_name/auth_header_value).
/// </summary>
public class CreateEdgeLogDrainRequest
{
    /// <summary>Drain name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Drain description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Destination type (e.g. "s3", "webhook").</summary>
    [JsonPropertyName("destination_type")]
    public string DestinationType { get; set; } = string.Empty;

    /// <summary>Destination-specific configuration (write-only).</summary>
    [JsonPropertyName("configuration")]
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>Privacy policy applied before export.</summary>
    [JsonPropertyName("redaction_policy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRedactionPolicy? RedactionPolicy { get; set; }

    /// <summary>Whether the drain starts enabled.</summary>
    [JsonPropertyName("is_enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsEnabled { get; set; }

    /// <summary>Export interval, in seconds.</summary>
    [JsonPropertyName("export_interval_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ExportIntervalSeconds { get; set; }
}

/// <summary>
/// Partial update of an edge access-log drain; omitted fields keep their value.
/// </summary>
public class UpdateEdgeLogDrainRequest
{
    /// <summary>New drain name.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>New drain description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>New destination type.</summary>
    [JsonPropertyName("destination_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DestinationType { get; set; }

    /// <summary>New destination-specific configuration.</summary>
    [JsonPropertyName("configuration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>New privacy policy.</summary>
    [JsonPropertyName("redaction_policy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRedactionPolicy? RedactionPolicy { get; set; }

    /// <summary>New enabled state.</summary>
    [JsonPropertyName("is_enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsEnabled { get; set; }

    /// <summary>New export interval, in seconds.</summary>
    [JsonPropertyName("export_interval_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ExportIntervalSeconds { get; set; }
}

/// <summary>
/// Reports whether a drain's destination is reachable.
/// </summary>
public class EdgeLogDrainTestResult
{
    /// <summary>Whether the destination is reachable.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>Error detail when the test failed.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}

/// <summary>
/// One entry in the append-only edge config version history. The live edge configuration is
/// the source of truth for what is active; this history is the immutable audit trail and the
/// source a rollback restores from.
/// </summary>
public class EdgeConfigVersion
{
    /// <summary>Version number.</summary>
    [JsonPropertyName("version")]
    public long Version { get; set; }

    /// <summary>Hash of the version's config.</summary>
    [JsonPropertyName("config_hash")]
    public string ConfigHash { get; set; } = string.Empty;

    /// <summary>
    /// What produced this version: "reconcile" (platform recompute bump), "settings"
    /// (customer settings write), or "rollback" (restore of a prior version's subset).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>User that initiated the change, when attributable; null for reconciler bumps.</summary>
    [JsonPropertyName("created_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedBy { get; set; }

    /// <summary>ISO-8601 timestamp when this version was created.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Whether this version is the currently active (live) one.</summary>
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    /// <summary>For a rollback version, the version whose subset it restored; null otherwise.</summary>
    [JsonPropertyName("rolled_back_from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? RolledBackFrom { get; set; }
}

/// <summary>
/// The GET /app-services/{id}/edge/versions response: the app's edge config version history
/// (newest first, bounded) and the live active version.
/// </summary>
public class EdgeConfigVersions
{
    /// <summary>The live active version.</summary>
    [JsonPropertyName("active_version")]
    public long ActiveVersion { get; set; }

    /// <summary>Version history, newest first.</summary>
    [JsonPropertyName("versions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EdgeConfigVersion>? Versions { get; set; }
}

/// <summary>
/// Names the version to roll back to. Supply exactly one of ToVersion (an explicit positive
/// version) or To set to "previous" (the version immediately before the active one).
/// </summary>
public class EdgeRollbackRequest
{
    /// <summary>Explicit version to roll back to.</summary>
    [JsonPropertyName("to_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long ToVersion { get; set; }

    /// <summary>Set to "previous" to roll back to the version before the active one.</summary>
    [JsonPropertyName("to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? To { get; set; }
}

/// <summary>
/// Reports the new active version a rollback produced. The rollback writes a NEW forward
/// version restoring the target's customer-settable subset; it never mutates the history.
/// </summary>
public class EdgeRollbackResponse
{
    /// <summary>New live active version.</summary>
    [JsonPropertyName("active_version")]
    public long ActiveVersion { get; set; }

    /// <summary>Version whose subset was restored.</summary>
    [JsonPropertyName("rolled_back_from")]
    public long RolledBackFrom { get; set; }

    /// <summary>Source label of the new version ("rollback").</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// One staged edge config rollout. A rollout stages a new config version to a canary subset
/// (one node, or one PoP) first, then either promotes it to the rest of the fleet or aborts.
/// </summary>
public class EdgeRollout
{
    /// <summary>Unique identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The config version being staged.</summary>
    [JsonPropertyName("target_version")]
    public long TargetVersion { get; set; }

    /// <summary>
    /// One of "canary" (held on the subset), "promoting" (fanning out), "promoted" (whole
    /// fleet converged), or "aborted" (the rest was never given the version).
    /// </summary>
    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    /// <summary>"node" (CanarySelector is a VM UUID) or "pop" (CanarySelector is a zone code).</summary>
    [JsonPropertyName("canary_scope")]
    public string CanaryScope { get; set; } = string.Empty;

    /// <summary>The canary node UUID or zone code.</summary>
    [JsonPropertyName("canary_selector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanarySelector { get; set; }

    /// <summary>ISO-8601 timestamp when the rollout started.</summary>
    [JsonPropertyName("started_at")]
    public string StartedAt { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp of the last change.</summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp when the rollout was promoted, if applicable.</summary>
    [JsonPropertyName("promoted_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromotedAt { get; set; }

    /// <summary>ISO-8601 timestamp when the rollout was aborted, if applicable.</summary>
    [JsonPropertyName("aborted_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AbortedAt { get; set; }

    /// <summary>Reason the rollout was aborted, if applicable.</summary>
    [JsonPropertyName("abort_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AbortReason { get; set; }
}

/// <summary>
/// The GET /app-services/{id}/edge/rollout response: the app's current (or most recent)
/// rollout. Active reports whether the rollout is in a non-terminal phase; Rollout is null
/// when the app has never had one.
/// </summary>
public class EdgeRolloutStatus
{
    /// <summary>Whether the rollout is in a non-terminal phase (canary or promoting).</summary>
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    /// <summary>The current or most recent rollout; null when the app has never had one.</summary>
    [JsonPropertyName("rollout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EdgeRollout? Rollout { get; set; }
}

/// <summary>
/// Carries an optional operator note recorded as a rollout's abort reason. An empty Reason
/// records a default "manual abort" note.
/// </summary>
public class EdgeRolloutAbortRequest
{
    /// <summary>Operator note recorded as the abort reason.</summary>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
}
