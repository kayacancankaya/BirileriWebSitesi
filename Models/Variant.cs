using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Variant
    {
        [Key]
        [Required]
        public string VariantCode { get; set; } = string.Empty;
        [Required]
        public string VariantName { get; set; } = string.Empty;
        public string? VariantNameEng { get; set; }
        public string? VariantNameSpa { get; set; }

        // Navigation Properties
        public virtual ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
    }

}
