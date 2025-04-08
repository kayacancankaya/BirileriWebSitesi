namespace BirileriWebSitesi.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int TotalCount { get; set; } = 0;
        public static int PageSize { get; set; } = 9;
        public int CurrentPage { get; set; } = 1;

        public int TotalPage { get; set; } = 0;
    }
}
