using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.AppServices;

/// <summary>
/// Operations on app services (container hosting). All endpoints are under
/// <c>/app-services</c>.
/// </summary>
public class AppServicesApi
{
    private readonly FoundryDBClient _client;

    internal AppServicesApi(FoundryDBClient client)
    {
        _client = client;
    }

    // ----- CRUD -----

    /// <summary>
    /// Returns all app services visible to the authenticated user.
    /// </summary>
    /// <param name="orgId">Optional organisation ID to scope the request.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<AppService>> ListAsync(string? orgId = null, CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/app-services", orgId, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<AppService>();
        if (doc.RootElement.TryGetProperty("app_services", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var app = JsonSerializer.Deserialize<AppService>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (app is not null) result.Add(app);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns the app service with the given UUID, or <see langword="null"/> when it does not exist (404).
    /// </summary>
    /// <param name="id">App service UUID.</param>
    /// <param name="orgId">Optional organisation ID to scope the request.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService?> GetAsync(string id, string? orgId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("App service ID must not be empty.", nameof(id));
        try
        {
            var json = await _client.GetAsync($"/app-services/{id}", orgId, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions);
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Deploys a new app container and returns its initial state. The service is
    /// created in Pending status; poll <see cref="WaitForRunningAsync"/> until the
    /// container is live and reachable over HTTPS.
    /// </summary>
    /// <param name="req">Creation parameters including name, plan, and container config.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> CreateAsync(CreateAppServiceRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("App service name must not be empty.", nameof(req));

        var orgId = req.OrganizationId ?? _client.Config.OrganizationId;
        var json = await _client.PostAsync("/app-services", req, orgId, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    /// <summary>
    /// Applies a new container configuration and returns the updated state. A changed
    /// image or environment triggers an asynchronous zero-downtime blue/green redeploy;
    /// poll <see cref="WaitForRunningAsync"/> until the app returns to Running.
    /// </summary>
    /// <param name="id">App service UUID.</param>
    /// <param name="req">New container configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> UpdateAsync(string id, UpdateAppServiceRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("App service ID must not be empty.", nameof(id));
        ArgumentNullException.ThrowIfNull(req);

        var json = await _client.PatchAsync($"/app-services/{id}", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    /// <summary>
    /// Initiates deletion of the app service. The platform reverts attached
    /// services' firewall rules and tears down network peerings. A 404 response
    /// is treated as success (idempotent).
    /// </summary>
    /// <param name="id">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("App service ID must not be empty.", nameof(id));
        try
        {
            await _client.DeleteAsync($"/app-services/{id}", orgId: null, ct).ConfigureAwait(false);
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            // Idempotent: treat 404 as success.
        }
    }

    // ----- Attachments -----

    /// <summary>
    /// Attaches a managed service to a running app and returns the updated app
    /// service. The target may be a database or another app (east-west
    /// app-to-app). The platform peers the private networks, opens the target's
    /// port to the app's subnet, and rolls a zero-downtime redeploy so the
    /// injected environment is updated: a database injects connection
    /// credentials; an app injects <c>MDB_&lt;NAME&gt;_HOST/PORT/URL</c> for
    /// plain-HTTP calls over the private SDN (no credentials, no
    /// <c>DATABASE_URL</c>). An app supports up to five attachments (databases
    /// and apps combined). The target must be Running, owned by the same user,
    /// in the app's peering region, and not the app itself. Poll
    /// <see cref="WaitForRunningAsync"/> until the app returns to Running.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="attachedServiceId">ID of the database or app to attach.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> AttachAsync(string appServiceId, string attachedServiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(attachedServiceId)) throw new ArgumentException("Attached service ID must not be empty.", nameof(attachedServiceId));

        var req = new AttachServiceRequest { AttachedServiceId = attachedServiceId };
        var json = await _client.PostAsync($"/app-services/{appServiceId}/attachments", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    /// <summary>
    /// Removes an attachment from a running app and returns the updated app
    /// service. The platform reverts the firewall opening, tears down the
    /// peering, and rolls a zero-downtime redeploy to remove the injected
    /// environment. Poll <see cref="WaitForRunningAsync"/> until the app
    /// returns to Running.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="attachmentId">Attachment UUID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> DetachAsync(string appServiceId, string attachmentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(attachmentId)) throw new ArgumentException("Attachment ID must not be empty.", nameof(attachmentId));

        var json = await _client.DeleteWithBodyAsync($"/app-services/{appServiceId}/attachments/{attachmentId}", orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    // ----- Deployments -----

    /// <summary>
    /// Returns the deploy history of an app service, newest first. Each entry
    /// is a previously rolled-out image and configuration; inspect
    /// <see cref="AppDeployment.DeployLogs"/> for the ordered steps the agent
    /// executed (image start, health probe, ingress cutover, previous-color
    /// teardown). Pass an entry's ID to <see cref="RollbackAsync"/> to redeploy it.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<AppDeployment>> ListDeploymentsAsync(string appServiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));

        var json = await _client.GetAsync($"/app-services/{appServiceId}/deployments", orgId: null, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<AppDeployment>();
        if (doc.RootElement.TryGetProperty("deployments", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var dep = JsonSerializer.Deserialize<AppDeployment>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (dep is not null) result.Add(dep);
            }
        }
        return result;
    }

    /// <summary>
    /// Redeploys an earlier deployment via a zero-downtime blue/green swap and
    /// returns the updated app service. Poll <see cref="WaitForRunningAsync"/>
    /// until it returns to Running.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="deploymentId">Deployment UUID from <see cref="ListDeploymentsAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> RollbackAsync(string appServiceId, string deploymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(deploymentId)) throw new ArgumentException("Deployment ID must not be empty.", nameof(deploymentId));

        var req = new RollbackAppServiceRequest { DeploymentId = deploymentId };
        var json = await _client.PostAsync($"/app-services/{appServiceId}/rollback", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    // ----- Scale / restart -----

    /// <summary>
    /// Changes the compute tier of an app service and returns the updated state.
    /// Scaling up is a zero-downtime hot resize; scaling down may require a brief
    /// restart. Poll <see cref="WaitForRunningAsync"/> until it returns to Running.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="planName">Target compute plan (e.g. "tier-4").</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> ScaleAsync(string appServiceId, string planName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("Plan name must not be empty.", nameof(planName));

        var req = new ScaleAppServiceRequest { PlanName = planName };
        var json = await _client.PostAsync($"/app-services/{appServiceId}/scale", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain an app service.");
    }

    /// <summary>
    /// Restarts the app's running container in place.
    /// </summary>
    /// <param name="appServiceId">App service UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RestartAsync(string appServiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId)) throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));
        await _client.PostAsync($"/app-services/{appServiceId}/restart", payload: null, orgId: null, ct).ConfigureAwait(false);
    }

    // ----- Polling -----

    /// <summary>
    /// Polls the app service until it reaches "Running" status or the timeout
    /// expires. Throws <see cref="TimeoutException"/> if not reached in time.
    /// Throws <see cref="FoundryDBException"/> immediately on terminal failure
    /// states. Poll interval is 10 seconds.
    /// </summary>
    /// <param name="id">App service UUID.</param>
    /// <param name="timeout">Maximum wait duration. Defaults to 10 minutes.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> WaitForRunningAsync(string id, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("App service ID must not be empty.", nameof(id));

        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(10));
        var pollInterval = TimeSpan.FromSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var app = await GetAsync(id, orgId: null, ct).ConfigureAwait(false);
            if (app is null)
                throw new FoundryDBException(404, "Not Found", $"App service '{id}' not found while waiting for Running status.");

            var status = app.Status.ToLowerInvariant();
            if (status == "running")
                return app;

            if (status.Contains("failed") || status == "error")
                throw new FoundryDBException(0, "Terminal State", $"App service '{id}' entered terminal status '{app.Status}'.");

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var delay = remaining < pollInterval ? remaining : pollInterval;
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"App service '{id}' did not reach Running status within {timeout ?? TimeSpan.FromMinutes(10)}.");
    }
}
