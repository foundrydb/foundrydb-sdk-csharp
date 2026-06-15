using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// Container configuration for an app service: the OCI image to run, the port
/// it listens on, and its environment. A changed image reference or environment
/// triggers a zero-downtime blue/green redeploy.
/// </summary>
public class AppContainerConfig
{
    /// <summary>OCI image reference (e.g. "registry.example.com/myapp:v1.2.3").</summary>
    [JsonPropertyName("image_ref")]
    public string ImageRef { get; set; } = string.Empty;

    /// <summary>TCP port the container listens on.</summary>
    [JsonPropertyName("container_port")]
    public int ContainerPort { get; set; }

    /// <summary>Environment variables injected into the container.</summary>
    [JsonPropertyName("env")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>
    /// Extra hostnames the app is served on beyond {name}.foundrydb.com.
    /// Up to 5; foundrydb.com subdomains are not allowed.
    /// </summary>
    [JsonPropertyName("custom_domains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? CustomDomains { get; set; }

    /// <summary>
    /// Username for pulling from a private registry.
    /// Pair with <see cref="RegistryPassword"/> (write-only: never returned by the API).
    /// </summary>
    [JsonPropertyName("registry_username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryUsername { get; set; }

    /// <summary>
    /// Password for pulling from a private registry. Write-only: accepted on
    /// create/update, stored in the platform secret store, never returned.
    /// </summary>
    [JsonPropertyName("registry_password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryPassword { get; set; }

    /// <summary>
    /// HTTP path probed during blue/green redeploys and at runtime to determine
    /// container health (e.g. "/healthz"). When empty the platform falls back
    /// to a TCP connect on <see cref="ContainerPort"/>.
    /// </summary>
    [JsonPropertyName("health_check_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HealthCheckPath { get; set; }

    /// <summary>How often the health probe runs, in seconds.</summary>
    [JsonPropertyName("health_check_interval_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HealthCheckIntervalSeconds { get; set; }

    /// <summary>How long a single probe may take before it counts as a failure, in seconds.</summary>
    [JsonPropertyName("health_check_timeout_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HealthCheckTimeoutSeconds { get; set; }

    /// <summary>
    /// Number of consecutive successful probes required before a new container
    /// is promoted to serve traffic.
    /// </summary>
    [JsonPropertyName("health_check_healthy_threshold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HealthCheckHealthyThreshold { get; set; }
}

/// <summary>
/// A customer application container hosted on the platform. When attached to a
/// managed service, the platform peers the private networks, opens the service's
/// port to the app's subnet, and injects connection credentials as environment
/// variables. Reachable over HTTPS at {name}.foundrydb.com.
/// </summary>
public class AppService
{
    /// <summary>Unique service identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Owner user ID.</summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Organisation this service belongs to (if any).</summary>
    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationId { get; set; }

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Service kind discriminator (always "app" for app services).</summary>
    [JsonPropertyName("service_kind")]
    public string ServiceKind { get; set; } = string.Empty;

    /// <summary>Current lifecycle status (e.g. "Running", "Pending", "Deploying").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Deployment zone (e.g. "se-sto1").</summary>
    [JsonPropertyName("zone")]
    public string Zone { get; set; } = string.Empty;

    /// <summary>Compute plan name (e.g. "tier-2").</summary>
    [JsonPropertyName("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Data disk size in GB.</summary>
    [JsonPropertyName("storage_size_gb")]
    public int StorageSizeGb { get; set; }

    /// <summary>Storage performance tier ("standard" or "maxiops").</summary>
    [JsonPropertyName("storage_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StorageTier { get; set; }

    /// <summary>Allowed CIDR blocks for inbound traffic.</summary>
    [JsonPropertyName("allowed_cidrs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedCidrs { get; set; }

    /// <summary>Current container configuration.</summary>
    [JsonPropertyName("app_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppContainerConfig? AppConfig { get; set; }

    /// <summary>
    /// IDs of managed services (databases or other apps) currently attached.
    /// </summary>
    [JsonPropertyName("attached_service_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AttachedServiceIds { get; set; }

    /// <summary>ISO-8601 timestamp when the app service was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// One phase of an app deploy or redeploy, captured on the agent.
/// Status is one of "ok", "failed", or "info".
/// </summary>
public class AppDeployStep
{
    /// <summary>Step name (e.g. "image_start", "health_probe", "ingress_cutover", "teardown").</summary>
    [JsonPropertyName("step")]
    public string Step { get; set; } = string.Empty;

    /// <summary>Outcome of the step: "ok", "failed", or "info".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Short human-readable summary of the step outcome.</summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>Extended detail for the step (e.g. error output).</summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    /// <summary>Timestamp when this step started.</summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Wall-clock duration of the step in milliseconds. Zero for steps not yet completed.</summary>
    [JsonPropertyName("duration_ms")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long DurationMs { get; set; }
}

/// <summary>
/// A single revision in an app service's deploy history: the image and
/// configuration that were rolled out at <see cref="CreatedAt"/>. The newest
/// entry reflects the currently serving container. Pass an older entry's
/// <see cref="Id"/> to <c>AppServicesApi.RollbackAsync</c> to redeploy it.
/// </summary>
public class AppDeployment
{
    /// <summary>Unique deployment identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>ID of the app service this revision belongs to.</summary>
    [JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>OCI image reference that was deployed.</summary>
    [JsonPropertyName("image_ref")]
    public string ImageRef { get; set; } = string.Empty;

    /// <summary>TCP port the container listened on.</summary>
    [JsonPropertyName("container_port")]
    public int ContainerPort { get; set; }

    /// <summary>Environment variables active for this revision.</summary>
    [JsonPropertyName("env")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>Custom domains active for this revision.</summary>
    [JsonPropertyName("custom_domains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? CustomDomains { get; set; }

    /// <summary>Registry username used for this revision's image pull.</summary>
    [JsonPropertyName("registry_username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryUsername { get; set; }

    /// <summary>
    /// What triggered this deployment (e.g. "config update", "rollback").
    /// </summary>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    /// <summary>
    /// Ordered list of deploy steps the agent executed for this revision
    /// (image start, health probe, ingress cutover, previous-color teardown).
    /// Distinct from runtime container logs. Empty for revisions deployed
    /// before the platform captured deploy steps.
    /// </summary>
    [JsonPropertyName("deploy_logs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AppDeployStep>? DeployLogs { get; set; }

    /// <summary>ISO-8601 timestamp when this revision was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// ----- Request models -----

/// <summary>
/// Request body for creating a new app service.
/// </summary>
public class CreateAppServiceRequest
{
    /// <summary>Human-readable display name for the app service.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Compute plan (e.g. "tier-2").</summary>
    [JsonPropertyName("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Deployment zone (e.g. "se-sto1"). Defaults to the platform's primary zone when omitted.</summary>
    [JsonPropertyName("zone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zone { get; set; }

    /// <summary>Container image and runtime configuration.</summary>
    [JsonPropertyName("app_config")]
    public AppContainerConfig AppConfig { get; set; } = new();

    /// <summary>Data disk size in GB (minimum 10).</summary>
    [JsonPropertyName("storage_size_gb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StorageSizeGb { get; set; }

    /// <summary>Storage performance tier ("standard" or "maxiops").</summary>
    [JsonPropertyName("storage_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StorageTier { get; set; }

    /// <summary>Managed service IDs to attach at creation time.</summary>
    [JsonPropertyName("attached_service_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AttachedServiceIds { get; set; }

    /// <summary>Organisation to create the service under. Overrides the client-level default.</summary>
    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Request body for updating an app service's container configuration.
/// </summary>
public class UpdateAppServiceRequest
{
    /// <summary>New container configuration. A changed image or environment triggers a blue/green redeploy.</summary>
    [JsonPropertyName("app_config")]
    public AppContainerConfig AppConfig { get; set; } = new();
}

/// <summary>
/// Request body for attaching a managed service to a running app.
/// </summary>
public class AttachServiceRequest
{
    /// <summary>ID of the managed service (database or app) to attach.</summary>
    [JsonPropertyName("attached_service_id")]
    public string AttachedServiceId { get; set; } = string.Empty;
}

/// <summary>
/// Request body for scaling an app service to a new compute tier.
/// </summary>
public class ScaleAppServiceRequest
{
    /// <summary>Target compute plan (e.g. "tier-4").</summary>
    [JsonPropertyName("plan_name")]
    public string PlanName { get; set; } = string.Empty;
}

/// <summary>
/// Request body for rolling back an app service to an earlier deployment.
/// </summary>
public class RollbackAppServiceRequest
{
    /// <summary>ID of the deployment to redeploy (from <c>AppServicesApi.ListDeploymentsAsync</c>).</summary>
    [JsonPropertyName("deployment_id")]
    public string DeploymentId { get; set; } = string.Empty;
}
