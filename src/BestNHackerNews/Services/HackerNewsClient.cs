using BestNHackerNews.Models;

namespace BestNHackerNews.Services;

public class HackerNewsClient(HttpClient httpClient) : IHackerNewsClient
{
    public async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await httpClient.GetFromJsonAsync<int[]>("beststories.json", cancellationToken);
        return ids ?? [];
    }

    public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        return httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
    }
}
