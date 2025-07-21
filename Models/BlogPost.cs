using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace BirileriWebSitesi.Models
{
    public class BlogPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
        public string ImageUrl { get; set; } = string.Empty;
        [ForeignKey(nameof(CategoryId))]
        public int CategoryId { get; set; } 
        public string Slug { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public string SeoImage { get; set; } = string.Empty;
        public string SeoUrl { get; set; } = string.Empty;
        public int SeenCount { get; set; } = 0;
        public ICollection<BlogTag> TagsList { get; set; } = new List<BlogTag>();
        public BlogCategory Category { get; set; } = new BlogCategory();
    }
}
