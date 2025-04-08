using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Discount
    {
        [Key]
        public int DiscountID { get; set; }
        [Required]
        public string ProductCode { get; set; }
        [Required]
        public string DiscountType { get; set; }  // e.g., 'Percentage' or 'Fixed'
        [Required]
        public decimal DiscountAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; } = 1;

        // Navigation Property
        [Required]
        public virtual Product Product { get; set; } = new Product();
        public virtual ProductVariant ProductVariant { get; set; } = new ProductVariant();
    }

}
