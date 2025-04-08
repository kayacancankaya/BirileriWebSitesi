using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Bundle
    {
        [Required] 
        public string BundleCode { get; set; } = string.Empty;
        [Required] 
        public string ProductCode { get; set; } = string.Empty;
        [Required] 
        public int Quantity { get; set; } = 0;
    }
}
