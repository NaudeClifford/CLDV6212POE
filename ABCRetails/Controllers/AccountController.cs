using ABCRetails.Data;
using ABCRetails.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.UserNameOrEmail = model.UserNameOrEmail?.Trim();
            model.Password = model.Password?.Trim();

            // Find by username OR email
            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
                       ?? (model.UserNameOrEmail.Contains("@")
                           ? await _userManager.FindByEmailAsync(model.UserNameOrEmail)
                           : null);

            if (user != null)
            {
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(
                    user, model.Password, false);

                if (passwordCheck.Succeeded)
                {
                    await _signInManager.SignInAsync(user, model.RememberMe);

                    // Set welcome message based on role
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        TempData["WelcomeMessage"] = "Welcome Admin!";
                    else if (await _userManager.IsInRoleAsync(user, "Customer"))
                        TempData["WelcomeMessage"] = "Welcome Customer!";

                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }




        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Signin(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Create Identity user
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            // Customers get a shipping address
            if (model.Role == "Customer")
                user.ShippingAddress = model.ShippingAddress;

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            // Assign role
            await _userManager.AddToRoleAsync(user, model.Role);

            // SAVE to your own tables
            if (model.Role == "Customer")
            {
                var customer = new Customer
                {
                    Id = user.Id,
                    User = user,
                    Name = user.FirstName,
                    Surname = user.LastName,
                    Email = user.Email,
                    Username = user.UserName,
                    ShippingAddress = user.ShippingAddress
                };

                _context.Customers.Add(customer);
            }
            else if (model.Role == "Admin")
            {
                var admin = new Admin
                {
                    UserId = user.Id,
                    User = user,
                    Name = user.FirstName,
                    Surname = user.LastName,
                    Email = user.Email,
                    Username = user.UserName,
                    Role = "Admin"
                };

                _context.Admins.Add(admin);
            }

            await _context.SaveChangesAsync();

            // Auto login and redirect
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["WelcomeMessage"] =
                model.Role == "Admin" ? "Welcome Admin" : "Welcome Customer";

            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Login", "Account");
        }
    }
}
