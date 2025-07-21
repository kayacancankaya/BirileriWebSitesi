namespace BirileriWebSitesi.Models.ViewModels
{
    public class BlogContentSidebarViewModel
    {
        public IEnumerable<BlogPost> PopularPosts { get; set; } = new List<BlogPost>();
        public IEnumerable<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
        public IEnumerable<BlogTag> Tags { get; set; } = new List<BlogTag>();

        public string SearchQuery { get; set; } = string.Empty;
        public int SelectedCategoryId { get; set; }
    }
}
