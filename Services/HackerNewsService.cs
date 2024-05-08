using HackerNewsAPI.Common;
using HackerNewsAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HackerNewsAPI.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly CacheSettings _cacheSettings;
   
        public HackerNewsService(HttpClient httpClient, IMemoryCache cache, IOptions<CacheSettings> cacheSettings,
            IOptions<HackerNewsApiSettings> hackerNewsApiSettings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
            
            var hackerNewsApiSettingsValue = hackerNewsApiSettings?.Value ?? 
                throw new ArgumentNullException(nameof(hackerNewsApiSettings));
            _httpClient.BaseAddress = new Uri(hackerNewsApiSettingsValue.BaseUrl);
        }

        public async Task<PagedStoriesResponse> GetNewestStoriesAsync(int pageNumber, int pageSize, string? title = null)
        {
            // Fetch all new story IDs
            var response = await _httpClient.GetAsync("newstories.json");
            response.EnsureSuccessStatusCode();

            var idsJson = await response.Content.ReadAsStringAsync();
            var ids = JsonConvert.DeserializeObject<int[]>(idsJson);

            var validStories = new List<Story>();
            int totalFilteredStories = 0;

            if (ids != null)
            {
                // Fetch all stories concurrently
                var tasks = ids.Select(GetStoryAsync);
                var allStories = await Task.WhenAll(tasks);

                // Filter by title if provided
                validStories = allStories.Where(story => story != null).ToList();
                if (!string.IsNullOrEmpty(title))
                {
                    validStories = validStories.Where(story =>
                        story!.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                totalFilteredStories = validStories.Count;

                // Apply pagination
                validStories = validStories.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }

            return new PagedStoriesResponse
            {
                TotalStories = totalFilteredStories,
                Stories = validStories
            };
        }

        private async Task<Story> GetStoryAsync(int id)
        {
            var cacheKey = string.Format(_cacheSettings.StoryCacheKey, id);
            if (!_cache.TryGetValue(cacheKey, out Story? story))
            {
                var response = await _httpClient.GetAsync($"item/{id}.json");
                response.EnsureSuccessStatusCode();

                var storyJson = await response.Content.ReadAsStringAsync();
                story = JsonConvert.DeserializeObject<Story>(storyJson);
                if (story == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize the story with ID {id}.");
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(_cacheSettings.SlidingExpirationMinutes));
                _cache.Set(cacheKey, story, cacheEntryOptions);
            }

            return story ?? throw new InvalidOperationException($"Story with ID {id} not found.");
        }
    }
}