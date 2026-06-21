using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Attachments;

/// <summary>
/// Operations for managing companion-app attachments on managed services.
/// Companion apps (Metabase, Directus, Hasura, NocoDB, Open WebUI) are
/// deployed as app services and wired to a parent managed service automatically.
/// </summary>
public class AttachmentsApi
{
    private readonly FoundryDBClient _client;

    internal AttachmentsApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Deploys a companion app and attaches it to the parent managed service.
    /// The platform provisions an app service of the requested kind,
    /// wires it to the parent service, and injects connection credentials. Poll
    /// <see cref="FoundryDB.SDK.AppServices.AppServicesApi.WaitForRunningAsync"/> on
    /// the returned <see cref="AppService.Id"/> until the companion app is reachable.
    /// </summary>
    /// <param name="parentServiceId">UUID of the managed service to attach to.</param>
    /// <param name="req">Attachment parameters (kind, plan, optional subdomain).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AppService> CreateAttachmentAsync(
        string parentServiceId,
        CreateAttachmentRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(parentServiceId))
            throw new ArgumentException("Parent service ID must not be empty.", nameof(parentServiceId));
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Kind))
            throw new ArgumentException("Attachment kind must not be empty.", nameof(req));

        var json = await _client.PostAsync(
            $"/managed-services/{parentServiceId}/attachments", req, orgId: null, ct)
            .ConfigureAwait(false);

        return JsonSerializer.Deserialize<AppService>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error",
                "Response did not contain an app service.");
    }

    /// <summary>
    /// Returns all companion-app attachments for the given managed service.
    /// </summary>
    /// <param name="parentServiceId">UUID of the managed service.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<AttachmentSummary>> ListAttachmentsAsync(
        string parentServiceId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(parentServiceId))
            throw new ArgumentException("Parent service ID must not be empty.", nameof(parentServiceId));

        var json = await _client.GetAsync(
            $"/managed-services/{parentServiceId}/attachments", orgId: null, ct)
            .ConfigureAwait(false);

        using var doc = JsonDocument.Parse(json);
        var result = new List<AttachmentSummary>();

        if (doc.RootElement.TryGetProperty("attachments", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var entry = JsonSerializer.Deserialize<AttachmentSummary>(
                    el.GetRawText(), FoundryDBClient.JsonOptions);
                if (entry is not null) result.Add(entry);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the full attachment catalog listing every available companion-app kind
    /// with its display name, description, category, recommended plan, and supported
    /// parent service kinds.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<AttachmentCatalogEntry>> GetAttachmentCatalogAsync(
        CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/attachment-catalog", orgId: null, ct)
            .ConfigureAwait(false);

        using var doc = JsonDocument.Parse(json);
        var result = new List<AttachmentCatalogEntry>();

        // The catalog may be returned as a top-level array or wrapped in a
        // "catalog" / "entries" property depending on the API version.
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var entry = JsonSerializer.Deserialize<AttachmentCatalogEntry>(
                    el.GetRawText(), FoundryDBClient.JsonOptions);
                if (entry is not null) result.Add(entry);
            }
        }
        else
        {
            var catalogProp = doc.RootElement.TryGetProperty("catalog", out var cat)
                ? cat
                : doc.RootElement.TryGetProperty("entries", out var ent)
                    ? ent
                    : default;

            if (catalogProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in catalogProp.EnumerateArray())
                {
                    var entry = JsonSerializer.Deserialize<AttachmentCatalogEntry>(
                        el.GetRawText(), FoundryDBClient.JsonOptions);
                    if (entry is not null) result.Add(entry);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the admin credentials for a companion app. Credentials include the
    /// auto-provisioned admin email and password, any generated API keys or tokens,
    /// and the login URL.
    /// </summary>
    /// <param name="appServiceId">UUID of the companion app's underlying app service.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AttachmentCredentials> GetAttachmentCredentialsAsync(
        string appServiceId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appServiceId))
            throw new ArgumentException("App service ID must not be empty.", nameof(appServiceId));

        var json = await _client.GetAsync(
            $"/app-services/{appServiceId}/attachment-credentials", orgId: null, ct)
            .ConfigureAwait(false);

        return JsonSerializer.Deserialize<AttachmentCredentials>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error",
                "Response did not contain attachment credentials.");
    }
}
