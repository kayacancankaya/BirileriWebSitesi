using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using Microsoft.EntityFrameworkCore;
namespace BirileriWebSitesi.Services
{
    public class BlogService : IBlogInterface
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BlogService> _logger;
        public BlogService(ApplicationDbContext context, ILogger<BlogService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IEnumerable<BlogPost>> GetBlogPostsAsync(int pageNumber, int pageSize)
        {
            try
            {

                IEnumerable<BlogPost> blogPosts = await _context.BlogPosts
                                                .Where(bp => bp.IsPublished)
                                                .OrderByDescending(bp => bp.PublishedDate)
                                                .Skip((pageNumber - 1) * pageSize)
                                                .Take(pageSize)
                                                .Include(bp => bp.Category)
                                                .ToListAsync();
                return blogPosts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Enumerable.Empty<BlogPost>();
            }
        }
        public async Task<int> CountBlogPostsAsync()
        {
            try
            {

                int result = await _context.BlogPosts
                                                .Where(bp => bp.IsPublished)
                                                .CountAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return -1;
            }
        }
        public async Task<BlogPost?> GetBlogPostAsync(string seoUrl)
        {
            try
            {

                BlogPost? blogPost = await _context.BlogPosts
                                                .FirstOrDefaultAsync(bp => bp.Slug == seoUrl);
                List<string> tags = blogPost.Tags.Split(',')
                                                  .Select(t => t.Trim())
                                                  .ToList();
                foreach (var tag in tags)
                {
                    BlogTag? blogTag = await _context.BlogTags
                                                .FirstOrDefaultAsync(bt => bt.Name == tag);
                    if (blogTag != null)
                    {
                        blogPost.TagsList.Add(blogTag);
                    }
                }
                return blogPost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        public async Task<IEnumerable<BlogPost>> GetPopularBlogPostsAsync()
        {
            try
            {

                IEnumerable<BlogPost> blogPosts = await _context.BlogPosts
                                                .Where(bp => bp.IsPublished)
                                                .OrderByDescending(bp => bp.SeenCount)
                                                .Take(3)
                                                .ToListAsync();
                return blogPosts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Enumerable.Empty<BlogPost>();
            }
        }
        public async Task<IEnumerable<BlogCategory>> GetBlogCategoriesAsync()
        {
            try
            {

                IEnumerable<BlogCategory> blogCategories = await _context.BlogCategories
                                                .ToListAsync();
                return blogCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Enumerable.Empty<BlogCategory>();
            }
        }
        public async Task<IEnumerable<BlogTag>> GetPopularBlogTagsAsync()
        {
            try
            {

                IEnumerable<BlogTag> blogTags = await _context.BlogTags
                                                .OrderByDescending(bt => bt.PostCount)
                                                .Take(5)
                                                .ToListAsync();
                return blogTags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Enumerable.Empty<BlogTag>();
            }
        }
        public async Task<int> GetSelectedCategoryId(int id)
        {
            try
            {

                return await _context.BlogPosts
                                                .Where(b => b.Id == id)
                                                .Select(b => b.CategoryId)
                                                .FirstOrDefaultAsync();
    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return 0;
            }
        }
    }
}
