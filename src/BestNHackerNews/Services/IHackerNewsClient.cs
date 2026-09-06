using BestNHackerNews.Models;

namespace BestNHackerNews.Services;

public interface IHackerNewsClient
{
    Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
}
