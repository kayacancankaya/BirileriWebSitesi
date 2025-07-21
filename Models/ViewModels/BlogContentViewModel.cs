namespace BirileriWebSitesi.Models.ViewModels
{
    public class BlogContentViewModel
    {
        public BlogPost BlogPost { get; set; } = new BlogPost();
        
        public BlogContentSidebarViewModel BlogContentSidebarViewModel { get; set; } = new BlogContentSidebarViewModel();
    }
}
