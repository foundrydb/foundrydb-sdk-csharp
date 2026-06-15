namespace FoundryDB.SDK.Webhooks;

/// <summary>
/// Operations on organisation webhook endpoints.
/// </summary>
public class WebhooksApi
{
    private readonly FoundryDBClient _client;

    internal WebhooksApi(FoundryDBClient client)
    {
        _client = client;
    }
}
