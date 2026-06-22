using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Compliance;

/// <summary>
/// Operations for generating, listing, and downloading signed compliance evidence packets
/// (SOC 2 Type II and GDPR Article 30 Records of Processing Activities).
/// </summary>
public class ComplianceApi
{
    private readonly FoundryDBClient _client;

    internal ComplianceApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Generates a new signed compliance evidence packet for the given organisation and framework.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="framework">Compliance framework: "soc2" or "gdpr_ropa".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated report ID, the evidence packet, and its cryptographic signature.</returns>
    public async Task<GenerateComplianceReportResponse> GenerateComplianceReportAsync(
        string orgId,
        string framework,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));
        if (string.IsNullOrWhiteSpace(framework))
            throw new ArgumentException("Framework must not be empty.", nameof(framework));

        var body = new { framework };
        var json = await _client.PostAsync(
            $"/organizations/{orgId}/compliance-reports",
            body,
            orgId: null,
            ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<GenerateComplianceReportResponse>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a compliance report.");
    }

    /// <summary>
    /// Returns the list of previously generated compliance reports for an organisation.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<ComplianceReportRecord>> ListComplianceReportsAsync(
        string orgId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));

        var json = await _client.GetAsync(
            $"/organizations/{orgId}/compliance-reports",
            orgId: null,
            ct).ConfigureAwait(false);

        var records = new List<ComplianceReportRecord>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("reports", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var r = JsonSerializer.Deserialize<ComplianceReportRecord>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (r is not null) records.Add(r);
            }
        }

        return records;
    }

    /// <summary>
    /// Downloads the signed JSON packet for a specific compliance report.
    /// The returned string is the raw canonical JSON as signed by the platform.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="reportId">Report UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> DownloadComplianceReportJsonAsync(
        string orgId,
        string reportId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));
        if (string.IsNullOrWhiteSpace(reportId))
            throw new ArgumentException("Report ID must not be empty.", nameof(reportId));

        return await _client.GetAsync(
            $"/organizations/{orgId}/compliance-reports/{reportId}",
            orgId: null,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads the pre-rendered PDF version of a compliance report.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="reportId">Report UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw PDF bytes.</returns>
    public async Task<byte[]> DownloadComplianceReportPdfAsync(
        string orgId,
        string reportId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));
        if (string.IsNullOrWhiteSpace(reportId))
            throw new ArgumentException("Report ID must not be empty.", nameof(reportId));

        return await _client.GetBytesAsync(
            $"/organizations/{orgId}/compliance-reports/{reportId}/pdf",
            orgId: null,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the set of public signing keys published at /.well-known/compliance-signing-keys.
    /// Callers can use these to independently verify the signature on any downloaded packet.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ComplianceSigningKeySet> ComplianceSigningKeysAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync(
            "/.well-known/compliance-signing-keys",
            orgId: null,
            ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ComplianceSigningKeySet>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a signing key set.");
    }

    /// <summary>
    /// Returns all compliance framework subscriptions for the given organisation.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<ComplianceSubscription>> ListComplianceSubscriptionsAsync(
        string orgId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));

        var json = await _client.GetAsync(
            $"/organizations/{orgId}/compliance-subscriptions",
            orgId: null,
            ct).ConfigureAwait(false);

        var subscriptions = new List<ComplianceSubscription>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("subscriptions", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var s = JsonSerializer.Deserialize<ComplianceSubscription>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (s is not null) subscriptions.Add(s);
            }
        }

        return subscriptions;
    }

    /// <summary>
    /// Subscribes the organisation to a compliance framework, enabling on-demand report generation
    /// and quarterly automated evidence packets for that framework.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="framework">Compliance framework to subscribe to (e.g. "soc2", "gdpr_ropa", "dora", "eu_ai_act").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resulting subscription record.</returns>
    public async Task<ComplianceSubscription> SubscribeComplianceFrameworkAsync(
        string orgId,
        string framework,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));
        if (string.IsNullOrWhiteSpace(framework))
            throw new ArgumentException("Framework must not be empty.", nameof(framework));

        var json = await _client.PutAsync(
            $"/organizations/{orgId}/compliance-subscriptions/{framework}",
            payload: null,
            orgId: null,
            ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ComplianceSubscription>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a compliance subscription.");
    }

    /// <summary>
    /// Cancels the organisation's subscription to a compliance framework.
    /// Existing reports remain accessible after cancellation.
    /// </summary>
    /// <param name="orgId">Organisation UUID.</param>
    /// <param name="framework">Compliance framework to unsubscribe from.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UnsubscribeComplianceFrameworkAsync(
        string orgId,
        string framework,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("Organisation ID must not be empty.", nameof(orgId));
        if (string.IsNullOrWhiteSpace(framework))
            throw new ArgumentException("Framework must not be empty.", nameof(framework));

        await _client.DeleteAsync(
            $"/organizations/{orgId}/compliance-subscriptions/{framework}",
            orgId: null,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Rotates the platform compliance signing key. After rotation, new reports are signed with
    /// the freshly generated key. The previous key is retired but remains published at the
    /// well-known endpoint so that signatures on existing reports can still be verified.
    /// Requires admin privileges.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated set of public signing keys, including the new active key.</returns>
    public async Task<ComplianceSigningKeySet> RotateComplianceSigningKeyAsync(CancellationToken ct = default)
    {
        var json = await _client.PostAsync(
            "/admin/compliance/signing-keys/rotate",
            payload: null,
            orgId: null,
            ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ComplianceSigningKeySet>(json, FoundryDBClient.JsonOptions)
            ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a signing key set.");
    }
}
