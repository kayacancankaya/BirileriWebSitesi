namespace BirileriWebSitesi.Models.ViewModels
{
    public class ProductDetailedViewModel
    {
        public Product Product { get; set; } = new Product();
        public Dictionary<string, string> GlobalVariants { get; set; } = new();
        public Dictionary<string, string> VariantAttributes { get; set; } = new();
        public ProductDetailedVariantInfoViewModel ProductVariantInfo { get; set; } = new();
        public ProductDetailedVariantImageViewModel ProductVariantImage { get; set; } = new();
        public IEnumerable<Product> RelatedProducts { get; set; } = new List<Product>();
        public IEnumerable<Product> PopularProducts { get; set; } = new List<Product>();
    }
}
