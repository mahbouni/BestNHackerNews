using BestNHackerNews.Models;
using Microsoft.Extensions.Options;

namespace BestNHackerNews.Services;

public class StoryRefresher
(
    IHackerNewsClient client,
    IStoryCache cache,
    IOptions<HackerNewsOptions> options,
    ILogger<StoryRefresher> logger) : IStoryRefresher
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Skipping refresh cycle; previous cycle is still running.");
            return false;
        }

        try
        {
            var ids = await client.GetBestStoryIdsAsync(cancellationToken);
            var items = await FetchAllAsync(ids, cancellationToken);
            cache.SetSnapshot(items);
            logger.LogInformation("Refreshed story cache with {Count} stories.", items.Count);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Refresh cycle failed; keeping previous snapshot.");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<int, HackerNewsItem>> FetchAllAsync(int[] ids, CancellationToken cancellationToken)
    {
        using var throttle = new SemaphoreSlim(options.Value.MaxConcurrentRequests);

        var fetches = ids.Select(async id =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                return await client.GetItemAsync(id, cancellationToken);
            }
            finally
            {
                throttle.Release();
            }
        });

        var results = await Task.WhenAll(fetches);

        var items = new Dictionary<int, HackerNewsItem>();
        foreach (var item in results)
        {
            if (item is not null && !item.Deleted && !item.Dead)
            {
                items[item.Id] = item;
            }
        }

        return items;
    }
}
