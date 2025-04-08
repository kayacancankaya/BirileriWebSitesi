using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class VariantAttribute
    {
        [Key]
        public required string VariantAttributeCode { get; set; } = string.Empty;
        
        public required string VariantCode { get; set; } = string.Empty;
        public required string VariantAttributeName { get; set; } = string.Empty;
        public string? VariantAttributeNameEng { get; set; }
        public string? VariantAttributeNameSpa { get; set; }

        // Navigation Property
        public virtual Variant Variant { get; set; } = new Variant();
    }

}
