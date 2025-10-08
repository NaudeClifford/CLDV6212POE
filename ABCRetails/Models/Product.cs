using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ABCRetails.Models
{
    public class Product 
    {

        [Display(Name = "Product ID")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }


        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [BindNever]

        [Display(Name = "Image Url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
