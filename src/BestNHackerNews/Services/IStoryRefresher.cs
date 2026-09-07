namespace BestNHackerNews.Services
{
    public interface IStoryRefresher
    {
        Task<bool> RunOnceAsync(CancellationToken cancellationToken);
    }
}
