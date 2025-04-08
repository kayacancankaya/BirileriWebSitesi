using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirileriWebSitesi.Models.OrderAggregate
{

    public class OrderItem 
    {
        public string ProductCode { get; set; }
        [Required]
        public decimal UnitPrice { get;  set; }
        [Required]
        public int Units { get;  set; }
        [Required]
        [ForeignKey(nameof(Order))]
        public int OrderId { get;  set; }

#pragma warning disable CS8618 // Required by Entity Framework
        public OrderItem() { }

        public OrderItem (string productCode, int quantity, decimal unitPrice)
        {
           ProductCode = productCode;
           UnitPrice = unitPrice;
           Units = quantity;     
        }
        public void AddQuantity(int quantity)
        {
            Units += quantity;
        }

        public virtual ProductVariant ProductVariant { get; set; } = new();

    }

}
