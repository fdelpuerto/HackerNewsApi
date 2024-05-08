using HackerNewsAPI.Common;
using HackerNewsAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace HackerNewsAPI.Services
{
    public class HackerNewsServiceTests
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly HttpClient _httpClient;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<ICacheEntry> _mockCacheEntry;
        private readonly Dictionary<object, object> _cacheStore;
        private readonly IOptions<CacheSettings> _cacheSettings;
        private readonly IOptions<HackerNewsApiSettings> _hackerNewsApiSettings;
        private readonly HackerNewsService _hackerNewsService;

        public HackerNewsServiceTests()
        {
            // Load appsettings.Development.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
                .Build();

            // Retrieve settings from configuration
            _cacheSettings = Options.Create(configuration.GetSection("CacheSettings").Get<CacheSettings>());
            _hackerNewsApiSettings = Options.Create(configuration.GetSection("HackerNewsApi").Get<HackerNewsApiSettings>());

            // Setup MockHttp and simulate a response.
            _mockHttp = new MockHttpMessageHandler();
            _mockHttp.When($"{_hackerNewsApiSettings.Value.BaseUrl}newstories.json")
                     .Respond("application/json", "[123, 456, 789]");

            _mockHttp.When($"{_hackerNewsApiSettings.Value.BaseUrl}item/*.json")
                .Respond(request =>
                {
                    // Extracting the item ID from the URL
                    var itemId = request.RequestUri.Segments.Last().Replace(".json", "");

                    // Create the JSON content dynamically based on the item ID
                    var jsonContent = $@"{{""Id"": {itemId}, ""Title"": ""Title for {itemId}"", 
                        ""Url"": ""http://www.example.com/{itemId}""}}";

                    // Create the response message with the correct content type and content
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
                    };
                });

            _httpClient = new HttpClient(_mockHttp)
            {
                BaseAddress = new Uri(_hackerNewsApiSettings.Value.BaseUrl)
            };

            // Mock IMemoryCache and create a dictionary to act as the cache store.
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockCacheEntry = new Mock<ICacheEntry>();
            _cacheStore = [];

            // Mock the behavior of the TryGetValue method
            _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Returns((object key, out object value) => _cacheStore.TryGetValue(key, out value));

            // Mock CreateEntry method to return the mock cache entry
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    _mockCacheEntry.Setup(e => e.Key).Returns(key);
                    _mockCacheEntry.SetupProperty(e => e.Value);  // Allow setting Value
                    return _mockCacheEntry.Object;
                });

            // Simulate setting values in the cache entry
            _mockCacheEntry.SetupSet(e => e.Value = It.IsAny<object>()).Callback((object value) =>
            {
                _cacheStore[_mockCacheEntry.Object.Key] = value;
            });

            // Create HackerNewsService with mocked dependencies
            _hackerNewsService = new HackerNewsService(_httpClient, _mockMemoryCache.Object, _cacheSettings, 
                _hackerNewsApiSettings);
        }

        [Fact]
        public async Task GetNewestStoriesAsync_ShouldReturnStories()
        {
            // Act
            var response = await _hackerNewsService.GetNewestStoriesAsync(1, 3, "Title");

            // Assert
            Assert.NotNull(response.Stories);
            Assert.Equal(3, response.Stories.Count());
            Assert.All(response.Stories, story => Assert.Contains("Title", story.Title));
        }

        [Fact]
        public async Task GetNewestStoriesAsync_WithTitleFilter_ReturnsNoStoriesIfNotFound()
        {
            // Act
            var response = await _hackerNewsService.GetNewestStoriesAsync(1, 3, "NonExistent");

            // Assert
            Assert.NotNull(response.Stories);
            Assert.Empty(response.Stories);
        }

        [Fact]
        public async Task GetNewestStoriesAsync_ShouldRetrieveStoryFromCache()
        {
            // Arrange
            Story cachedStory = new Story { Id = 123, Title = "Title for 123", Url = "http://www.example.com/123" };
            _cacheStore.Add("HackerNewsStory-123", cachedStory);

            // Act
            var response = await _hackerNewsService.GetNewestStoriesAsync(1, 1);

            // Assert
            Assert.NotNull(response.Stories);
            Assert.Single(response.Stories);
            Assert.Equal(cachedStory.Title, response.Stories.First().Title);
        }

        [Fact]
        public async Task GetNewestStoriesAsync_WithTitleFilter_ShouldNotRetrieveStoryFromCache()
        {
            // Arrange
            Story cachedStory = new Story { Id = 123, Title = "Title for 123", Url = "http://www.example.com/123" };
            _cacheStore.Add("HackerNewsStory-123", cachedStory);

            // Act
            var response = await _hackerNewsService.GetNewestStoriesAsync(1, 1, "Nonexistent");

            // Assert
            Assert.NotNull(response.Stories);
            Assert.Empty(response.Stories);
        }
    }
}