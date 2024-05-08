using HackerNewsAPI.Models;
using HackerNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HackerNewsAPI.Controllers
{
    public class StoriesControllerTests
    {
        private readonly Mock<IHackerNewsService> _mockHackerNewsService;
        private readonly StoriesController _storiesController;
        private readonly Mock<ILogger<StoriesController>> _mockLogger;

        public StoriesControllerTests()
        {
            _mockHackerNewsService = new Mock<IHackerNewsService>();
            _mockLogger = new Mock<ILogger<StoriesController>>();
            _storiesController = new StoriesController(_mockHackerNewsService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetNewestStories_WithTitleFilter_ReturnsFilteredStories()
        {
            // Arrange
            var stories = new List<Story>
            {
                new() { Id = 1, Title = "Tech News" },
                new() { Id = 2, Title = "Another Tech Update" }
            };
            _mockHackerNewsService.Setup(s => s.GetNewestStoriesAsync(1, 10, "Tech"))
                .ReturnsAsync(new PagedStoriesResponse { Stories = stories, TotalStories = 100 });

            // Act
            var response = await _storiesController.GetNewestStories(1, 10, "Tech");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response);
            var returnedResponse = Assert.IsType<PagedStoriesResponse>(okResult.Value);
            Assert.Equal(2, returnedResponse.Stories.Count());
            Assert.Equal(100, returnedResponse.TotalStories);
        }
    }
}