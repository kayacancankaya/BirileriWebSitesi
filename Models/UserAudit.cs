using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class UserAudit
    {
        [Key]
        public string UserId { get; set; }  // Matches IdentityUser.Id

        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string Ip { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
    }
}
