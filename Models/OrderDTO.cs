namespace BirileriWebSitesi.Models
{
    public class OrderDTO
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; } = 0;
        public int Units { get; set; } = 0;
    }
}
