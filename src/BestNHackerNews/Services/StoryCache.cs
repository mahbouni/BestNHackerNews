using BestNHackerNews.Models;
using Microsoft.Extensions.Options;

namespace BestNHackerNews.Services
{
    public class StoryCache(IOptions<HackerNewsOptions> options) : IStoryCache
    {
        private volatile IReadOnlyDictionary<int, HackerNewsItem>? _snapshot;

        public IReadOnlyList<StoryResponse> GetTopStories(int n)
        {
            var snapshot = _snapshot;
            if (snapshot is null || n <= 0)
            {
                return [];
            }

            return snapshot.Values
                .OrderByDescending(item => item.Score)
                .Take(n)
                .Select(ToResponse)
                .ToList();
        }

        public void SetSnapshot(IReadOnlyDictionary<int, HackerNewsItem> items) =>
            _snapshot = items;

        private StoryResponse ToResponse(HackerNewsItem item) => new()
        {
            Title = item.Title ?? string.Empty,
            Uri = item.Url ?? string.Format(options.Value.DiscussionUrlTemplate, item.Id),
            PostedBy = item.By ?? string.Empty,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Score = item.Score,
            CommentCount = item.Descendants,
        };
    }
}
