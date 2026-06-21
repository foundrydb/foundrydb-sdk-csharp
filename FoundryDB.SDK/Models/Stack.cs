using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// A stack template describing a pre-composed set of platform resources that
/// can be launched as a single unit. Retrieved via <c>StacksApi.ListStackTemplatesAsync</c>.
/// </summary>
public class StackTemplate
{
    /// <summary>Machine-readable template identifier (e.g. "rag-chatbot-starter").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name (e.g. "Launch a RAG chatbot").</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description of the stack's purpose and components.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Semantic version of the template (e.g. "1.0.0").</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>Indicative cost breakdown for this template.</summary>
    [JsonPropertyName("cost_preview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StackCostPreview? CostPreview { get; set; }
}

/// <summary>
/// Estimated monthly cost for launching a stack, broken down by resource.
/// Returned by <c>StacksApi.PreviewStackAsync</c> and embedded in
/// <see cref="StackTemplate.CostPreview"/>.
/// </summary>
public class StackCostPreview
{
    /// <summary>Machine-readable template identifier this preview applies to.</summary>
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>ISO 4217 currency code for all monetary values (e.g. "EUR").</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>Sum of all line items' monthly costs.</summary>
    [JsonPropertyName("monthly_total")]
    public decimal MonthlyTotal { get; set; }

    /// <summary>Per-resource cost breakdown.</summary>
    [JsonPropertyName("line_items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<StackCostLineItem>? LineItems { get; set; }

    /// <summary>
    /// Human-readable warnings about assumptions or caveats in this estimate
    /// (e.g. "Inference costs depend on usage volume.").
    /// </summary>
    [JsonPropertyName("warnings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Warnings { get; set; }
}

/// <summary>
/// One line in a <see cref="StackCostPreview"/>, representing the estimated
/// cost for a single resource in the stack.
/// </summary>
public class StackCostLineItem
{
    /// <summary>
    /// Symbolic name identifying the resource within the template
    /// (e.g. "pg_primary", "files_bucket", "inference_proxy").
    /// </summary>
    [JsonPropertyName("symbolic_name")]
    public string SymbolicName { get; set; } = string.Empty;

    /// <summary>
    /// Resource kind discriminator: "database", "files", "inference", or "app".
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Human-readable description of this line item.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Estimated monthly cost for this resource in <see cref="StackCostPreview.Currency"/>.</summary>
    [JsonPropertyName("monthly_cost")]
    public decimal MonthlyCost { get; set; }

    /// <summary>
    /// When true, this figure is a maximum ceiling rather than a fixed cost
    /// (e.g. usage-based inference spend).
    /// </summary>
    [JsonPropertyName("is_ceiling")]
    public bool IsCeiling { get; set; }
}

/// <summary>
/// A launched stack instance. A stack provisions and wires together multiple
/// platform resources (databases, file buckets, inference proxies, app
/// containers) defined by a template. Poll status until "Running" or "Failed".
/// </summary>
public class Stack
{
    /// <summary>Unique stack identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name given at launch time.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Template used to build this stack (e.g. "rag-chatbot-starter").</summary>
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>Version of the template that was resolved at launch time.</summary>
    [JsonPropertyName("template_version")]
    public string TemplateVersion { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status. One of: Pending, Provisioning, Wiring, Running,
    /// RollingBack, Failed, Deleting, Deleted.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Human-readable detail about the current status (e.g. current step or error).</summary>
    [JsonPropertyName("status_detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusDetail { get; set; }

    /// <summary>
    /// Public HTTPS URL at which the stack's primary app is reachable once Running.
    /// </summary>
    [JsonPropertyName("endpoint_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointUrl { get; set; }

    /// <summary>Estimated monthly cost in EUR across all provisioned resources.</summary>
    [JsonPropertyName("estimated_monthly_cost")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? EstimatedMonthlyCost { get; set; }

    /// <summary>Organisation this stack belongs to (when created under an org).</summary>
    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationId { get; set; }

    /// <summary>Individual resources provisioned as part of this stack.</summary>
    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<StackResource>? Resources { get; set; }

    /// <summary>ISO-8601 timestamp when the stack was launched.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last status change.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// One resource that was provisioned as part of a <see cref="Stack"/>.
/// Each entry maps a symbolic template role to a concrete platform service.
/// </summary>
public class StackResource
{
    /// <summary>Unique resource record identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>ID of the stack this resource belongs to.</summary>
    [JsonPropertyName("stack_id")]
    public string StackId { get; set; } = string.Empty;

    /// <summary>
    /// Symbolic role name within the template
    /// (e.g. "pg_primary", "files_bucket", "inference_proxy", "chat_app").
    /// </summary>
    [JsonPropertyName("symbolic_name")]
    public string SymbolicName { get; set; } = string.Empty;

    /// <summary>
    /// Resource kind discriminator: "database", "files", "inference", or "app".
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// ID of the underlying platform service (managed database, file service,
    /// inference proxy, or app service) that fulfils this role.
    /// </summary>
    [JsonPropertyName("service_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceId { get; set; }

    /// <summary>
    /// External reference ID used by some resource kinds (e.g. a bucket name or
    /// inference key reference).
    /// </summary>
    [JsonPropertyName("ref_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefId { get; set; }

    /// <summary>
    /// Provisioning status for this individual resource. One of: Pending,
    /// Provisioning, Wiring, Running, RollingBack, Failed, Deleting, Deleted.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Human-readable detail about the current resource status.</summary>
    [JsonPropertyName("status_detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusDetail { get; set; }

    /// <summary>
    /// Symbolic names of other resources in the stack that must reach Running
    /// before this resource is provisioned.
    /// </summary>
    [JsonPropertyName("depends_on")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DependsOn { get; set; }

    /// <summary>Provisioning order within the stack (lower values are provisioned first).</summary>
    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    /// <summary>ISO-8601 timestamp when this resource record was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last status change for this resource.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

// ----- Request models -----

/// <summary>
/// Request body for previewing the estimated cost of a stack before launching it.
/// </summary>
public class PreviewStackRequest
{
    /// <summary>Template to preview (e.g. "rag-chatbot-starter").</summary>
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;
}

/// <summary>
/// Request body for launching a new stack.
/// </summary>
public class LaunchStackRequest
{
    /// <summary>Human-readable display name for this stack instance.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Template to use when building the stack (e.g. "rag-chatbot-starter").</summary>
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Organisation to create the stack under. Overrides the client-level default
    /// when set. This property is NOT serialised into the JSON request body.
    /// </summary>
    [JsonIgnore]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Accepted maximum monthly cost in EUR. The platform rejects the launch when
    /// the computed cost exceeds this value, preventing surprise bills. Required.
    /// </summary>
    [JsonPropertyName("accepted_monthly_cost")]
    public decimal AcceptedMonthlyCost { get; set; }

    /// <summary>
    /// Optional per-resource overrides keyed by symbolic name. Use to customise
    /// resource parameters such as compute plan or storage size for individual
    /// components without forking the template.
    /// </summary>
    [JsonPropertyName("overrides")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Overrides { get; set; }
}
