using System.ComponentModel.DataAnnotations;
using ABCRetails.Models;

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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? CustomerId { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        public string? ProductId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;

        [Required]
        public string Status { get; set; } = "pending";

        // Navigation properties
        public Customer? Customer { get; set; }
        
        public Product? Product { get; set; }
    }
}
