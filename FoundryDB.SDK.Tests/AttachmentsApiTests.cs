using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDB.SDK.Attachments.AttachmentsApi"/>.
/// </summary>
public class AttachmentsApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- CreateAttachmentAsync -----

    [Fact]
    public async Task CreateAttachmentAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new { id = "app-1", status = "Pending" }));
        });

        var req = new CreateAttachmentRequest { Kind = "metabase", PlanName = "tier-2" };
        await client.Attachments.CreateAttachmentAsync("svc-123", req);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/managed-services/svc-123/attachments", path);
    }

    [Fact]
    public async Task CreateAttachmentAsync_DeserializesAppService()
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "app-abc",
            name = "metabase-svc",
            status = "Pending",
            service_kind = "app",
            zone = "se-sto1",
            plan_name = "tier-2",
            user_id = "u1",
            created_at = "2026-01-01T00:00:00Z",
            updated_at = "2026-01-01T00:00:00Z"
        });

        using var client = BuildClient(_ => Responses.Ok(body));

        var req = new CreateAttachmentRequest { Kind = "metabase", PlanName = "tier-2" };
        var result = await client.Attachments.CreateAttachmentAsync("svc-123", req);

        Assert.Equal("app-abc", result.Id);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("metabase-svc", result.Name);
    }

    [Fact]
    public async Task CreateAttachmentAsync_ThrowsOnEmptyParentServiceId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        var req = new CreateAttachmentRequest { Kind = "metabase", PlanName = "tier-2" };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Attachments.CreateAttachmentAsync("   ", req));
    }

    [Fact]
    public async Task CreateAttachmentAsync_ThrowsOnEmptyKind()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        var req = new CreateAttachmentRequest { Kind = "", PlanName = "tier-2" };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Attachments.CreateAttachmentAsync("svc-123", req));
    }

    // ----- ListAttachmentsAsync -----

    [Fact]
    public async Task ListAttachmentsAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"attachments\":[]}");
        });

        await client.Attachments.ListAttachmentsAsync("svc-456");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/managed-services/svc-456/attachments", path);
    }

    [Fact]
    public async Task ListAttachmentsAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"attachments\":[]}"));
        var result = await client.Attachments.ListAttachmentsAsync("svc-456");
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAttachmentsAsync_DeserializesAttachmentSummaries()
    {
        var body = JsonSerializer.Serialize(new
        {
            attachments = new[]
            {
                new
                {
                    attachment_id = "att-1",
                    app_service_id = "app-1",
                    kind = "metabase",
                    name = "My Metabase",
                    status = "Running",
                    wiring_status = "wired",
                    url = "https://my-metabase.foundrydb.com"
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Attachments.ListAttachmentsAsync("svc-456");

        Assert.Single(result);
        Assert.Equal("att-1", result[0].AttachmentId);
        Assert.Equal("app-1", result[0].AppServiceId);
        Assert.Equal("metabase", result[0].Kind);
        Assert.Equal("Running", result[0].Status);
        Assert.Equal("wired", result[0].WiringStatus);
        Assert.Equal("https://my-metabase.foundrydb.com", result[0].Url);
    }

    [Fact]
    public async Task ListAttachmentsAsync_ThrowsOnEmptyParentServiceId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Attachments.ListAttachmentsAsync(""));
    }

    // ----- GetAttachmentCatalogAsync -----

    [Fact]
    public async Task GetAttachmentCatalogAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("[]");
        });

        await client.Attachments.GetAttachmentCatalogAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/attachment-catalog", path);
    }

    [Fact]
    public async Task GetAttachmentCatalogAsync_DeserializesTopLevelArray()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new
            {
                kind = "metabase",
                display_name = "Metabase",
                description = "Open source BI and analytics",
                category = "analytics",
                default_plan = "tier-2",
                requires_parent_kinds = new[] { "postgresql", "mysql" }
            },
            new
            {
                kind = "directus",
                display_name = "Directus",
                description = "Headless CMS and data platform",
                category = "cms",
                default_plan = "tier-2",
                requires_parent_kinds = new[] { "postgresql" }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Attachments.GetAttachmentCatalogAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("metabase", result[0].Kind);
        Assert.Equal("Metabase", result[0].DisplayName);
        Assert.Equal("analytics", result[0].Category);
        Assert.Equal("tier-2", result[0].DefaultPlan);
        Assert.Equal(new[] { "postgresql", "mysql" }, result[0].RequiresParentKinds);
        Assert.Equal("directus", result[1].Kind);
    }

    [Fact]
    public async Task GetAttachmentCatalogAsync_DeserializesCatalogWrappedResponse()
    {
        var body = JsonSerializer.Serialize(new
        {
            catalog = new[]
            {
                new
                {
                    kind = "nocodb",
                    display_name = "NocoDB",
                    description = "Open source Airtable alternative",
                    category = "cms",
                    default_plan = "tier-2"
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Attachments.GetAttachmentCatalogAsync();

        Assert.Single(result);
        Assert.Equal("nocodb", result[0].Kind);
        Assert.Equal("NocoDB", result[0].DisplayName);
    }

    [Fact]
    public async Task GetAttachmentCatalogAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));
        var result = await client.Attachments.GetAttachmentCatalogAsync();
        Assert.Empty(result);
    }

    // ----- GetAttachmentCredentialsAsync -----

    [Fact]
    public async Task GetAttachmentCredentialsAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                admin_email = "admin@example.com",
                admin_password = "secret"
            }));
        });

        await client.Attachments.GetAttachmentCredentialsAsync("app-789");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/app-services/app-789/attachment-credentials", path);
    }

    [Fact]
    public async Task GetAttachmentCredentialsAsync_DeserializesCredentials()
    {
        var body = JsonSerializer.Serialize(new
        {
            admin_email = "admin@foundrydb.com",
            admin_password = "supersecret",
            generated = new Dictionary<string, string>
            {
                ["api_key"] = "mbk_abc123"
            },
            login_url = "https://my-metabase.foundrydb.com/auth/login"
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Attachments.GetAttachmentCredentialsAsync("app-789");

        Assert.Equal("admin@foundrydb.com", result.AdminEmail);
        Assert.Equal("supersecret", result.AdminPassword);
        Assert.NotNull(result.Generated);
        Assert.Equal("mbk_abc123", result.Generated["api_key"]);
        Assert.Equal("https://my-metabase.foundrydb.com/auth/login", result.LoginUrl);
    }

    [Fact]
    public async Task GetAttachmentCredentialsAsync_ThrowsOnEmptyAppServiceId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Attachments.GetAttachmentCredentialsAsync(""));
    }
}
