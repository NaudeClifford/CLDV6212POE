using System.ComponentModel.DataAnnotations;

namespace PROG6212POE.Models
{
    public class LoginViewModel
    {

        [Required]
        [Display(Name = "Email or Username")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
