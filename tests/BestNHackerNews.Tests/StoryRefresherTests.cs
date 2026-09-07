using BestNHackerNews.Models;
using BestNHackerNews.Services;
using Microsoft.Extensions.Logging.Abstractions;
using static BestNHackerNews.Tests.HackerNewsTestHelper;

namespace BestNHackerNews.Tests;

public class StoryRefresherTests
{
    [Test]
    public async Task RunOnceAsync_PopulatesCache_WithFetchedItems()
    {
        var cache = new FakeStoryCache();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => Task.FromResult(new[] { 1, 2 }),
            GetItemHandler = (id, _) => Task.FromResult<HackerNewsItem?>(CreateHackerNewsItem(id, score: id * 10)),
        };

        var result = await CreateRefresher(client, cache).RunOnceAsync(CancellationToken.None);

        Assert.That(result, Is.True);
        Assert.That(cache.LastSnapshot, Is.Not.Null);
        Assert.That(cache.LastSnapshot!.Keys, Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(cache.LastSnapshot[1].Score, Is.EqualTo(10));
        Assert.That(cache.LastSnapshot[2].Score, Is.EqualTo(20));
    }

    [Test]
    public async Task RunOnceAsync_FiltersOutDeletedAndDeadItems()
    {
        var cache = new FakeStoryCache();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => Task.FromResult(new[] { 1, 2, 3 }),
            GetItemHandler = (id, _) => Task.FromResult<HackerNewsItem?>(id switch
            {
                1 => CreateHackerNewsItem(1, score: 10),
                2 => CreateHackerNewsItem(2, score: 20, deleted: true),
                3 => CreateHackerNewsItem(3, score: 30, dead: true),
                _ => throw new InvalidOperationException(),
            }),
        };
         
        await CreateRefresher(client, cache).RunOnceAsync(CancellationToken.None);

        Assert.That(cache.LastSnapshot, Is.Not.Null);
        Assert.That(cache.LastSnapshot!.Keys, Is.EquivalentTo(new[] { 1 }));
    }

    [Test]
    public async Task RunOnceAsync_DoesNotUpdateCache_WhenIdListFetchThrows()
    {
        var cache = new FakeStoryCache();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => throw new HttpRequestException("boom"),
        };

        var result = await CreateRefresher(client, cache).RunOnceAsync(CancellationToken.None);

        Assert.That(result, Is.False);
        Assert.That(cache.LastSnapshot, Is.Null);
    }

    [Test]
    public async Task RunOnceAsync_DoesNotUpdateCache_WhenAnItemFetchThrows()
    {
        var cache = new FakeStoryCache();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => Task.FromResult(new[] { 10, 20 }),
            GetItemHandler = (id, _) => id == 20
                ? throw new HttpRequestException("boom")
                : Task.FromResult<HackerNewsItem?>(CreateHackerNewsItem(id, score: 50)),
        };

        var result = await CreateRefresher(client, cache).RunOnceAsync(CancellationToken.None);

        Assert.That(result, Is.False);
        Assert.That(cache.LastSnapshot, Is.Null);
    }

    [Test]
    public async Task RunOnceAsync_SkipsCycle_WhenPreviousCycleStillRunning()
    {
        var idsGate = new TaskCompletionSource<int[]>();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => idsGate.Task,
        };
        var refresher = CreateRefresher(client, new FakeStoryCache());

        var firstCycle = refresher.RunOnceAsync(CancellationToken.None);
        var secondResult = await refresher.RunOnceAsync(CancellationToken.None);

        Assert.That(secondResult, Is.False);
        Assert.That(client.GetBestStoryIdsCallCount, Is.EqualTo(1));

        idsGate.SetResult([]);
        await firstCycle;
    }

    [Test]
    public async Task RunOnceAsync_LimitsConcurrentItemFetches_ToConfiguredMax()
    {
        const int maxConcurrent = 3;
        var currentConcurrency = 0;
        var maxObservedConcurrency = 0;
        var gate = new object();

        var ids = Enumerable.Range(1, 20).ToArray();
        var client = new FakeHackerNewsClient
        {
            GetBestStoryIdsHandler = _ => Task.FromResult(ids),
            GetItemHandler = async (id, _) =>
            {
                lock (gate)
                {
                    currentConcurrency++;
                    maxObservedConcurrency = Math.Max(maxObservedConcurrency, currentConcurrency);
                }

                await Task.Delay(20);

                lock (gate)
                {
                    currentConcurrency--;
                }

                return CreateHackerNewsItem(id, score: id);
            },
        };

        await CreateRefresher(client, new FakeStoryCache(), maxConcurrent).RunOnceAsync(CancellationToken.None);

        Assert.That(maxObservedConcurrency, Is.EqualTo(maxConcurrent),
            "expected the semaphore to both cap concurrency at, and actually reach, the configured max");
    }

    private static IStoryRefresher CreateRefresher(IHackerNewsClient client, IStoryCache cache, int maxConcurrent = 20)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new HackerNewsOptions { MaxConcurrentRequests = maxConcurrent });
        return new StoryRefresher(client, cache, options, NullLogger<StoryRefresher>.Instance);
    }

    private class FakeStoryCache : IStoryCache
    {
        public IReadOnlyDictionary<int, HackerNewsItem>? LastSnapshot { get; private set; }

        public void SetSnapshot(IReadOnlyDictionary<int, HackerNewsItem> items) => LastSnapshot = items;

        public IReadOnlyList<StoryResponse> GetTopStories(int n) => throw new NotSupportedException();
    }

    private class FakeHackerNewsClient : IHackerNewsClient
    {
        public Func<CancellationToken, Task<int[]>>? GetBestStoryIdsHandler { get; set; }

        public Func<int, CancellationToken, Task<HackerNewsItem?>>? GetItemHandler { get; set; }

        public int GetBestStoryIdsCallCount;

        public Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref GetBestStoryIdsCallCount);
            return GetBestStoryIdsHandler?.Invoke(cancellationToken) ?? Task.FromResult(Array.Empty<int>());
        }

        public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
        {
            return GetItemHandler?.Invoke(id, cancellationToken) ?? Task.FromResult<HackerNewsItem?>(null);
        }
    }
}
