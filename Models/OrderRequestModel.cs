using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models
{
    public class OrderRequestModel
    {
        public AddressDTO ShipToAddress { get; set; } = new AddressDTO();
        public AddressDTO BillingAddress { get; set; } = new AddressDTO();
        public List<OrderDTO> OrderItems { get; set; } = new List<OrderDTO>();
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool UpdateUserInfo { get; set; } = false;
        public decimal TotalAmount { get; set; } = 0;
        public int InstallmentAmount { get; set; } = 0;
        public int PaymentType { get; set; } = 1;
        public string? CardHolderName { get; set; }
        public string? CreditCardNumber { get; set; }
        public string? ExpMonth { get; set; }
        public string? ExpYear { get; set; }
        public string? CVV { get; set; }

    }

}
