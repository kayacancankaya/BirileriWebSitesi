using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using BirileriWebSitesi.Models.OrderAggregate;
namespace BirileriWebSitesi.Models
{
    public class PaymentLog
    {
        [Key]
        [Required]
        public string? ConversationId { get; set; }
        [Required]
        public int OrderId { get; set; } 
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        public string? Price { get; set; }
        public string? PaidPrice { get; set; }
        public string? IyziCommissionRateAmount { get; set; }
        public string? IyziCommissionFee { get; set; }
        public string? CardAssociation { get; set; } 
        public string? CardFamily { get; set; }
        public string? CardType { get; set; }
        public string? BinNumber { get; set; }
        public string? LastFourDigits { get; set; } 
        public string? Status { get; set; }
        public DateTime PaidAt { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = new Order();
    }

}
