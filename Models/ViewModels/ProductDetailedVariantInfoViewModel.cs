namespace BirileriWebSitesi.Models.ViewModels
{
    public class ProductDetailedVariantInfoViewModel
    {
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;
        public decimal VariantPrice { get; set; } = decimal.Zero;
        public string SelectedVariantAttribute { get; set; } = string.Empty;

    }
}
