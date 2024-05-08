using HackerNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ILogger<StoriesController> _logger;

        public StoriesController(IHackerNewsService hackerNewsService, ILogger<StoriesController> logger)
        {
            _hackerNewsService = hackerNewsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNewestStories([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? title = null)
        {
            try
            {
                var response = await _hackerNewsService.GetNewestStoriesAsync(pageNumber, pageSize, title);
                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                // Network-related errors, like if the base URL is not reachable
                _logger.LogError($"HttpRequestException: {ex.Message}");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service is temporarily unavailable.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"InvalidOperationException: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while fetching stories.");
            }
            catch (Exception ex)
            {
                // All other exceptions
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }
    }
}