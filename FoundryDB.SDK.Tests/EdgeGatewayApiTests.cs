using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

public class EdgeGatewayApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- ListAppDomainsAsync -----

    [Fact]
    public async Task ListAppDomainsAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"domains\":[]}");
        });

        await client.EdgeGateway.ListAppDomainsAsync("app-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/app-services/app-1/domains", path);
    }

    [Fact]
    public async Task ListAppDomainsAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"domains\":[]}"));

        var result = await client.EdgeGateway.ListAppDomainsAsync("app-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAppDomainsAsync_DeserializesDomains()
    {
        var body = JsonSerializer.Serialize(new
        {
            domains = new[]
            {
                new
                {
                    id = "d1",
                    service_id = "app-1",
                    user_id = "u1",
                    domain = "www.example.com",
                    status = "active",
                    cname_target = "app-1.edge.foundrydb.com",
                    created_at = "2026-01-01T00:00:00Z",
                    updated_at = "2026-01-02T00:00:00Z"
                }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.EdgeGateway.ListAppDomainsAsync("app-1");

        Assert.Single(result);
        Assert.Equal("d1", result[0].Id);
        Assert.Equal("www.example.com", result[0].Domain);
        Assert.Equal(EdgeDomainStatus.Active, result[0].Status);
        Assert.Equal("app-1.edge.foundrydb.com", result[0].CnameTarget);
    }

    [Fact]
    public async Task ListAppDomainsAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.ListAppDomainsAsync("  "));
    }

    // ----- CreateAppDomainAsync -----

    [Fact]
    public async Task CreateAppDomainAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        string? body = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Responses.Ok("{\"id\":\"d1\",\"service_id\":\"app-1\",\"user_id\":\"u1\",\"domain\":\"www.example.com\",\"status\":\"pending_verification\",\"created_at\":\"2026-01-01T00:00:00Z\",\"updated_at\":\"2026-01-01T00:00:00Z\"}");
        });

        var result = await client.EdgeGateway.CreateAppDomainAsync("app-1", "www.example.com");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/app-1/domains", path);
        Assert.Contains("www.example.com", body ?? "");
        Assert.Equal("d1", result.Id);
        Assert.Equal(EdgeDomainStatus.PendingVerification, result.Status);
    }

    [Fact]
    public async Task CreateAppDomainAsync_EmptyAppServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.CreateAppDomainAsync("", "example.com"));
    }

    [Fact]
    public async Task CreateAppDomainAsync_EmptyDomain_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.CreateAppDomainAsync("app-1", "  "));
    }

    // ----- VerifyAppDomainAsync -----

    [Fact]
    public async Task VerifyAppDomainAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"id\":\"d1\",\"service_id\":\"app-1\",\"user_id\":\"u1\",\"domain\":\"www.example.com\",\"status\":\"verifying\",\"created_at\":\"2026-01-01T00:00:00Z\",\"updated_at\":\"2026-01-01T00:00:00Z\"}");
        });

        var result = await client.EdgeGateway.VerifyAppDomainAsync("app-1", "d1");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/app-1/domains/d1/verify", path);
        Assert.Equal(EdgeDomainStatus.Verifying, result.Status);
    }

    [Fact]
    public async Task VerifyAppDomainAsync_EmptyDomainId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.VerifyAppDomainAsync("app-1", "  "));
    }

    // ----- DeleteAppDomainAsync -----

    [Fact]
    public async Task DeleteAppDomainAsync_SendsDeleteToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("");
        });

        await client.EdgeGateway.DeleteAppDomainAsync("app-1", "d1");

        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal("/app-services/app-1/domains/d1", path);
    }

    [Fact]
    public async Task DeleteAppDomainAsync_404_IsIdempotent()
    {
        using var client = BuildClient(_ => Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));

        // Should not throw.
        await client.EdgeGateway.DeleteAppDomainAsync("app-1", "gone");
    }

    [Fact]
    public async Task DeleteAppDomainAsync_EmptyDomainId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.DeleteAppDomainAsync("app-1", "  "));
    }

    // ----- GetAppEdgeStatusAsync -----

    [Fact]
    public async Task GetAppEdgeStatusAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"edge_enabled\":true,\"home_pop\":\"se-sto1\",\"cname_target\":\"app-1.edge.foundrydb.com\",\"config_version\":3,\"applications\":[]}");
        });

        await client.EdgeGateway.GetAppEdgeStatusAsync("app-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/app-services/app-1/edge", path);
    }

    [Fact]
    public async Task GetAppEdgeStatusAsync_DeserializesStatus()
    {
        var body = JsonSerializer.Serialize(new
        {
            edge_enabled = true,
            home_pop = "se-sto1",
            cname_target = "app-1.edge.foundrydb.com",
            config_version = 5L,
            applications = new[]
            {
                new { zone = "se-sto1", applied_version = 5L, status = "converged" }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.EdgeGateway.GetAppEdgeStatusAsync("app-1");

        Assert.True(result.EdgeEnabled);
        Assert.Equal("se-sto1", result.HomePop);
        Assert.Equal("app-1.edge.foundrydb.com", result.CnameTarget);
        Assert.Equal(5L, result.ConfigVersion);
        Assert.NotNull(result.Applications);
        Assert.Single(result.Applications!);
        Assert.Equal("se-sto1", result.Applications![0].Zone);
        Assert.Equal(5L, result.Applications[0].AppliedVersion);
        Assert.Equal("converged", result.Applications[0].Status);
    }

    [Fact]
    public async Task GetAppEdgeStatusAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.EdgeGateway.GetAppEdgeStatusAsync("  "));
    }

    // ----- UpdateAppEdgeSettingsAsync -----

    [Fact]
    public async Task UpdateAppEdgeSettingsAsync_SendsPutToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"waf_mode\":\"off\",\"config_version\":4}");
        });

        await client.EdgeGateway.UpdateAppEdgeSettingsAsync("app-1", new EdgeSettingsRequest
        {
            WafMode = EdgeWAFMode.Off
        });

        Assert.Equal(HttpMethod.Put, method);
        Assert.Equal("/app-services/app-1/edge/settings", path);
    }

    [Fact]
    public async Task UpdateAppEdgeSettingsAsync_DeserializesSettings()
    {
        var body = JsonSerializer.Serialize(new
        {
            cache_rules = new[]
            {
                new { path_prefix = "/static/", ttl_seconds = 3600 }
            },
            waf_mode = "detect",
            config_version = 7L
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.EdgeGateway.UpdateAppEdgeSettingsAsync("app-1", new EdgeSettingsRequest
        {
            CacheRules = new List<EdgeCacheRule>
            {
                new EdgeCacheRule { PathPrefix = "/static/", TtlSeconds = 3600 }
            },
            WafMode = EdgeWAFMode.Detect
        });

        Assert.NotNull(result.CacheRules);
        Assert.Single(result.CacheRules!);
        Assert.Equal("/static/", result.CacheRules![0].PathPrefix);
        Assert.Equal(3600, result.CacheRules[0].TtlSeconds);
        Assert.Equal(EdgeWAFMode.Detect, result.WafMode);
        Assert.Equal(7L, result.ConfigVersion);
    }

    [Fact]
    public async Task UpdateAppEdgeSettingsAsync_WithRateLimit_SendsRateLimitInBody()
    {
        string? requestBody = null;
        using var client = BuildClient(req =>
        {
            requestBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Responses.Ok("{\"waf_mode\":\"off\",\"config_version\":2}");
        });

        await client.EdgeGateway.UpdateAppEdgeSettingsAsync("app-1", new EdgeSettingsRequest
        {
            RateLimit = new EdgeRateLimit { RequestsPerSecond = 100, Burst = 200, Key = EdgeRateLimitKey.Ip }
        });

        Assert.Contains("requests_per_second", requestBody ?? "");
        Assert.Contains("100", requestBody ?? "");
        Assert.Contains("ip", requestBody ?? "");
    }

    [Fact]
    public async Task UpdateAppEdgeSettingsAsync_NullSettings_ThrowsArgumentNullException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.EdgeGateway.UpdateAppEdgeSettingsAsync("app-1", null!));
    }

    [Fact]
    public async Task UpdateAppEdgeSettingsAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.EdgeGateway.UpdateAppEdgeSettingsAsync("  ", new EdgeSettingsRequest()));
    }
}
