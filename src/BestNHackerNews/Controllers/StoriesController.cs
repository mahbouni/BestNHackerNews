using BestNHackerNews.Models;
using BestNHackerNews.Services;
using Microsoft.AspNetCore.Mvc;

namespace BestNHackerNews.Controllers;

[ApiController]
[Route("api/stories")]
public class StoriesController(IStoryCache cache) : ControllerBase
{
    [HttpGet("best")]
    [ProducesResponseType(typeof(IReadOnlyList<StoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<StoryResponse>> GetBest([FromQuery] int n)
    {
        if (n <= 0)
        {
            return BadRequest("n must be a positive integer.");
        }

        return Ok(cache.GetTopStories(n));
    }
}
