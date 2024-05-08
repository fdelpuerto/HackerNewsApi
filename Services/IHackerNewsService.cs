using HackerNewsAPI.Models;

namespace HackerNewsAPI.Services
{
    public interface IHackerNewsService
    {
        Task<PagedStoriesResponse> GetNewestStoriesAsync(int pageNumber, int pageSize, string? title = null);
    }
}