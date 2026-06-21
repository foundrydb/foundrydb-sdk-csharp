using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// Summary of a companion-app attachment on a managed service, returned by
/// <c>AttachmentsApi.ListAsync</c>.
/// </summary>
public class AttachmentSummary
{
    /// <summary>Unique attachment identifier (UUID).</summary>
    [JsonPropertyName("attachment_id")]
    public string AttachmentId { get; set; } = string.Empty;

    /// <summary>ID of the underlying app service hosting the companion app.</summary>
    [JsonPropertyName("app_service_id")]
    public string AppServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Companion-app kind (e.g. "metabase", "directus", "hasura", "nocodb",
    /// "open-webui").
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Human-readable display name of the companion app.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Lifecycle status of the companion app (e.g. "Running", "Pending").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether the wiring between the companion app and the parent service is
    /// complete (e.g. "wired", "pending", "error").
    /// </summary>
    [JsonPropertyName("wiring_status")]
    public string WiringStatus { get; set; } = string.Empty;

    /// <summary>Public HTTPS URL at which the companion app is reachable.</summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
}

/// <summary>
/// Entry in the attachment catalog describing one available companion-app kind.
/// Retrieve the full catalog via <c>AttachmentsApi.GetCatalogAsync</c>.
/// </summary>
public class AttachmentCatalogEntry
{
    /// <summary>
    /// Machine-readable kind identifier (e.g. "metabase", "directus", "hasura",
    /// "nocodb", "open-webui").
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Human-readable display name (e.g. "Metabase").</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description of the companion app.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Category grouping (e.g. "analytics", "cms", "api", "ai").</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Recommended compute plan for this kind (e.g. "tier-2").</summary>
    [JsonPropertyName("default_plan")]
    public string DefaultPlan { get; set; } = string.Empty;

    /// <summary>
    /// Database engine kinds this companion app can attach to
    /// (e.g. ["postgresql", "mysql"]).
    /// </summary>
    [JsonPropertyName("requires_parent_kinds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RequiresParentKinds { get; set; }
}

/// <summary>
/// Admin credentials for a companion app, returned by
/// <c>AttachmentsApi.GetCredentialsAsync</c>.
/// </summary>
public class AttachmentCredentials
{
    /// <summary>Email address of the auto-provisioned admin account.</summary>
    [JsonPropertyName("admin_email")]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Password for the admin account.</summary>
    [JsonPropertyName("admin_password")]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Additional generated credentials specific to the companion-app kind
    /// (e.g. API keys, tokens).
    /// </summary>
    [JsonPropertyName("generated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Generated { get; set; }

    /// <summary>URL of the companion app's login page.</summary>
    [JsonPropertyName("login_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LoginUrl { get; set; }
}

// ----- Request models -----

/// <summary>
/// Request body for attaching a companion app to a managed service.
/// </summary>
public class CreateAttachmentRequest
{
    /// <summary>
    /// Companion-app kind to deploy (e.g. "metabase", "directus", "hasura",
    /// "nocodb", "open-webui").
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Compute plan for the companion-app service (e.g. "tier-2").</summary>
    [JsonPropertyName("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain prefix for the companion app's public URL
    /// (e.g. "analytics" produces analytics.foundrydb.com).
    /// When omitted the platform generates a name automatically.
    /// </summary>
    [JsonPropertyName("subdomain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subdomain { get; set; }
}
