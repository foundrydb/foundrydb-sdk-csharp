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
/// Tests for <see cref="FoundryDB.SDK.Stacks.StacksApi"/>.
/// </summary>
public class StacksApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- ListStackTemplatesAsync -----

    [Fact]
    public async Task ListStackTemplatesAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"templates\":[]}");
        });

        await client.Stacks.ListStackTemplatesAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks/templates", path);
    }

    [Fact]
    public async Task ListStackTemplatesAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"templates\":[]}"));
        var result = await client.Stacks.ListStackTemplatesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListStackTemplatesAsync_DeserializesTemplates()
    {
        var body = JsonSerializer.Serialize(new
        {
            templates = new[]
            {
                new
                {
                    name = "rag-chatbot-starter",
                    display_name = "Launch a RAG chatbot",
                    description = "Provisions PG+pgvector, Files, inference proxy, and Open WebUI.",
                    version = "1.0.0",
                    cost_preview = new
                    {
                        template_name = "rag-chatbot-starter",
                        currency = "EUR",
                        monthly_total = 177.11m,
                        line_items = new[]
                        {
                            new
                            {
                                symbolic_name = "pg_primary",
                                kind = "database",
                                description = "PostgreSQL with pgvector",
                                monthly_cost = 60.00m,
                                is_ceiling = false
                            }
                        },
                        warnings = new[] { "Inference costs depend on usage volume." }
                    }
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.ListStackTemplatesAsync();

        Assert.Single(result);
        var tmpl = result[0];
        Assert.Equal("rag-chatbot-starter", tmpl.Name);
        Assert.Equal("Launch a RAG chatbot", tmpl.DisplayName);
        Assert.Equal("1.0.0", tmpl.Version);
        Assert.NotNull(tmpl.CostPreview);
        Assert.Equal("EUR", tmpl.CostPreview!.Currency);
        Assert.Equal(177.11m, tmpl.CostPreview.MonthlyTotal);
        Assert.Single(tmpl.CostPreview.LineItems!);
        Assert.Equal("pg_primary", tmpl.CostPreview.LineItems![0].SymbolicName);
        Assert.False(tmpl.CostPreview.LineItems[0].IsCeiling);
        Assert.Single(tmpl.CostPreview.Warnings!);
    }

    // ----- PreviewStackAsync -----

    [Fact]
    public async Task PreviewStackAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                template_name = "rag-chatbot-starter",
                currency = "EUR",
                monthly_total = 177.11m
            }));
        });

        await client.Stacks.PreviewStackAsync("rag-chatbot-starter");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/preview", path);
    }

    [Fact]
    public async Task PreviewStackAsync_DeserializesCostPreview()
    {
        var body = JsonSerializer.Serialize(new
        {
            template_name = "rag-chatbot-starter",
            currency = "EUR",
            monthly_total = 177.11m,
            line_items = new[]
            {
                new
                {
                    symbolic_name = "inference_proxy",
                    kind = "inference",
                    description = "Inference proxy (usage-based ceiling)",
                    monthly_cost = 50.00m,
                    is_ceiling = true
                }
            },
            warnings = new[] { "Inference ceiling applies." }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.PreviewStackAsync("rag-chatbot-starter");

        Assert.Equal("rag-chatbot-starter", result.TemplateName);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(177.11m, result.MonthlyTotal);
        Assert.Single(result.LineItems!);
        Assert.Equal("inference_proxy", result.LineItems![0].SymbolicName);
        Assert.Equal("inference", result.LineItems[0].Kind);
        Assert.True(result.LineItems[0].IsCeiling);
        Assert.Single(result.Warnings!);
    }

    [Fact]
    public async Task PreviewStackAsync_ThrowsOnEmptyTemplateName()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.PreviewStackAsync(""));
    }

    // ----- LaunchStackAsync -----

    [Fact]
    public async Task LaunchStackAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Status(HttpStatusCode.Created, JsonSerializer.Serialize(new
            {
                id = "stk-1",
                name = "my-chatbot",
                template_name = "rag-chatbot-starter",
                template_version = "1.0.0",
                status = "Pending",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        var req = new LaunchStackRequest
        {
            Name = "my-chatbot",
            TemplateName = "rag-chatbot-starter",
            AcceptedMonthlyCost = 200m
        };
        await client.Stacks.LaunchStackAsync(req);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks", path);
    }

    [Fact]
    public async Task LaunchStackAsync_DeserializesStack()
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "stk-abc",
            name = "my-chatbot",
            template_name = "rag-chatbot-starter",
            template_version = "1.0.0",
            status = "Pending",
            status_detail = "Queued for provisioning",
            estimated_monthly_cost = 177.11m,
            organization_id = "org-1",
            resources = new[]
            {
                new
                {
                    id = "res-1",
                    stack_id = "stk-abc",
                    symbolic_name = "pg_primary",
                    kind = "database",
                    status = "Pending",
                    sequence = 1,
                    created_at = "2026-06-01T00:00:00Z",
                    updated_at = "2026-06-01T00:00:00Z"
                }
            },
            created_at = "2026-06-01T00:00:00Z",
            updated_at = "2026-06-01T00:00:00Z"
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.LaunchStackAsync(new LaunchStackRequest
        {
            Name = "my-chatbot",
            TemplateName = "rag-chatbot-starter",
            AcceptedMonthlyCost = 200m
        });

        Assert.Equal("stk-abc", result.Id);
        Assert.Equal("my-chatbot", result.Name);
        Assert.Equal("rag-chatbot-starter", result.TemplateName);
        Assert.Equal("1.0.0", result.TemplateVersion);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Queued for provisioning", result.StatusDetail);
        Assert.Equal(177.11m, result.EstimatedMonthlyCost);
        Assert.Equal("org-1", result.OrganizationId);
        Assert.Single(result.Resources!);
        Assert.Equal("pg_primary", result.Resources![0].SymbolicName);
        Assert.Equal("database", result.Resources[0].Kind);
        Assert.Equal(1, result.Resources[0].Sequence);
    }

    [Fact]
    public async Task LaunchStackAsync_ThrowsOnEmptyName()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.LaunchStackAsync(new LaunchStackRequest
            {
                Name = "",
                TemplateName = "rag-chatbot-starter",
                AcceptedMonthlyCost = 200m
            }));
    }

    [Fact]
    public async Task LaunchStackAsync_ThrowsOnEmptyTemplateName()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.LaunchStackAsync(new LaunchStackRequest
            {
                Name = "my-stack",
                TemplateName = "",
                AcceptedMonthlyCost = 200m
            }));
    }

    // ----- ListStacksAsync -----

    [Fact]
    public async Task ListStacksAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"stacks\":[]}");
        });

        await client.Stacks.ListStacksAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks", path);
    }

    [Fact]
    public async Task ListStacksAsync_ReturnsEmptyList_WhenArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"stacks\":[]}"));
        var result = await client.Stacks.ListStacksAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListStacksAsync_DeserializesStacks()
    {
        var body = JsonSerializer.Serialize(new
        {
            stacks = new[]
            {
                new
                {
                    id = "stk-1",
                    name = "my-chatbot",
                    template_name = "rag-chatbot-starter",
                    template_version = "1.0.0",
                    status = "Running",
                    endpoint_url = "https://my-chatbot.foundrydb.com",
                    created_at = "2026-06-01T00:00:00Z",
                    updated_at = "2026-06-01T00:00:00Z"
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.ListStacksAsync();

        Assert.Single(result);
        Assert.Equal("stk-1", result[0].Id);
        Assert.Equal("Running", result[0].Status);
        Assert.Equal("https://my-chatbot.foundrydb.com", result[0].EndpointUrl);
    }

    // ----- GetStackAsync -----

    [Fact]
    public async Task GetStackAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "stk-1",
                name = "my-chatbot",
                template_name = "rag-chatbot-starter",
                template_version = "1.0.0",
                status = "Running",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        await client.Stacks.GetStackAsync("stk-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks/stk-1", path);
    }

    [Fact]
    public async Task GetStackAsync_ReturnsNull_OnNotFound()
    {
        using var client = BuildClient(_ => Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));
        var result = await client.Stacks.GetStackAsync("stk-missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStackAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.GetStackAsync(""));
    }

    // ----- DeleteStackAsync -----

    [Fact]
    public async Task DeleteStackAsync_SendsDeleteToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"status\":\"Deleting\"}");
        });

        var status = await client.Stacks.DeleteStackAsync("stk-1");

        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal("/stacks/stk-1", path);
        Assert.Equal("Deleting", status);
    }

    [Fact]
    public async Task DeleteStackAsync_ReturnsEmpty_OnNotFound()
    {
        using var client = BuildClient(_ => Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));
        var result = await client.Stacks.DeleteStackAsync("stk-gone");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task DeleteStackAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.DeleteStackAsync(""));
    }

    // ----- RetryStackAsync -----

    [Fact]
    public async Task RetryStackAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"status\":\"Provisioning\"}");
        });

        var status = await client.Stacks.RetryStackAsync("stk-1");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/stk-1/retry", path);
        Assert.Equal("Provisioning", status);
    }

    [Fact]
    public async Task RetryStackAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.RetryStackAsync("   "));
    }
}
