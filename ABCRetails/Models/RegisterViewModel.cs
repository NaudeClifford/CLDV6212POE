using System.ComponentModel.DataAnnotations;

namespace ABCRetails.Models
{
    public class RegisterViewModel
    {
        [Required, Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required, Display(Name = "Role")]
        public string Role { get; set; } = "Customer"; // Default role

        [Display(Name = "Shipping Address (Optional)")]
        public string? ShippingAddress { get; set; } = string.Empty;
    }
}
