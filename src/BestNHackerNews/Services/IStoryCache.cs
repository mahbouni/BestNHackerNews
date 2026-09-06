using BestNHackerNews.Models;

namespace BestNHackerNews.Services
{
    public interface IStoryCache
    {
        void SetSnapshot(IReadOnlyDictionary<int, HackerNewsItem> items);

        IReadOnlyList<StoryResponse> GetTopStories(int n);
    }
}
