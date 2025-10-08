using System.ComponentModel.DataAnnotations;
using System.Globalization;

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

        public string PriceString { get; set; } = string.Empty;

        [Display(Name = "Price")]

        public double Price
        {
            get
            {
                return double.TryParse(PriceString, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
            }
            set
            {
                PriceString = value.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image Url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
