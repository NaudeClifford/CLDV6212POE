using System.ComponentModel.DataAnnotations;
using ABCRetails.Models;
using Microsoft.AspNetCore.Identity;

namespace PROG6212POE.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        // Role navigation
        public int RoleId { get; set; }
        public Role? Role { get; set; }

        // Orders
        public ICollection<Order>? Orders { get; set; }
    }
}
