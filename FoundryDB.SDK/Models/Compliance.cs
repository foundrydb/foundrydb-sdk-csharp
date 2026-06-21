using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// The result of a single control assertion within a compliance packet.
/// </summary>
public class ControlAssertion
{
    /// <summary>Unique identifier for the control (e.g. "CC6.1").</summary>
    [JsonPropertyName("control_id")]
    public string ControlId { get; set; } = string.Empty;

    /// <summary>Human-readable title of the control.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Structured assertion text describing the platform behaviour.</summary>
    [JsonPropertyName("assertion")]
    public string Assertion { get; set; } = string.Empty;

    /// <summary>Assertion status (e.g. "pass", "fail", "not_applicable").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Evidence reference identifiers supporting this assertion.</summary>
    [JsonPropertyName("evidence_refs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? EvidenceRefs { get; set; }
}

/// <summary>
/// Audit log summary included in a compliance packet.
/// </summary>
public class ComplianceAuditLogSummary
{
    /// <summary>Retention policy description (e.g. "90 days").</summary>
    [JsonPropertyName("retention_policy")]
    public string RetentionPolicy { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp of the oldest retained audit log entry.</summary>
    [JsonPropertyName("oldest_entry_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldestEntryAt { get; set; }

    /// <summary>Total number of audit log entries in the retention window.</summary>
    [JsonPropertyName("entry_count")]
    public long EntryCount { get; set; }
}

/// <summary>
/// High-level summary statistics in a compliance packet.
/// </summary>
public class CompliancePacketSummary
{
    /// <summary>Number of in-scope managed services.</summary>
    [JsonPropertyName("service_count")]
    public int ServiceCount { get; set; }

    /// <summary>Whether all in-scope services store data exclusively in EU regions.</summary>
    [JsonPropertyName("all_services_eu_residency")]
    public bool AllServicesEuResidency { get; set; }

    /// <summary>Audit log statistics.</summary>
    [JsonPropertyName("audit_log")]
    public ComplianceAuditLogSummary AuditLog { get; set; } = new();
}

/// <summary>
/// Organisation metadata embedded in a compliance packet.
/// </summary>
public class ComplianceOrganizationInfo
{
    /// <summary>Organisation UUID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Organisation display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Billing contact email address.</summary>
    [JsonPropertyName("billing_email")]
    public string BillingEmail { get; set; } = string.Empty;

    /// <summary>Country code of the organisation (ISO 3166-1 alpha-2).</summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; set; }
}

/// <summary>
/// A signed compliance evidence packet for a specific framework and audit period.
/// </summary>
public class CompliancePacket
{
    /// <summary>Schema version of this packet format (e.g. "1.0").</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>Compliance framework identifier ("soc2" or "gdpr_ropa").</summary>
    [JsonPropertyName("framework")]
    public string Framework { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp when this packet was generated.</summary>
    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = string.Empty;

    /// <summary>ISO-8601 date marking the start of the audit period.</summary>
    [JsonPropertyName("period_start")]
    public string PeriodStart { get; set; } = string.Empty;

    /// <summary>ISO-8601 date marking the end of the audit period.</summary>
    [JsonPropertyName("period_end")]
    public string PeriodEnd { get; set; } = string.Empty;

    /// <summary>Organisation metadata at the time of generation.</summary>
    [JsonPropertyName("organization")]
    public ComplianceOrganizationInfo Organization { get; set; } = new();

    /// <summary>Description of the services and data in scope for this report.</summary>
    [JsonPropertyName("scope_boundary")]
    public string ScopeBoundary { get; set; } = string.Empty;

    /// <summary>Per-control assertions and evidence for the selected framework.</summary>
    [JsonPropertyName("controls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ControlAssertion>? Controls { get; set; }

    /// <summary>High-level summary statistics.</summary>
    [JsonPropertyName("summary")]
    public CompliancePacketSummary Summary { get; set; } = new();
}

/// <summary>
/// Cryptographic signature over a compliance packet.
/// </summary>
public class CompliancePacketSignature
{
    /// <summary>Signing algorithm (e.g. "RS256").</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Identifier of the signing key used.</summary>
    [JsonPropertyName("key_id")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Base64-encoded signature value.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>Hex-encoded SHA-256 digest of the canonical packet JSON.</summary>
    [JsonPropertyName("canonical_sha256")]
    public string CanonicalSha256 { get; set; } = string.Empty;
}

/// <summary>
/// A compliance packet bundled with its cryptographic signature.
/// </summary>
public class CompliancePacketResponse
{
    /// <summary>The evidence packet.</summary>
    [JsonPropertyName("packet")]
    public CompliancePacket Packet { get; set; } = new();

    /// <summary>Signature over the packet.</summary>
    [JsonPropertyName("signature")]
    public CompliancePacketSignature Signature { get; set; } = new();
}

/// <summary>
/// Response returned when a new compliance report is generated.
/// </summary>
public class GenerateComplianceReportResponse
{
    /// <summary>UUID of the newly created compliance report.</summary>
    [JsonPropertyName("report_id")]
    public string ReportId { get; set; } = string.Empty;

    /// <summary>The evidence packet embedded in the report.</summary>
    [JsonPropertyName("packet")]
    public CompliancePacket Packet { get; set; } = new();

    /// <summary>Cryptographic signature over the packet.</summary>
    [JsonPropertyName("signature")]
    public CompliancePacketSignature Signature { get; set; } = new();
}

/// <summary>
/// Summary record for a previously generated compliance report.
/// </summary>
public class ComplianceReportRecord
{
    /// <summary>Unique identifier of the report.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Organisation UUID this report belongs to.</summary>
    [JsonPropertyName("organization_id")]
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>Compliance framework ("soc2" or "gdpr_ropa").</summary>
    [JsonPropertyName("framework")]
    public string Framework { get; set; } = string.Empty;

    /// <summary>Schema version of the packet format.</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>ISO-8601 date marking the start of the audit period.</summary>
    [JsonPropertyName("period_start")]
    public string PeriodStart { get; set; } = string.Empty;

    /// <summary>ISO-8601 date marking the end of the audit period.</summary>
    [JsonPropertyName("period_end")]
    public string PeriodEnd { get; set; } = string.Empty;

    /// <summary>ISO-8601 timestamp when this report was generated.</summary>
    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = string.Empty;

    /// <summary>User or system that triggered generation.</summary>
    [JsonPropertyName("generated_by")]
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>Identifier of the signing key used for this report.</summary>
    [JsonPropertyName("signing_key_id")]
    public string SigningKeyId { get; set; } = string.Empty;

    /// <summary>Signing algorithm (e.g. "RS256").</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Current lifecycle status of the report (e.g. "ready", "generating").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Whether a pre-rendered PDF version is available for download.</summary>
    [JsonPropertyName("has_pdf")]
    public bool HasPdf { get; set; }
}

/// <summary>
/// A public compliance signing key published at the well-known endpoint.
/// </summary>
public class ComplianceSigningKey
{
    /// <summary>Unique identifier for this signing key.</summary>
    [JsonPropertyName("key_id")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Algorithm this key is used with (e.g. "RS256").</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>PEM-encoded public key.</summary>
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Whether this key is currently active for signing new reports.</summary>
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    /// <summary>ISO-8601 timestamp when this key was retired, if applicable.</summary>
    [JsonPropertyName("retired_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RetiredAt { get; set; }
}

/// <summary>
/// The full set of public compliance signing keys published at /.well-known/compliance-signing-keys.
/// </summary>
public class ComplianceSigningKeySet
{
    /// <summary>Signing algorithm used by all keys in this set.</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>All current and retired signing keys.</summary>
    [JsonPropertyName("keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ComplianceSigningKey>? Keys { get; set; }
}
