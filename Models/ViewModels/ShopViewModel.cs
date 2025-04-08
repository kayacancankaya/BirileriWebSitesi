namespace BirileriWebSitesi.Models.ViewModels
{
    public class ShopViewModel
    {
        public ProductCardViewModel productCard {  get; set; } = new ProductCardViewModel();
        public IEnumerable<Catalog> Catalogs { get; set; } = new List<Catalog>();
        public IEnumerable<Product> PopularProducts { get; set; } = new List<Product>();
    }
}
