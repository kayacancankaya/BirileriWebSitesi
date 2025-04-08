using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirileriWebSitesi.Models
{
    public class ProductVariant
    {
        [Required]
        [Key]
        public string ProductCode { get; set; } = string.Empty;
        [Required]
        public string ProductName { get; set; } = string.Empty;
        [Required]
        public string ProductNameEng { get; set; } = string.Empty;
        [Required]
        public string ProductNameSpa { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public decimal Price { get; set; } = decimal.Zero;
        public int Popularity { get; set; } = 0;
        public int Favourited { get; set; } = 0;
        public decimal Rating { get; set; } = 0;
        public int Wishlisted { get; set; } = 0;
        public int TotalReviewed { get; set; } = 0;
        public DateTime CreatedAt { get; set; } =DateTime.Now;
        public DateTime? EditedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } =true;
        public int CreatedBy { get; set; } = 1;
        public int? EditedBy { get; set; } = 1;
        public string ImagePath { get; set; } = string.Empty;
        [ForeignKey(nameof(Product))]
        public string BaseProduct { get; set; } = string.Empty;

        public virtual Product Product { get; set; } = new();
        public ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }

}
