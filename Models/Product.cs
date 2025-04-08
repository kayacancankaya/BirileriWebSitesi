using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Product
    {
        [Key]
        [Required]
        public string ProductCode { get; set; }
        [Required]
        public string ProductName { get; set; }
        public string? ProductNameEng { get; set; }
        public string? ProductNameSpa { get; set; }

        [Required]
        public string ProductDescription { get; set; }

        [Required]
        public int CatalogId { get; set; }
        [Required]
        public decimal BasePrice { get; set; } = decimal.Zero;
        [Required]
        public int Popularity { get; set; } = 0;
        public int MinOrder { get; set; }
        public string? Banner { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Depth { get; set; }

        [Required]
        public string MetaTitle { get; set; } = string.Empty;

        [Required]
        public string MetaDescription { get; set; } = string.Empty;

        [Required]
        public string MetaKeywords { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EditedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; } 
        public int? EditedBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CatalogId))]
        public virtual Catalog Catalog { get; set; } = new Catalog();
        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
        public virtual ICollection<RelatedProduct> RelatedProducts { get; set; } = new List<RelatedProduct>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    }

}
