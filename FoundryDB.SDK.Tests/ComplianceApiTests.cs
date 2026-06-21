using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

public class ComplianceApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- GenerateComplianceReportAsync -----

    [Fact]
    public async Task GenerateComplianceReportAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        string? body = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                report_id = "rpt-1",
                packet = new
                {
                    schema_version = "1.0",
                    framework = "soc2",
                    generated_at = "2026-06-01T00:00:00Z",
                    period_start = "2025-06-01",
                    period_end = "2026-05-31",
                    organization = new { id = "org-1", name = "Acme", billing_email = "billing@acme.com" },
                    scope_boundary = "All services",
                    summary = new { service_count = 3, all_services_eu_residency = true, audit_log = new { retention_policy = "90 days", entry_count = 1234 } }
                },
                signature = new
                {
                    algorithm = "RS256",
                    key_id = "key-1",
                    value = "base64sig==",
                    canonical_sha256 = "abcdef1234"
                }
            }));
        });

        await client.Compliance.GenerateComplianceReportAsync("org-1", "soc2");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/organizations/org-1/compliance-reports", path);
        Assert.Contains("soc2", body ?? "");
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_DeserializesResponse()
    {
        var payload = new
        {
            report_id = "rpt-abc",
            packet = new
            {
                schema_version = "1.0",
                framework = "gdpr_ropa",
                generated_at = "2026-06-01T12:00:00Z",
                period_start = "2025-06-01",
                period_end = "2026-05-31",
                organization = new { id = "org-2", name = "Globex", billing_email = "cto@globex.eu", country = "DE" },
                scope_boundary = "EU services only",
                controls = new[]
                {
                    new { control_id = "ART30-1", title = "Record of processing", assertion = "Maintained.", status = "pass", evidence_refs = new[] { "e1" } }
                },
                summary = new { service_count = 5, all_services_eu_residency = true, audit_log = new { retention_policy = "180 days", oldest_entry_at = "2025-12-01T00:00:00Z", entry_count = 9999L } }
            },
            signature = new
            {
                algorithm = "RS256",
                key_id = "key-2",
                value = "sig==",
                canonical_sha256 = "deadbeef"
            }
        };
        using var client = BuildClient(_ => Responses.Ok(JsonSerializer.Serialize(payload)));

        var result = await client.Compliance.GenerateComplianceReportAsync("org-2", "gdpr_ropa");

        Assert.Equal("rpt-abc", result.ReportId);
        Assert.Equal("gdpr_ropa", result.Packet.Framework);
        Assert.Equal("1.0", result.Packet.SchemaVersion);
        Assert.Equal("org-2", result.Packet.Organization.Id);
        Assert.Equal("Globex", result.Packet.Organization.Name);
        Assert.Equal("DE", result.Packet.Organization.Country);
        Assert.Equal("EU services only", result.Packet.ScopeBoundary);
        Assert.NotNull(result.Packet.Controls);
        Assert.Single(result.Packet.Controls!);
        Assert.Equal("ART30-1", result.Packet.Controls![0].ControlId);
        Assert.Equal("pass", result.Packet.Controls[0].Status);
        Assert.NotNull(result.Packet.Controls[0].EvidenceRefs);
        Assert.Equal("e1", result.Packet.Controls[0].EvidenceRefs![0]);
        Assert.Equal(5, result.Packet.Summary.ServiceCount);
        Assert.True(result.Packet.Summary.AllServicesEuResidency);
        Assert.Equal(9999L, result.Packet.Summary.AuditLog.EntryCount);
        Assert.Equal("2025-12-01T00:00:00Z", result.Packet.Summary.AuditLog.OldestEntryAt);
        Assert.Equal("RS256", result.Signature.Algorithm);
        Assert.Equal("key-2", result.Signature.KeyId);
        Assert.Equal("sig==", result.Signature.Value);
        Assert.Equal("deadbeef", result.Signature.CanonicalSha256);
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_EmptyOrgId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.GenerateComplianceReportAsync("  ", "soc2"));
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_EmptyFramework_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.GenerateComplianceReportAsync("org-1", ""));
    }

    // ----- ListComplianceReportsAsync -----

    [Fact]
    public async Task ListComplianceReportsAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"reports\":[]}");
        });

        await client.Compliance.ListComplianceReportsAsync("org-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/organizations/org-1/compliance-reports", path);
    }

    [Fact]
    public async Task ListComplianceReportsAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"reports\":[]}"));

        var result = await client.Compliance.ListComplianceReportsAsync("org-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListComplianceReportsAsync_DeserializesRecords()
    {
        var body = JsonSerializer.Serialize(new
        {
            reports = new[]
            {
                new
                {
                    id = "rpt-1",
                    organization_id = "org-1",
                    framework = "soc2",
                    schema_version = "1.0",
                    period_start = "2025-01-01",
                    period_end = "2025-12-31",
                    generated_at = "2026-01-02T10:00:00Z",
                    generated_by = "admin@org.com",
                    signing_key_id = "key-1",
                    algorithm = "RS256",
                    status = "ready",
                    has_pdf = true
                }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Compliance.ListComplianceReportsAsync("org-1");

        Assert.Single(result);
        Assert.Equal("rpt-1", result[0].Id);
        Assert.Equal("org-1", result[0].OrganizationId);
        Assert.Equal("soc2", result[0].Framework);
        Assert.Equal("1.0", result[0].SchemaVersion);
        Assert.Equal("2025-01-01", result[0].PeriodStart);
        Assert.Equal("2025-12-31", result[0].PeriodEnd);
        Assert.Equal("admin@org.com", result[0].GeneratedBy);
        Assert.Equal("key-1", result[0].SigningKeyId);
        Assert.Equal("RS256", result[0].Algorithm);
        Assert.Equal("ready", result[0].Status);
        Assert.True(result[0].HasPdf);
    }

    [Fact]
    public async Task ListComplianceReportsAsync_EmptyOrgId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.ListComplianceReportsAsync("  "));
    }

    // ----- DownloadComplianceReportJsonAsync -----

    [Fact]
    public async Task DownloadComplianceReportJsonAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"framework\":\"soc2\"}");
        });

        var result = await client.Compliance.DownloadComplianceReportJsonAsync("org-1", "rpt-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/organizations/org-1/compliance-reports/rpt-1", path);
        Assert.Contains("soc2", result);
    }

    [Fact]
    public async Task DownloadComplianceReportJsonAsync_EmptyOrgId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.DownloadComplianceReportJsonAsync("  ", "rpt-1"));
    }

    [Fact]
    public async Task DownloadComplianceReportJsonAsync_EmptyReportId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.DownloadComplianceReportJsonAsync("org-1", "  "));
    }

    // ----- DownloadComplianceReportPdfAsync -----

    [Fact]
    public async Task DownloadComplianceReportPdfAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF magic bytes
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            };
        });

        var result = await client.Compliance.DownloadComplianceReportPdfAsync("org-1", "rpt-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/organizations/org-1/compliance-reports/rpt-1/pdf", path);
        Assert.Equal(pdfBytes, result);
    }

    [Fact]
    public async Task DownloadComplianceReportPdfAsync_EmptyOrgId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.DownloadComplianceReportPdfAsync("  ", "rpt-1"));
    }

    [Fact]
    public async Task DownloadComplianceReportPdfAsync_EmptyReportId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Compliance.DownloadComplianceReportPdfAsync("org-1", "  "));
    }

    // ----- ComplianceSigningKeysAsync -----

    [Fact]
    public async Task ComplianceSigningKeysAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"algorithm\":\"RS256\",\"keys\":[]}");
        });

        await client.Compliance.ComplianceSigningKeysAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/.well-known/compliance-signing-keys", path);
    }

    [Fact]
    public async Task ComplianceSigningKeysAsync_DeserializesKeySet()
    {
        var body = JsonSerializer.Serialize(new
        {
            algorithm = "RS256",
            keys = new[]
            {
                new
                {
                    key_id = "key-1",
                    algorithm = "RS256",
                    public_key = "-----BEGIN PUBLIC KEY-----\nMIIBIj...\n-----END PUBLIC KEY-----",
                    active = true,
                    retired_at = (string?)null
                },
                new
                {
                    key_id = "key-0",
                    algorithm = "RS256",
                    public_key = "-----BEGIN PUBLIC KEY-----\nOLD...\n-----END PUBLIC KEY-----",
                    active = false,
                    retired_at = (string?)"2025-01-01T00:00:00Z"
                }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Compliance.ComplianceSigningKeysAsync();

        Assert.Equal("RS256", result.Algorithm);
        Assert.NotNull(result.Keys);
        Assert.Equal(2, result.Keys!.Count);
        Assert.Equal("key-1", result.Keys[0].KeyId);
        Assert.True(result.Keys[0].Active);
        Assert.Null(result.Keys[0].RetiredAt);
        Assert.Equal("key-0", result.Keys[1].KeyId);
        Assert.False(result.Keys[1].Active);
        Assert.Equal("2025-01-01T00:00:00Z", result.Keys[1].RetiredAt);
    }

    [Fact]
    public async Task ComplianceSigningKeysAsync_EmptyKeysList_ReturnsEmptySet()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"algorithm\":\"RS256\",\"keys\":[]}"));

        var result = await client.Compliance.ComplianceSigningKeysAsync();

        Assert.Equal("RS256", result.Algorithm);
        Assert.NotNull(result.Keys);
        Assert.Empty(result.Keys!);
    }
}
