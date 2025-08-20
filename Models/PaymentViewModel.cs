using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models
{
    public class PaymentViewModel
    {
        public Address? ShipToAddress { get; set; }
        public Address? BillingAddress { get; set; }
        public List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; } = 0;
        public int InstallmentAmount { get; set; } = 1;
        public int OrderId { get; set; } = 0;
        public int PaymentType { get; set; } = 1;
        public decimal ShippingCost { get; set; } = decimal.Zero;

    }
}
