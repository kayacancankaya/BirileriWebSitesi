namespace BirileriWebSitesi.Models.ViewModels
{
    public class BlogViewModel
    {
        public IEnumerable<BlogPost>? BlogPosts { get; set; }
        public int TotalBlogPosts { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
