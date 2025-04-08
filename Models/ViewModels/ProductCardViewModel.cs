namespace BirileriWebSitesi.Models.ViewModels
{
    public class ProductCardViewModel
    {
        public IEnumerable<Product> products { get; set; } = new List<Product>();
        public PaginationViewModel pagination { get; set; } = new PaginationViewModel();
    }
}
