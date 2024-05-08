namespace HackerNewsAPI.Common
{
    public class CacheSettings
    {
        public int SlidingExpirationMinutes { get; set; } = 10; // Default value
        public string StoryCacheKey { get; set; } = "HackerNewsStory-{0}";
    }
}
