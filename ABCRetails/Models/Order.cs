using System.ComponentModel.DataAnnotations;
using PROG6212POE.Models;

namespace ABCRetails.Models
{
    public enum OrderStatus
    {
        Submitted,
        Processing,
        Completed,
        Cancelled
    }

    public class Order
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public double UnitPrice { get; set; }

        public double TotalPrice => Quantity * UnitPrice;

        [Required]
        public string Status { get; set; } = "pending";

        // Navigation properties
        public User Customer { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
