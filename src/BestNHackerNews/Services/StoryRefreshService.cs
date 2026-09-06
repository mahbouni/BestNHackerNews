using Microsoft.Extensions.Options;

namespace BestNHackerNews.Services;

public class StoryRefreshService(
    IStoryRefresher refresher,
    IOptions<HackerNewsOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.RefreshIntervalSeconds));

        while (await timer.WaitForNextTickAsync(cancelToken))
        {
            await refresher.RunOnceAsync(cancelToken);
        }
    }
}
