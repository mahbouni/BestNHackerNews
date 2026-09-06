namespace BestNHackerNews.Services
{
    public interface IStoryRefresher
    {
        Task RunOnceAsync(CancellationToken cancellationToken);
    }
}
