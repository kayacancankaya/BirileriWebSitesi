using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models
{
    public class OrderRequestModel
    {
        public AddressDTO? ShipToAddress { get; set; }
        public AddressDTO? BillingAddress { get; set; }
        public List<OrderDTO> OrderItems { get; set; } 
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool UpdateUserInfo { get; set; } = false;
    }
}
