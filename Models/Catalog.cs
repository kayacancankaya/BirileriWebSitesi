using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Catalog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Primary Key

        [Required]
        [MaxLength(255)] // Limit email length to 255 characters
        public string CatalogName { get; set; } = string.Empty; // Subscriber's email address

        [Required]

        public string CatalogDescription { get; set; } = string.Empty; // Subscriber's email address
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
