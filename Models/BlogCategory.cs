using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BirileriWebSitesi.Models
{
    public class BlogCategory
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation property for related blog posts
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }
}
