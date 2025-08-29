namespace BirileriWebSitesi.Interfaces
{
    public interface IProductService 
    {
        Task<decimal> GetPriceAsync(string productCode);
        Task<string> GetProductNameAsync(string productCode);
        Task<string> GetImagePathAsync(string productCode);
        Task<string> GetProductSlugAsync(string productCode);
    }
}
