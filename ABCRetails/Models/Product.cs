using System.ComponentModel.DataAnnotations;

namespace ABCRetails.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public double Price { get; set; }

        [Required]
        public int StockAvailable { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Order>? Orders { get; set; }
    }
}
