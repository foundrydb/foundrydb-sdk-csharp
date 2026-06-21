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

    // ----- CreateStackTemplateAsync -----

    [Fact]
    public async Task CreateStackTemplateAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Status(System.Net.HttpStatusCode.Created, JsonSerializer.Serialize(new
            {
                id = "tmpl-1",
                name = "my-template",
                display_name = "My Template",
                description = "A custom template.",
                version = "1.0.0",
                visibility = "private",
                publication_status = "draft",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        var req = new CustomTemplateRequest { Name = "my-template", DisplayName = "My Template" };
        var result = await client.Stacks.CreateStackTemplateAsync(req);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/templates", path);
        Assert.Equal("tmpl-1", result.Id);
        Assert.Equal("my-template", result.Name);
        Assert.Equal("private", result.Visibility);
        Assert.Equal("draft", result.PublicationStatus);
    }

    [Fact]
    public async Task CreateStackTemplateAsync_ThrowsOnEmptyName()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.CreateStackTemplateAsync(new CustomTemplateRequest { Name = "", DisplayName = "X" }));
    }

    [Fact]
    public async Task CreateStackTemplateAsync_ThrowsOnEmptyDisplayName()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.CreateStackTemplateAsync(new CustomTemplateRequest { Name = "t", DisplayName = "" }));
    }

    // ----- ListMyStackTemplatesAsync -----

    [Fact]
    public async Task ListMyStackTemplatesAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"templates\":[]}");
        });

        await client.Stacks.ListMyStackTemplatesAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks/templates/mine", path);
    }

    [Fact]
    public async Task ListMyStackTemplatesAsync_DeserializesTemplates()
    {
        var body = JsonSerializer.Serialize(new
        {
            templates = new[]
            {
                new
                {
                    id = "tmpl-42",
                    name = "my-rag",
                    display_name = "My RAG Stack",
                    description = "Custom RAG chatbot.",
                    version = "1.2.0",
                    visibility = "org_shared",
                    publication_status = "draft",
                    created_at = "2026-06-01T00:00:00Z",
                    updated_at = "2026-06-01T00:00:00Z"
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.ListMyStackTemplatesAsync();

        Assert.Single(result);
        Assert.Equal("tmpl-42", result[0].Id);
        Assert.Equal("org_shared", result[0].Visibility);
        Assert.Equal("draft", result[0].PublicationStatus);
    }

    // ----- ListMarketplaceStackTemplatesAsync -----

    [Fact]
    public async Task ListMarketplaceStackTemplatesAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"templates\":[]}");
        });

        await client.Stacks.ListMarketplaceStackTemplatesAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks/templates/marketplace", path);
    }

    [Fact]
    public async Task ListMarketplaceStackTemplatesAsync_DeserializesPublishedTemplates()
    {
        var body = JsonSerializer.Serialize(new
        {
            templates = new[]
            {
                new
                {
                    id = "tmpl-pub",
                    name = "community-rag",
                    display_name = "Community RAG",
                    description = "Community-built RAG stack.",
                    version = "2.0.0",
                    visibility = "public",
                    publication_status = "published",
                    created_at = "2026-06-01T00:00:00Z",
                    updated_at = "2026-06-01T00:00:00Z"
                }
            }
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.ListMarketplaceStackTemplatesAsync();

        Assert.Single(result);
        Assert.Equal("tmpl-pub", result[0].Id);
        Assert.Equal("public", result[0].Visibility);
        Assert.Equal("published", result[0].PublicationStatus);
    }

    // ----- GetStackTemplateAsync -----

    [Fact]
    public async Task GetStackTemplateAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "tmpl-1",
                name = "my-template",
                display_name = "My Template",
                description = "",
                version = "1.0.0",
                visibility = "private",
                publication_status = "draft",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        await client.Stacks.GetStackTemplateAsync("tmpl-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/stacks/templates/tmpl-1", path);
    }

    [Fact]
    public async Task GetStackTemplateAsync_ReturnsNull_OnNotFound()
    {
        using var client = BuildClient(_ => Responses.Status(System.Net.HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));
        var result = await client.Stacks.GetStackTemplateAsync("tmpl-missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStackTemplateAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.GetStackTemplateAsync(""));
    }

    // ----- UpdateStackTemplateAsync -----

    [Fact]
    public async Task UpdateStackTemplateAsync_SendsPatchToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "tmpl-1",
                name = "my-template",
                display_name = "Updated Display",
                description = "New description.",
                version = "1.1.0",
                visibility = "org_shared",
                publication_status = "draft",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-02T00:00:00Z"
            }));
        });

        var req = new CustomTemplateRequest { Name = "my-template", DisplayName = "Updated Display", Version = "1.1.0" };
        var result = await client.Stacks.UpdateStackTemplateAsync("tmpl-1", req);

        Assert.Equal(HttpMethod.Patch, method);
        Assert.Equal("/stacks/templates/tmpl-1", path);
        Assert.Equal("Updated Display", result.DisplayName);
        Assert.Equal("1.1.0", result.Version);
    }

    [Fact]
    public async Task UpdateStackTemplateAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.UpdateStackTemplateAsync("", new CustomTemplateRequest { Name = "x", DisplayName = "x" }));
    }

    // ----- DeleteStackTemplateAsync -----

    [Fact]
    public async Task DeleteStackTemplateAsync_SendsDeleteToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"status\":\"Deleted\"}");
        });

        var status = await client.Stacks.DeleteStackTemplateAsync("tmpl-1");

        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal("/stacks/templates/tmpl-1", path);
        Assert.Equal("Deleted", status);
    }

    [Fact]
    public async Task DeleteStackTemplateAsync_ReturnsEmpty_OnNotFound()
    {
        using var client = BuildClient(_ => Responses.Status(System.Net.HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));
        var result = await client.Stacks.DeleteStackTemplateAsync("tmpl-gone");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task DeleteStackTemplateAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.DeleteStackTemplateAsync(""));
    }

    // ----- PublishStackTemplateAsync -----

    [Fact]
    public async Task PublishStackTemplateAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "tmpl-1",
                name = "my-template",
                display_name = "My Template",
                description = "",
                version = "1.0.0",
                visibility = "public",
                publication_status = "submitted",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-02T00:00:00Z"
            }));
        });

        var result = await client.Stacks.PublishStackTemplateAsync("tmpl-1");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/templates/tmpl-1/publish", path);
        Assert.Equal("submitted", result.PublicationStatus);
    }

    [Fact]
    public async Task PublishStackTemplateAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.PublishStackTemplateAsync(""));
    }

    // ----- UnpublishStackTemplateAsync -----

    [Fact]
    public async Task UnpublishStackTemplateAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "tmpl-1",
                name = "my-template",
                display_name = "My Template",
                description = "",
                version = "1.0.0",
                visibility = "public",
                publication_status = "unpublished",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-02T00:00:00Z"
            }));
        });

        var result = await client.Stacks.UnpublishStackTemplateAsync("tmpl-1");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/templates/tmpl-1/unpublish", path);
        Assert.Equal("unpublished", result.PublicationStatus);
    }

    [Fact]
    public async Task UnpublishStackTemplateAsync_ThrowsOnEmptyId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.UnpublishStackTemplateAsync(""));
    }

    // ----- PreviewStackUpgradeAsync -----

    [Fact]
    public async Task PreviewStackUpgradeAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                stack_id = "stk-1",
                current_version = "1.0.0",
                target_version = "2.0.0",
                resource_changes = Array.Empty<object>()
            }));
        });

        var req = new PreviewStackUpgradeRequest { TargetVersion = "2.0.0" };
        var result = await client.Stacks.PreviewStackUpgradeAsync("stk-1", req);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/stk-1/upgrade/preview", path);
        Assert.Equal("stk-1", result.StackId);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("2.0.0", result.TargetVersion);
    }

    [Fact]
    public async Task PreviewStackUpgradeAsync_DeserializesResourceChanges()
    {
        var body = JsonSerializer.Serialize(new
        {
            stack_id = "stk-1",
            current_version = "1.0.0",
            target_version = "2.0.0",
            resource_changes = new[]
            {
                new
                {
                    symbolic_name = "pg_primary",
                    change_type = "upgrade",
                    from_version = "1.0.0",
                    to_version = "2.0.0",
                    description = "PostgreSQL 16 to 17",
                    requires_restart = true
                }
            },
            warnings = new[] { "Primary database will restart briefly." },
            estimated_monthly_cost = 185.00m
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.PreviewStackUpgradeAsync(
            "stk-1",
            new PreviewStackUpgradeRequest { TargetVersion = "2.0.0" });

        Assert.Single(result.ResourceChanges);
        Assert.Equal("pg_primary", result.ResourceChanges[0].SymbolicName);
        Assert.Equal("upgrade", result.ResourceChanges[0].ChangeType);
        Assert.True(result.ResourceChanges[0].RequiresRestart);
        Assert.Equal("PostgreSQL 16 to 17", result.ResourceChanges[0].Description);
        Assert.Single(result.Warnings!);
        Assert.Equal(185.00m, result.EstimatedMonthlyCost);
    }

    [Fact]
    public async Task PreviewStackUpgradeAsync_ThrowsOnEmptyStackId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.PreviewStackUpgradeAsync("", new PreviewStackUpgradeRequest { TargetVersion = "2.0.0" }));
    }

    [Fact]
    public async Task PreviewStackUpgradeAsync_ThrowsOnEmptyTargetVersion()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.PreviewStackUpgradeAsync("stk-1", new PreviewStackUpgradeRequest { TargetVersion = "" }));
    }

    // ----- ApplyStackUpgradeAsync -----

    [Fact]
    public async Task ApplyStackUpgradeAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "mig-1",
                stack_id = "stk-1",
                from_version = "1.0.0",
                to_version = "2.0.0",
                status = "Applying",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        var req = new ApplyStackUpgradeRequest { TargetVersion = "2.0.0" };
        var result = await client.Stacks.ApplyStackUpgradeAsync("stk-1", req);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/stacks/stk-1/upgrade", path);
        Assert.Equal("mig-1", result.Id);
        Assert.Equal("stk-1", result.StackId);
        Assert.Equal("1.0.0", result.FromVersion);
        Assert.Equal("2.0.0", result.ToVersion);
        Assert.Equal("Applying", result.Status);
    }

    [Fact]
    public async Task ApplyStackUpgradeAsync_ThrowsOnEmptyStackId()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.ApplyStackUpgradeAsync("", new ApplyStackUpgradeRequest { TargetVersion = "2.0.0" }));
    }

    [Fact]
    public async Task ApplyStackUpgradeAsync_ThrowsOnEmptyTargetVersion()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Stacks.ApplyStackUpgradeAsync("stk-1", new ApplyStackUpgradeRequest { TargetVersion = "" }));
    }

    // ----- LaunchStackAsync with TemplateId -----

    [Fact]
    public async Task LaunchStackAsync_AcceptsTemplateId()
    {
        string? body = null;
        using var client = BuildClient(req =>
        {
            body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Responses.Ok(JsonSerializer.Serialize(new
            {
                id = "stk-2",
                name = "custom-stack",
                template_name = "",
                template_id = "tmpl-99",
                template_version = "1.0.0",
                status = "Pending",
                created_at = "2026-06-01T00:00:00Z",
                updated_at = "2026-06-01T00:00:00Z"
            }));
        });

        var result = await client.Stacks.LaunchStackAsync(new LaunchStackRequest
        {
            Name = "custom-stack",
            TemplateName = string.Empty,
            TemplateId = "tmpl-99",
            AcceptedMonthlyCost = 200m
        });

        Assert.Equal("stk-2", result.Id);
        Assert.Equal("tmpl-99", result.TemplateId);
        Assert.Contains("tmpl-99", body);
    }

    // ----- Stack.SourceTemplateId / SourcePublisherOrgId -----

    [Fact]
    public async Task GetStackAsync_DeserializesMarketplaceFields()
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "stk-mkt",
            name = "community-stack",
            template_name = "community-rag",
            template_id = "tmpl-pub",
            source_template_id = "tmpl-pub",
            source_publisher_org_id = "org-publisher",
            template_version = "2.0.0",
            status = "Running",
            created_at = "2026-06-01T00:00:00Z",
            updated_at = "2026-06-01T00:00:00Z"
        });

        using var client = BuildClient(_ => Responses.Ok(body));
        var result = await client.Stacks.GetStackAsync("stk-mkt");

        Assert.NotNull(result);
        Assert.Equal("tmpl-pub", result!.TemplateId);
        Assert.Equal("tmpl-pub", result.SourceTemplateId);
        Assert.Equal("org-publisher", result.SourcePublisherOrgId);
    }
}
