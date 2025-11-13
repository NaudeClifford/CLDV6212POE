using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ABCRetails.Models
{
    public class User : IdentityUser
    {
        [Required]
        public required string FirstName { get; set; }
       
        [Required]
        public required string LastName { get; set; }

        // Optional shipping address for customers
        public string? ShippingAddress { get; set; }

        public Admin? Admin { get; set; }
            public Customer? Customer { get; set; }
            public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        
    }
}
