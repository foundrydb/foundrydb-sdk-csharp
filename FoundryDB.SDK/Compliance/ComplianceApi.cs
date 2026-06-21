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
}
