using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models
{
    public class OrderRequestModel
    {
        public Address ShipToAddress { get; set; }
        public Address BillingAddress { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public bool UpdateUserInfo { get; set; }
    }
}
