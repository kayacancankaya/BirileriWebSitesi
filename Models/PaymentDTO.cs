using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models
{
    public class PaymentDTO
    {
        public Order? Order { get; set; }
        public string? PaymentType { get; set; }
        public string? CreditCardNumber { get; set; }
        public string? ExpMonth { get; set; }
        public string? ExpYear { get; set; }
        public string? CVV { get; set; }
           
    }
}
