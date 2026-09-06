namespace BestNHackerNews
{
    public class HackerNewsOptions
    {
        public const string SectionName = "HackerNews";

        public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";
        public int RefreshIntervalSeconds { get; set; } = 60;
        public int MaxConcurrentRequests { get; set; } = 20;
        public int RequestTimeoutSeconds { get; set; } = 10;
    }
}
