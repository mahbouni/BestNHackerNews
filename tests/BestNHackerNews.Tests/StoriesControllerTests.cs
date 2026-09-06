using BestNHackerNews.Controllers;
using BestNHackerNews.Models;
using BestNHackerNews.Services;
using Microsoft.AspNetCore.Mvc;

namespace BestNHackerNews.Tests;

public class StoriesControllerTests
{
    [TestCase(0)]
    [TestCase(-5)]
    public void GetBest_ReturnsBadRequest_WhenNIsNotPositive(int n)
    {
        var controller = new StoriesController(new FakeStoryCache());

        var result = controller.GetBest(n);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetBest_ReturnsOkWithCacheResult_WhenNIsPositive()
    {
        var expected = new List<StoryResponse>
        {
            new() { Title = "t", Uri = "u", PostedBy = "p", Time = "2020-01-01T00:00:00+00:00", Score = 1, CommentCount = 0 },
        };
        var cache = new FakeStoryCache { Response = expected };
        var controller = new StoriesController(cache);

        var result = controller.GetBest(5);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        Assert.That(cache.RequestedN, Is.EqualTo(5));
    }

    private class FakeStoryCache : IStoryCache
    {
        public IReadOnlyList<StoryResponse> Response { get; init; } = [];
        public int? RequestedN { get; private set; }

        public void SetSnapshot(IReadOnlyDictionary<int, HackerNewsItem> items) => throw new NotSupportedException();

        public IReadOnlyList<StoryResponse> GetTopStories(int n)
        {
            RequestedN = n;
            return Response;
        }
    }
}
