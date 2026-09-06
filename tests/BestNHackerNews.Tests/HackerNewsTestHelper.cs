using BestNHackerNews.Models;

namespace BestNHackerNews.Tests
{
    internal static class HackerNewsTestHelper
    {
        public static HackerNewsItem CreateHackerNewsItem(
            int id,
            int score,
            bool deleted = false,
            bool dead = false) => new()
        {
            Id = id,
            Title = $"Story {id}",
            Url = $"https://test.com/{id}",
            By = "author",
            Time = 123456789,
            Score = score,
            Descendants = 0,
            Deleted = deleted,
            Dead = dead,
        };
    }
}
