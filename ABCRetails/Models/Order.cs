using System.ComponentModel.DataAnnotations;

namespace ABCRetails.Models
{
    public enum OrderStatus
    {
        Submitted,
        Procossing,
        Completed,
        Cancelled
    }
    public class Order
    {

        [Display(Name = "Order ID")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow.Date;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public double UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";
    }

}
