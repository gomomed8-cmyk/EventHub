
using EventHub.Application.Contracts;
using StackExchange.Redis;

namespace EventHub.Infrastructure.Redis;

public sealed class PublicEventsCacheVersionService:IPublicEventsCacheVersionService
{
    private const string VersionKey ="EventHub:PublicEvents:Version";

    private readonly IConnectionMultiplexer _multiplexer;

    public PublicEventsCacheVersionService(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task<string> GetOrCreateVersionAsync()
    {
        var database = _multiplexer.GetDatabase();

        var currentVersion =
            await database.StringGetAsync(VersionKey);

        if (currentVersion.HasValue)
        {
            return currentVersion.ToString();
        }

        var newVersion = Guid.NewGuid().ToString("N");

        var created = await database.StringSetAsync(
            VersionKey,
            newVersion,
            when: When.NotExists);

        if (created)
        {
            return newVersion;
        }

        currentVersion =
            await database.StringGetAsync(VersionKey);

        if (currentVersion.HasValue)
        {
            return currentVersion.ToString();
        }

        throw new InvalidOperationException(
            "Failed to create or retrieve the public events cache version.");
    }

    public async Task RefreshVersionAsync()
    {
        var database = _multiplexer.GetDatabase();

        var newVersion = Guid.NewGuid().ToString("N");

        await database.StringSetAsync(
            VersionKey,
            newVersion);
    }
}
