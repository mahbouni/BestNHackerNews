using BestNHackerNews.Models;
using BestNHackerNews.Services;
using static BestNHackerNews.Tests.HackerNewsTestHelper;

namespace BestNHackerNews.Tests;

public class StoryCacheTests
{
    [Test]
    public void GetTopStories_ReturnsEmpty_WhenNoSnapshotSet()
    {
        var cache = CreateCache();

        var result = cache.GetTopStories(5);

        Assert.That(result, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetTopStories_ReturnsEmpty_WhenNIsNotPositive(int n)
    {
        var cache = CreateCache();
        cache.SetSnapshot(new Dictionary<int, HackerNewsItem> { [1] = CreateHackerNewsItem(1, score: 10) });

        var result = cache.GetTopStories(n);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTopStories_ReturnsItemsSortedByScoreDescending()
    {
        var cache = CreateCache();
        cache.SetSnapshot(new Dictionary<int, HackerNewsItem>
        {
            [1] = CreateHackerNewsItem(1, score: 10),
            [2] = CreateHackerNewsItem(2, score: 30),
            [3] = CreateHackerNewsItem(3, score: 20),
        });

        var result = cache.GetTopStories(10);

        Assert.That(result.Select(r => r.Score), Is.EqualTo(new[] { 30, 20, 10 }));
    }

    [Test]
    public void GetTopStories_ReturnsFewerThanN_WhenFewerAreAvailable()
    {
        var cache = CreateCache();
        cache.SetSnapshot(new Dictionary<int, HackerNewsItem>
        {
            [1] = CreateHackerNewsItem(1, score: 10),
            [2] = CreateHackerNewsItem(2, score: 20),
        });

        var result = cache.GetTopStories(10);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetTopStories_MapsFieldsCorrectly()
    {
        var cache = CreateCache();
        cache.SetSnapshot(new Dictionary<int, HackerNewsItem>
        {
            [42] = new HackerNewsItem
            {
                Id = 42,
                Title = "A uBlock Origin update was rejected from the Chrome Web Store",
                Url = "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
                By = "ismaildonmez",
                Time = 1577836800, // 2020-01-01T00:00:00Z
                Score = 1716,
                Descendants = 572,
            },
        });

        var story = cache.GetTopStories(1).Single();

        Assert.That(story.Title, Is.EqualTo("A uBlock Origin update was rejected from the Chrome Web Store"));
        Assert.That(story.Uri, Is.EqualTo("https://github.com/uBlockOrigin/uBlock-issues/issues/745"));
        Assert.That(story.PostedBy, Is.EqualTo("ismaildonmez"));
        Assert.That(story.Time, Is.EqualTo("2020-01-01T00:00:00+00:00"));
        Assert.That(story.Score, Is.EqualTo(1716));
        Assert.That(story.CommentCount, Is.EqualTo(572));
    }

    [Test]
    public void GetTopStories_FallsBackToDiscussionUrl_WhenUrlIsNull()
    {
        var cache = CreateCache();
        cache.SetSnapshot(new Dictionary<int, HackerNewsItem>
        {
            [99] = new HackerNewsItem { Id = 99, Title = "Ask HN: something", Url = null, By = "someone", Time = 1577836800, Score = 5, Descendants = 1 },
        });

        var story = cache.GetTopStories(1).Single();

        Assert.That(story.Uri, Is.EqualTo("https://news.ycombinator.com/item?id=99"));
    }

    private static StoryCache CreateCache() =>
        new(Microsoft.Extensions.Options.Options.Create(new HackerNewsOptions()));
}
