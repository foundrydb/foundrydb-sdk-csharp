using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

// ----- Visibility and publication status constants -----

/// <summary>
/// Visibility scope of a custom stack template.
/// </summary>
public static class StackTemplateVisibility
{
    /// <summary>Visible only to the creating user.</summary>
    public const string Private = "private";

    /// <summary>Visible to all members of the owning organisation.</summary>
    public const string OrgShared = "org_shared";

    /// <summary>Visible to all users (published to the marketplace).</summary>
    public const string Public = "public";
}

/// <summary>
/// Publication status of a custom stack template on the marketplace.
/// </summary>
public static class StackTemplatePublicationStatus
{
    /// <summary>Template is in draft; not submitted for review.</summary>
    public const string Draft = "draft";

    /// <summary>Template has been submitted for marketplace review.</summary>
    public const string Submitted = "submitted";

    /// <summary>Template has been approved and is pending final publication.</summary>
    public const string Approved = "approved";

    /// <summary>Template is live on the marketplace.</summary>
    public const string Published = "published";

    /// <summary>Marketplace review rejected this template.</summary>
    public const string Rejected = "rejected";

    /// <summary>Template was previously published and has since been unpublished.</summary>
    public const string Unpublished = "unpublished";
}

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

    /// <summary>
    /// UUID of a custom template used to build this stack. Present when the stack was
    /// launched from a user-authored template rather than a built-in one.
    /// </summary>
    [JsonPropertyName("template_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateId { get; set; }

    /// <summary>
    /// ID of the organisation that published the source template (marketplace stacks only).
    /// </summary>
    [JsonPropertyName("source_publisher_org_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourcePublisherOrgId { get; set; }

    /// <summary>
    /// UUID of the original custom template that was published to the marketplace and
    /// used to build this stack. Set when the stack originates from a marketplace listing.
    /// </summary>
    [JsonPropertyName("source_template_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceTemplateId { get; set; }

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

    /// <summary>
    /// UUID of a custom template to preview. Use instead of <see cref="TemplateName"/>
    /// when previewing a user-authored template.
    /// </summary>
    [JsonPropertyName("template_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateId { get; set; }
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
    /// UUID of a custom template to launch. Use instead of <see cref="TemplateName"/>
    /// when launching a user-authored or marketplace template.
    /// </summary>
    [JsonPropertyName("template_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateId { get; set; }

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

// ----- Custom template models -----

/// <summary>
/// Describes one resource slot in a custom stack template descriptor.
/// </summary>
public class StackDescriptorResource
{
    /// <summary>
    /// Symbolic name for this resource slot within the template
    /// (e.g. "pg_primary", "files_bucket").
    /// </summary>
    [JsonPropertyName("symbolic_name")]
    public string SymbolicName { get; set; } = string.Empty;

    /// <summary>
    /// Resource kind: "database", "files", "inference", or "app".
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in cost previews and resource lists.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Symbolic names of other resource slots that must reach Running before
    /// this slot is provisioned.
    /// </summary>
    [JsonPropertyName("depends_on")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DependsOn { get; set; }

    /// <summary>
    /// Default provisioning parameters for this resource (e.g. plan, storage size,
    /// database type, app image). Callers may override these at launch time.
    /// </summary>
    [JsonPropertyName("defaults")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Defaults { get; set; }
}

/// <summary>
/// The resource composition descriptor for a custom stack template.
/// </summary>
public class StackDescriptor
{
    /// <summary>Ordered list of resources this template provisions.</summary>
    [JsonPropertyName("resources")]
    public List<StackDescriptorResource> Resources { get; set; } = new();

    /// <summary>
    /// Wiring rules that inject one resource's outputs into another resource's
    /// environment at launch time.
    /// </summary>
    [JsonPropertyName("wiring")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Dictionary<string, string>>? Wiring { get; set; }
}

/// <summary>
/// A user-authored custom stack template. Returned by the template CRUD endpoints.
/// </summary>
public class CustomStackTemplate
{
    /// <summary>Unique template identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Machine-readable identifier (slug) for the template.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description of the template's purpose and components.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Semantic version string (e.g. "1.0.0").</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Visibility scope. One of the <see cref="StackTemplateVisibility"/> constants:
    /// "private", "org_shared", or "public".
    /// </summary>
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = StackTemplateVisibility.Private;

    /// <summary>
    /// Marketplace publication status. One of the <see cref="StackTemplatePublicationStatus"/>
    /// constants: "draft", "submitted", "approved", "published", "rejected", "unpublished".
    /// </summary>
    [JsonPropertyName("publication_status")]
    public string PublicationStatus { get; set; } = StackTemplatePublicationStatus.Draft;

    /// <summary>Resource composition descriptor defining what this template provisions.</summary>
    [JsonPropertyName("descriptor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StackDescriptor? Descriptor { get; set; }

    /// <summary>Organisation that owns this template.</summary>
    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationId { get; set; }

    /// <summary>Indicative cost breakdown for this template.</summary>
    [JsonPropertyName("cost_preview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StackCostPreview? CostPreview { get; set; }

    /// <summary>
    /// Optional rejection reason from marketplace review (set when
    /// <see cref="PublicationStatus"/> is "rejected").
    /// </summary>
    [JsonPropertyName("rejection_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RejectionReason { get; set; }

    /// <summary>ISO-8601 timestamp when the template was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Request body for creating or updating a custom stack template.
/// </summary>
public class CustomTemplateRequest
{
    /// <summary>Machine-readable identifier (slug) for the template.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description of the template's purpose and components.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Semantic version string (e.g. "1.0.0").</summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>
    /// Visibility scope. One of the <see cref="StackTemplateVisibility"/> constants.
    /// Defaults to "private" when omitted.
    /// </summary>
    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; set; }

    /// <summary>Resource composition descriptor defining what this template provisions.</summary>
    [JsonPropertyName("descriptor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StackDescriptor? Descriptor { get; set; }
}

// ----- Stack upgrade models -----

/// <summary>
/// Describes a single resource change within a stack upgrade plan.
/// </summary>
public class ResourceChange
{
    /// <summary>
    /// Symbolic name of the resource that will be changed
    /// (e.g. "pg_primary", "inference_proxy").
    /// </summary>
    [JsonPropertyName("symbolic_name")]
    public string SymbolicName { get; set; } = string.Empty;

    /// <summary>
    /// Change category. Typical values: "upgrade", "add", "remove", "no_change".
    /// </summary>
    [JsonPropertyName("change_type")]
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>Template version the resource is currently at, when applicable.</summary>
    [JsonPropertyName("from_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromVersion { get; set; }

    /// <summary>Template version the resource will be at after the upgrade.</summary>
    [JsonPropertyName("to_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToVersion { get; set; }

    /// <summary>
    /// Human-readable description of what will change (e.g. "PostgreSQL 16 to 17").
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// When true, this resource will have a brief interruption during the upgrade.
    /// </summary>
    [JsonPropertyName("requires_restart")]
    public bool RequiresRestart { get; set; }
}

/// <summary>
/// Preview of the changes that will be applied when upgrading a stack to a newer
/// template version. Returned by <c>StacksApi.PreviewStackUpgradeAsync</c>.
/// </summary>
public class StackUpgradePlan
{
    /// <summary>UUID of the stack that will be upgraded.</summary>
    [JsonPropertyName("stack_id")]
    public string StackId { get; set; } = string.Empty;

    /// <summary>Template version the stack is currently running.</summary>
    [JsonPropertyName("current_version")]
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>Template version the stack will be upgraded to.</summary>
    [JsonPropertyName("target_version")]
    public string TargetVersion { get; set; } = string.Empty;

    /// <summary>Per-resource changes included in this upgrade.</summary>
    [JsonPropertyName("resource_changes")]
    public List<ResourceChange> ResourceChanges { get; set; } = new();

    /// <summary>
    /// Human-readable warnings about the upgrade (e.g. "Primary database will restart.").
    /// </summary>
    [JsonPropertyName("warnings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Warnings { get; set; }

    /// <summary>
    /// Estimated total monthly cost after the upgrade in EUR. May differ from the
    /// current cost when new resources are added or removed.
    /// </summary>
    [JsonPropertyName("estimated_monthly_cost")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? EstimatedMonthlyCost { get; set; }
}

/// <summary>
/// Represents an upgrade migration that has been applied to a stack.
/// </summary>
public class StackMigration
{
    /// <summary>Unique migration record identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>UUID of the stack this migration was applied to.</summary>
    [JsonPropertyName("stack_id")]
    public string StackId { get; set; } = string.Empty;

    /// <summary>Template version the stack was at before this migration.</summary>
    [JsonPropertyName("from_version")]
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>Template version the stack was upgraded to by this migration.</summary>
    [JsonPropertyName("to_version")]
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>
    /// Migration status. One of: Pending, Applying, Completed, Failed, RollingBack, RolledBack.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Human-readable detail about the current migration status.</summary>
    [JsonPropertyName("status_detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusDetail { get; set; }

    /// <summary>Per-resource changes applied by this migration.</summary>
    [JsonPropertyName("resource_changes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ResourceChange>? ResourceChanges { get; set; }

    /// <summary>ISO-8601 timestamp when this migration was initiated.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last status change for this migration.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Request body for applying a stack upgrade to a specific template version.
/// </summary>
public class ApplyStackUpgradeRequest
{
    /// <summary>Target template version to upgrade to (e.g. "2.0.0").</summary>
    [JsonPropertyName("target_version")]
    public string TargetVersion { get; set; } = string.Empty;
}

/// <summary>
/// Request body for previewing a stack upgrade.
/// </summary>
public class PreviewStackUpgradeRequest
{
    /// <summary>Target template version to preview upgrading to (e.g. "2.0.0").</summary>
    [JsonPropertyName("target_version")]
    public string TargetVersion { get; set; } = string.Empty;
}
