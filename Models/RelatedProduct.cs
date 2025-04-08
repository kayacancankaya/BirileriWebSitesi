using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class RelatedProduct
    {

        public required string ProductCode { get; set; }
        public required string RelatedProductCode { get; set; }
        public required string RelationshipType { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; } = new Product();
        public virtual Product RelatedProducts { get; set; } = new Product();
        
    }

}
