namespace FoundryDB.SDK.AppJobs;

/// <summary>
/// Operations on jobs defined on app services.
/// </summary>
public class AppJobsApi
{
    private readonly FoundryDBClient _client;

    internal AppJobsApi(FoundryDBClient client)
    {
        _client = client;
    }
}
