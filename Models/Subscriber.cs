using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models
{
    public class Subscriber
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Primary Key

        [Required]
        [MaxLength(255)] // Limit email length to 255 characters
        public string EmailAddress { get; set; } = string.Empty; // Subscriber's email address

        [Required]
        public DateTime SubscribedOn { get; set; } = DateTime.Now; // Subscription date and time
    }

}
