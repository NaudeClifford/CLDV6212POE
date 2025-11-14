using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Data;
using ABCRetails.Models;

namespace ABCRetails.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // INDEX - SHOW OWN ADMIN PROFILE
        public async Task<IActionResult> Index()
        {
            // Get current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // EDIT - GET
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // EDIT - POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }
}
