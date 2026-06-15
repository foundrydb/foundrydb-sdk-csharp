using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDB.SDK.Auth.AuthApi"/> and the
/// top-level shorthand methods on <see cref="FoundryDBClient"/>.
/// </summary>
public class AuthApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- EnableAsync -----

    [Fact]
    public async Task EnableAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleAuthWithKeysJson("svc-1"));
        });

        await client.Auth.EnableAsync("svc-1", new AuthEnableRequest
        {
            AttachmentId = "att-1",
            IssuerDomainChoice = "fallback",
            Smtp = new AuthSmtpConfig
            {
                Host = "smtp.example.com",
                Port = 587,
                Username = "user",
                Password = "pass",
                FromAddress = "no-reply@example.com"
            }
        });

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/svc-1/auth/enable", path);
    }

    [Fact]
    public async Task EnableAsync_DeserializesAuthConfigurationWithKeys()
    {
        using var client = BuildClient(_ => Responses.Ok(SampleAuthWithKeysJson("svc-1")));

        var result = await client.Auth.EnableAsync("svc-1", new AuthEnableRequest
        {
            AttachmentId = "att-1",
            IssuerDomainChoice = "fallback",
            Smtp = new AuthSmtpConfig { Host = "smtp.example.com", Port = 587, Username = "u", Password = "p", FromAddress = "a@b.com" }
        });

        Assert.NotNull(result.Auth);
        Assert.Equal("auth-cfg-1", result.Auth!.Id);
        Assert.Equal("svc-1", result.Auth.AppServiceId);
        Assert.Equal("https://auth-svc-1.foundrydb.com", result.Auth.IssuerUrl);
        Assert.Equal("Enabled", result.Auth.Status);
        Assert.Single(result.SigningKeys);
        Assert.Equal("kid-abc", result.SigningKeys[0].Kid);
        Assert.Equal("RS256", result.SigningKeys[0].Algorithm);
        Assert.Equal("active", result.SigningKeys[0].Status);
    }

    [Fact]
    public async Task EnableAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Auth.EnableAsync("  ", new AuthEnableRequest()));
    }

    [Fact]
    public async Task EnableAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.Auth.EnableAsync("svc-1", null!));
    }

    [Fact]
    public async Task EnableAsync_ThrowsFoundryDBException_On400()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.BadRequest, "{\"error\":\"invalid smtp config\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Auth.EnableAsync("svc-1", new AuthEnableRequest()));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("invalid smtp config", ex.Detail);
    }

    // ----- GetAsync -----

    [Fact]
    public async Task GetAsync_SendsGetToCorrectPath()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleAuthWithKeysJson("svc-2"));
        });

        await client.Auth.GetAsync("svc-2");

        Assert.Equal("/app-services/svc-2/auth", path);
    }

    [Fact]
    public async Task GetAsync_DeserializesAuthConfigurationWithKeys()
    {
        using var client = BuildClient(_ => Responses.Ok(SampleAuthWithKeysJson("svc-2")));

        var result = await client.Auth.GetAsync("svc-2");

        Assert.NotNull(result.Auth);
        Assert.Equal("svc-2", result.Auth!.AppServiceId);
        Assert.Equal("Enabled", result.Auth.Status);
        Assert.NotEmpty(result.SigningKeys);
        Assert.Equal("kid-abc", result.SigningKeys[0].Kid);
    }

    [Fact]
    public async Task GetAsync_Throws404_WhenAuthNotEnabled()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"auth not enabled\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Auth.GetAsync("svc-99"));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Auth.GetAsync(""));
    }

    // ----- DisableAsync -----

    [Fact]
    public async Task DisableAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"status\":\"Disabled\"}");
        });

        await client.Auth.DisableAsync("svc-3");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/svc-3/auth/disable", path);
    }

    [Fact]
    public async Task DisableAsync_DeserializesStatus()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"status\":\"Disabled\"}"));

        var result = await client.Auth.DisableAsync("svc-3");

        Assert.Equal("Disabled", result.Status);
    }

    [Fact]
    public async Task DisableAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Auth.DisableAsync(""));
    }

    // ----- RotateKeyAsync -----

    [Fact]
    public async Task RotateKeyAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleRotateKeyResponseJson());
        });

        await client.Auth.RotateKeyAsync("svc-4");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/svc-4/auth/rotate-key", path);
    }

    [Fact]
    public async Task RotateKeyAsync_DeserializesSigningKey()
    {
        using var client = BuildClient(_ => Responses.Ok(SampleRotateKeyResponseJson()));

        var result = await client.Auth.RotateKeyAsync("svc-4");

        Assert.Equal("key-id-2", result.Id);
        Assert.Equal("kid-new", result.Kid);
        Assert.Equal("RS256", result.Algorithm);
        Assert.Equal("active", result.Status);
    }

    [Fact]
    public async Task RotateKeyAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Auth.RotateKeyAsync(""));
    }

    // ----- RevokeSessionAsync -----

    [Fact]
    public async Task RevokeSessionAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"task_id\":\"task-xyz\"}");
        });

        await client.Auth.RevokeSessionAsync("svc-5", "sess-abc");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/app-services/svc-5/auth/sessions/sess-abc/revoke", path);
    }

    [Fact]
    public async Task RevokeSessionAsync_DeserializesTaskId()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"task_id\":\"task-xyz\"}"));

        var result = await client.Auth.RevokeSessionAsync("svc-5", "sess-abc");

        Assert.Equal("task-xyz", result.TaskId);
    }

    [Fact]
    public async Task RevokeSessionAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Auth.RevokeSessionAsync("", "sess-abc"));
    }

    [Fact]
    public async Task RevokeSessionAsync_EmptySessionId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Auth.RevokeSessionAsync("svc-5", "  "));
    }

    // ----- Top-level shorthand methods -----

    [Fact]
    public async Task EnableAppServiceAuthAsync_DelegatesToAuthEnable()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleAuthWithKeysJson("svc-6"));
        });

        await client.EnableAppServiceAuthAsync("svc-6", new AuthEnableRequest
        {
            AttachmentId = "att-1",
            IssuerDomainChoice = "fallback",
            Smtp = new AuthSmtpConfig { Host = "h", Port = 587, Username = "u", Password = "p", FromAddress = "a@b.com" }
        });

        Assert.Equal("/app-services/svc-6/auth/enable", path);
    }

    [Fact]
    public async Task GetAppServiceAuthAsync_DelegatesToAuthGet()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleAuthWithKeysJson("svc-7"));
        });

        await client.GetAppServiceAuthAsync("svc-7");

        Assert.Equal("/app-services/svc-7/auth", path);
    }

    [Fact]
    public async Task DisableAppServiceAuthAsync_DelegatesToAuthDisable()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"status\":\"Disabled\"}");
        });

        await client.DisableAppServiceAuthAsync("svc-8");

        Assert.Equal("/app-services/svc-8/auth/disable", path);
    }

    [Fact]
    public async Task RotateAppServiceAuthKeyAsync_DelegatesToAuthRotateKey()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(SampleRotateKeyResponseJson());
        });

        await client.RotateAppServiceAuthKeyAsync("svc-9");

        Assert.Equal("/app-services/svc-9/auth/rotate-key", path);
    }

    [Fact]
    public async Task RevokeAppServiceAuthSessionAsync_DelegatesToAuthRevokeSession()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"task_id\":\"t1\"}");
        });

        await client.RevokeAppServiceAuthSessionAsync("svc-10", "sess-1");

        Assert.Equal("/app-services/svc-10/auth/sessions/sess-1/revoke", path);
    }

    // ----- Response shape: client_secret must never appear -----

    [Fact]
    public void IdpProviderInfo_DoesNotExposeClientSecret()
    {
        // IdpProviderInfo is the response model; it must never carry a ClientSecret.
        var props = typeof(IdpProviderInfo).GetProperties();
        foreach (var prop in props)
        {
            Assert.False(
                prop.Name.Equals("ClientSecret", StringComparison.OrdinalIgnoreCase),
                "IdpProviderInfo must not expose ClientSecret in response models.");
        }
    }

    [Fact]
    public void IdpProviderRequest_ExposesClientSecret()
    {
        // IdpProviderRequest is the request model; it must carry ClientSecret.
        var prop = typeof(IdpProviderRequest).GetProperty("ClientSecret");
        Assert.NotNull(prop);
    }

    // ----- Helpers -----

    /// <summary>
    /// Produces a canonical auth+signing_keys JSON response matching the Go SDK
    /// AuthConfigurationWithKeys shape.
    /// </summary>
    private static string SampleAuthWithKeysJson(string appServiceId) =>
        JsonSerializer.Serialize(new
        {
            auth = new
            {
                id = "auth-cfg-1",
                user_id = "user-1",
                app_service_id = appServiceId,
                database_service_id = "db-svc-1",
                attachment_id = "att-1",
                issuer_url = $"https://auth-{appServiceId}.foundrydb.com",
                fallback_domain = $"auth-{appServiceId}.foundrydb.com",
                status = "Enabled",
                schema_version_applied = "1",
                theme = new { display_name = "My App" },
                idp_providers = Array.Empty<object>(),
                created_at = "2026-01-01T00:00:00Z",
                updated_at = "2026-01-01T00:00:00Z"
            },
            signing_keys = new[]
            {
                new
                {
                    id = "key-id-1",
                    auth_configuration_id = "auth-cfg-1",
                    kid = "kid-abc",
                    algorithm = "RS256",
                    status = "active",
                    created_at = "2026-01-01T00:00:00Z",
                    updated_at = "2026-01-01T00:00:00Z"
                }
            }
        });

    private static string SampleRotateKeyResponseJson() =>
        JsonSerializer.Serialize(new
        {
            signing_key = new
            {
                id = "key-id-2",
                auth_configuration_id = "auth-cfg-1",
                kid = "kid-new",
                algorithm = "RS256",
                status = "active",
                created_at = "2026-01-02T00:00:00Z",
                updated_at = "2026-01-02T00:00:00Z"
            }
        });
}
