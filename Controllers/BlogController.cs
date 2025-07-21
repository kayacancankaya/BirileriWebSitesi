using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BirileriWebSitesi.Controllers
{
    public class BlogController : Controller
    {
        private readonly ILogger<BlogController> _logger;
        private readonly IBlogInterface _blogInterface;
        public BlogController(ILogger<BlogController> logger,
                                IBlogInterface blogInterface)
        {
            _logger = logger;
            _blogInterface = blogInterface;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var blogPosts = await _blogInterface.GetBlogPostsAsync(1,10);
                int totalPosts = await _blogInterface.CountBlogPostsAsync();
                BlogViewModel blogViewModel = new BlogViewModel
                {
                    BlogPosts = blogPosts,
                    TotalBlogPosts = totalPosts
                };
                return View(blogViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public async Task<IActionResult> LoadMoreBlogPosts(int pageNumber, int pageSize)
        {
            try
            {
                var blogPosts = await _blogInterface.GetBlogPostsAsync(pageNumber, pageSize);

                return PartialView("_PartialBlogCardList", blogPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public async Task<IActionResult> BlogPost(string path)
        {
            try
            {
                var blogPost = await _blogInterface.GetBlogPostAsync(path);
                return View("BlogPost", blogPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public async Task<IActionResult> _PartialBlogSidebar(int id)
        {
            try
            {
                BlogContentSidebarViewModel sidebar = new();
                sidebar.PopularPosts = await _blogInterface.GetPopularBlogPostsAsync();
                sidebar.Categories = await _blogInterface.GetBlogCategoriesAsync();
                sidebar.Tags = await _blogInterface.GetPopularBlogTagsAsync();
                sidebar.SelectedCategoryId = await _blogInterface.GetSelectedCategoryId(id);
                return PartialView("_PartialBlogSidebar", sidebar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
    }
}
