using BirileriWebSitesi.Models;
namespace BirileriWebSitesi.Interfaces
{
    public interface IBlogInterface
    {
        Task<IEnumerable<BlogPost>> GetBlogPostsAsync(int pageNumber, int pageSize);
        Task<int> CountBlogPostsAsync();
        Task<BlogPost?> GetBlogPostAsync(string seoUrl);

        Task<IEnumerable<BlogPost>> GetPopularBlogPostsAsync();
        Task<IEnumerable<BlogCategory>> GetBlogCategoriesAsync();
        Task<IEnumerable<BlogTag>> GetPopularBlogTagsAsync();
        Task<int> GetSelectedCategoryId(int id);
     
    }
}
