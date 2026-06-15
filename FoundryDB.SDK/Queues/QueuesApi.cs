namespace FoundryDB.SDK.Queues;

/// <summary>
/// Operations on message queues on managed PostgreSQL services.
/// </summary>
public class QueuesApi
{
    private readonly FoundryDBClient _client;

    internal QueuesApi(FoundryDBClient client)
    {
        _client = client;
    }
}
