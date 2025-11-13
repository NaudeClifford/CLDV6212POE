using ABCRetails.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
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

            // Trim inputs to avoid whitespace issues
            model.UserNameOrEmail = model.UserNameOrEmail?.Trim();
            model.Password = model.Password?.Trim();

            // Try to find user by username or email
            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
                       ?? (model.UserNameOrEmail.Contains("@")
                           ? await _userManager.FindByEmailAsync(model.UserNameOrEmail)
                           : null);

            if (user != null)
            {
                // Check password without signing in yet
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

                if (passwordCheck.Succeeded)
                {
                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

                    // Redirect to returnUrl if provided and local
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);

                    // Default fallback
                    return RedirectToAction("Index", "Home");
                }
            }

            // If we get here, login failed
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }


        //Signin
        [HttpGet]
        public IActionResult Signin() => View(new RegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Signin(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if user already exists
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(model);
            }

            // Create user
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            // Only assign ShippingAddress if role is Customer
            if (model.Role == "Customer" && !string.IsNullOrEmpty(model.ShippingAddress))
            {
                user.ShippingAddress = model.ShippingAddress;
            }

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure role exists
                if (!await _roleManager.RoleExistsAsync(model.Role))
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));

                // Assign role
                await _userManager.AddToRoleAsync(user, model.Role);

                // Automatically sign in
                await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
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
