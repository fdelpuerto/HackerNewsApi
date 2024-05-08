namespace HackerNewsAPI.Models
{
    public class PagedStoriesResponse
    {
        public int TotalStories { get; set; }
        public IEnumerable<Story> Stories { get; set; } = [];
    }
}
