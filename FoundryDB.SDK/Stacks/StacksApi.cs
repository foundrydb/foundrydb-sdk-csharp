using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Stacks;

/// <summary>
/// Operations for managing vertical starter stacks. All endpoints are under
/// <c>/stacks</c>. A stack provisions and wires together multiple platform
/// resources (databases, file buckets, inference proxies, app containers) in
/// one atomic operation.
/// </summary>
public class StacksApi
{
    private readonly FoundryDBClient _client;

    internal StacksApi(FoundryDBClient client)
    {
        _client = client;
    }

    // ----- Built-in templates -----

    /// <summary>
    /// Returns all available stack templates. Each entry describes the resources
    /// a stack will provision and includes an indicative cost preview.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<StackTemplate>> ListStackTemplatesAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/stacks/templates", orgId: null, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<StackTemplate>();
        if (doc.RootElement.TryGetProperty("templates", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var tmpl = JsonSerializer.Deserialize<StackTemplate>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (tmpl is not null) result.Add(tmpl);
            }
        }
        return result;
    }

    // ----- Custom template CRUD -----

    /// <summary>
    /// Creates a new custom stack template owned by the authenticated user or
    /// organisation. Returns the created template with its assigned UUID.
    /// </summary>
    /// <param name="req">Template definition.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CustomStackTemplate> CreateStackTemplateAsync(
        CustomTemplateRequest req,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Template name must not be empty.", nameof(req));
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            throw new ArgumentException("Template display name must not be empty.", nameof(req));

        var json = await _client.PostAsync("/stacks/templates", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<CustomStackTemplate>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(201, "Deserialization Error", "Response did not contain a stack template.");
    }

    /// <summary>
    /// Returns all custom stack templates created by the authenticated user or
    /// organisation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<CustomStackTemplate>> ListMyStackTemplatesAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/stacks/templates/mine", orgId: null, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<CustomStackTemplate>();
        if (doc.RootElement.TryGetProperty("templates", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var tmpl = JsonSerializer.Deserialize<CustomStackTemplate>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (tmpl is not null) result.Add(tmpl);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns all templates published to the marketplace (publication status
    /// "published", visibility "public").
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<CustomStackTemplate>> ListMarketplaceStackTemplatesAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/stacks/templates/marketplace", orgId: null, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<CustomStackTemplate>();
        if (doc.RootElement.TryGetProperty("templates", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var tmpl = JsonSerializer.Deserialize<CustomStackTemplate>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (tmpl is not null) result.Add(tmpl);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns the custom stack template with the given UUID, or
    /// <see langword="null"/> when it does not exist (404).
    /// </summary>
    /// <param name="templateId">Template UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CustomStackTemplate?> GetStackTemplateAsync(string templateId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID must not be empty.", nameof(templateId));

        try
        {
            var json = await _client.GetAsync($"/stacks/templates/{templateId}", orgId: null, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<CustomStackTemplate>(json, FoundryDBClient.JsonOptions);
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates an existing custom stack template. Only the fields set on
    /// <paramref name="req"/> are modified; omitted fields are left unchanged.
    /// </summary>
    /// <param name="templateId">Template UUID.</param>
    /// <param name="req">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CustomStackTemplate> UpdateStackTemplateAsync(
        string templateId,
        CustomTemplateRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID must not be empty.", nameof(templateId));
        ArgumentNullException.ThrowIfNull(req);

        var json = await _client.PatchAsync($"/stacks/templates/{templateId}", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<CustomStackTemplate>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack template.");
    }

    /// <summary>
    /// Deletes the custom stack template with the given UUID. A 404 response is
    /// treated as success (idempotent). Returns the status string from the response.
    /// </summary>
    /// <param name="templateId">Template UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> DeleteStackTemplateAsync(string templateId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID must not be empty.", nameof(templateId));

        try
        {
            var json = await _client.DeleteWithBodyAsync($"/stacks/templates/{templateId}", orgId: null, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
                return statusEl.GetString() ?? string.Empty;
            return string.Empty;
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            return string.Empty;
        }
    }

    // ----- Marketplace publish / unpublish -----

    /// <summary>
    /// Submits the custom template for marketplace review. The template's
    /// <c>publication_status</c> transitions to "submitted". Returns the updated template.
    /// </summary>
    /// <param name="templateId">Template UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CustomStackTemplate> PublishStackTemplateAsync(string templateId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID must not be empty.", nameof(templateId));

        var json = await _client.PostAsync($"/stacks/templates/{templateId}/publish", payload: null, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<CustomStackTemplate>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack template.");
    }

    /// <summary>
    /// Withdraws the template from the marketplace. The template's
    /// <c>publication_status</c> transitions to "unpublished". Returns the updated template.
    /// </summary>
    /// <param name="templateId">Template UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CustomStackTemplate> UnpublishStackTemplateAsync(string templateId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID must not be empty.", nameof(templateId));

        var json = await _client.PostAsync($"/stacks/templates/{templateId}/unpublish", payload: null, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<CustomStackTemplate>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack template.");
    }

    // ----- Preview -----

    /// <summary>
    /// Returns an itemised monthly cost estimate for the given template without
    /// provisioning any resources. Use this to confirm the projected spend before
    /// calling <see cref="LaunchStackAsync"/> with an <c>AcceptedMonthlyCost</c>.
    /// </summary>
    /// <param name="templateName">Template identifier (e.g. "rag-chatbot-starter").</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<StackCostPreview> PreviewStackAsync(string templateName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name must not be empty.", nameof(templateName));

        var req = new PreviewStackRequest { TemplateName = templateName };
        var json = await _client.PostAsync("/stacks/preview", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StackCostPreview>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a cost preview.");
    }

    // ----- CRUD -----

    /// <summary>
    /// Launches a new stack from the specified template. The platform provisions
    /// all resources defined in the template atomically and rolls back on any
    /// failure. The stack is created in Pending status; poll
    /// <see cref="WaitForRunningAsync"/> until it reaches Running.
    /// The <see cref="LaunchStackRequest.AcceptedMonthlyCost"/> field is required
    /// and acts as a hard cost gate: the platform rejects the request when the
    /// computed cost exceeds the accepted value.
    /// </summary>
    /// <param name="req">Launch parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Stack> LaunchStackAsync(LaunchStackRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Stack name must not be empty.", nameof(req));
        if (string.IsNullOrWhiteSpace(req.TemplateName) && string.IsNullOrWhiteSpace(req.TemplateId))
            throw new ArgumentException("Either TemplateName or TemplateId must be provided.", nameof(req));

        var orgId = req.OrganizationId ?? _client.Config.OrganizationId;
        var json = await _client.PostAsync("/stacks", req, orgId, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<Stack>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack.");
    }

    /// <summary>
    /// Returns all stacks visible to the authenticated user (or organisation).
    /// </summary>
    /// <param name="orgId">Optional organisation ID to scope the request.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<Stack>> ListStacksAsync(string? orgId = null, CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/stacks", orgId, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var result = new List<Stack>();
        if (doc.RootElement.TryGetProperty("stacks", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var stack = JsonSerializer.Deserialize<Stack>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (stack is not null) result.Add(stack);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns the stack with the given UUID, or <see langword="null"/> when it
    /// does not exist (404).
    /// </summary>
    /// <param name="id">Stack UUID.</param>
    /// <param name="orgId">Optional organisation ID to scope the request.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Stack?> GetStackAsync(string id, string? orgId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Stack ID must not be empty.", nameof(id));

        try
        {
            var json = await _client.GetAsync($"/stacks/{id}", orgId, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<Stack>(json, FoundryDBClient.JsonOptions);
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Initiates deletion of the stack. The platform tears down all provisioned
    /// resources in reverse dependency order and rolls back any partial state.
    /// Returns the status string from the acceptance response (e.g. "Deleting").
    /// A 404 response is treated as success (idempotent).
    /// </summary>
    /// <param name="id">Stack UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> DeleteStackAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Stack ID must not be empty.", nameof(id));

        try
        {
            var json = await _client.DeleteWithBodyAsync($"/stacks/{id}", orgId: null, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
                return statusEl.GetString() ?? string.Empty;
            return string.Empty;
        }
        catch (FoundryDBException ex) when (ex.StatusCode == 404)
        {
            return string.Empty;
        }
    }

    // ----- Actions -----

    /// <summary>
    /// Retries a stack that is in a Failed state. The platform re-runs the
    /// provisioning sequence from the last successful checkpoint. Returns the
    /// status string from the acceptance response (e.g. "Provisioning").
    /// </summary>
    /// <param name="id">Stack UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> RetryStackAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Stack ID must not be empty.", nameof(id));

        var json = await _client.PostAsync($"/stacks/{id}/retry", payload: null, orgId: null, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("status", out var statusEl))
            return statusEl.GetString() ?? string.Empty;
        return string.Empty;
    }

    // ----- Stack upgrades -----

    /// <summary>
    /// Returns an upgrade plan showing which resources will change when the stack
    /// is upgraded to a newer template version, without applying any changes.
    /// </summary>
    /// <param name="stackId">Stack UUID.</param>
    /// <param name="req">Target version to preview.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<StackUpgradePlan> PreviewStackUpgradeAsync(
        string stackId,
        PreviewStackUpgradeRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stackId))
            throw new ArgumentException("Stack ID must not be empty.", nameof(stackId));
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.TargetVersion))
            throw new ArgumentException("Target version must not be empty.", nameof(req));

        var json = await _client.PostAsync($"/stacks/{stackId}/upgrade/preview", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StackUpgradePlan>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack upgrade plan.");
    }

    /// <summary>
    /// Applies an upgrade to the stack, migrating it to the specified template version.
    /// Returns the migration record. Poll the migration's status until it reaches
    /// "Completed" or "Failed".
    /// </summary>
    /// <param name="stackId">Stack UUID.</param>
    /// <param name="req">Target version to upgrade to.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<StackMigration> ApplyStackUpgradeAsync(
        string stackId,
        ApplyStackUpgradeRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stackId))
            throw new ArgumentException("Stack ID must not be empty.", nameof(stackId));
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.TargetVersion))
            throw new ArgumentException("Target version must not be empty.", nameof(req));

        var json = await _client.PostAsync($"/stacks/{stackId}/upgrade", req, orgId: null, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StackMigration>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a stack migration.");
    }

    // ----- Polling -----

    /// <summary>
    /// Polls the stack until it reaches "Running" status or the timeout expires.
    /// Throws <see cref="TimeoutException"/> when not reached in time. Throws
    /// <see cref="FoundryDBException"/> immediately on terminal failure states
    /// ("Failed", "Deleted"). Poll interval is 10 seconds.
    /// </summary>
    /// <param name="id">Stack UUID.</param>
    /// <param name="timeout">Maximum wait duration. Defaults to 15 minutes.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Stack> WaitForRunningAsync(string id, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Stack ID must not be empty.", nameof(id));

        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(15));
        var pollInterval = TimeSpan.FromSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var stack = await GetStackAsync(id, orgId: null, ct).ConfigureAwait(false);
            if (stack is null)
                throw new FoundryDBException(404, "Not Found", $"Stack '{id}' not found while waiting for Running status.");

            var status = stack.Status.ToLowerInvariant();
            if (status == "running")
                return stack;

            if (status == "failed" || status == "deleted")
                throw new FoundryDBException(0, "Terminal State",
                    $"Stack '{id}' entered terminal status '{stack.Status}'{(stack.StatusDetail is not null ? ": " + stack.StatusDetail : string.Empty)}.");

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var delay = remaining < pollInterval ? remaining : pollInterval;
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Stack '{id}' did not reach Running status within {timeout ?? TimeSpan.FromMinutes(15)}.");
    }
}
