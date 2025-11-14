using System.ComponentModel.DataAnnotations;

namespace ABCRetails.Models
{
    public class Customer
    {
       
        [Display(Name = "Customer ID")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
       
        [Required]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    }
}
