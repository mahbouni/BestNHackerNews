using BestNHackerNews.Services;

namespace BestNHackerNews.Tests;

internal class StoryRefreshServiceTests
{
    [Test]
    public async Task ExecuteAsync_CallsRefresherOnEachTick()
    {
        var refresher = new FakeStoryRefresher();
        var options = Microsoft.Extensions.Options.Options.Create(new HackerNewsOptions { RefreshIntervalSeconds = 1 });
        var service = new StoryRefreshService(refresher, options);

        await service.StartAsync(CancellationToken.None);
        try
        {
            await refresher.WaitForCallsAsync(count: 2, timeout: TimeSpan.FromSeconds(5));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.That(refresher.CallCount, Is.GreaterThanOrEqualTo(2));

    }

    private class FakeStoryRefresher : IStoryRefresher
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task RunOnceAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.CompletedTask;
        }

        public async Task WaitForCallsAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (CallCount < count && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }
    }
}
