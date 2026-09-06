namespace BestNHackerNews.Models;

public record StoryResponse
{
    public required string Title { get; init; }
    public required string Uri { get; init; }
    public required string PostedBy { get; init; }
    public required string Time { get; init; }
    public required int Score { get; init; }
    public required int CommentCount { get; init; }
}
