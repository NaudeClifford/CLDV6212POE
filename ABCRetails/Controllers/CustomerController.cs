using ABCRetails.Data;
using ABCRetails.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public CustomerController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LIST CUSTOMERS
        [Authorize(Roles = "Admin, Customer")]
        public async Task<IActionResult> Index(string? searchString)
        {
            IQueryable<Customer> customersQuery = _context.Customers.AsNoTracking();

            if (User.IsInRole("Customer"))
            {
                // Only show the logged-in customer
                var username = User.Identity.Name;
                customersQuery = customersQuery.Where(c => c.Username == username);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim().ToLower();
                customersQuery = customersQuery.Where(c =>
                    c.Name.ToLower().Contains(searchString) ||
                    c.Surname.ToLower().Contains(searchString) ||
                    c.Username.ToLower().Contains(searchString)
                );
            }

            var customers = await customersQuery.ToListAsync();
            ViewData["SearchString"] = searchString;
            return View(customers);
        }


        // EDIT CUSTOMER (GET)
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // If user is a customer, make sure it's their own account
            if (User.IsInRole("Customer") && customer.Username != User.Identity.Name)
                return Forbid(); // 403 Forbidden

            return View(customer);
        }

        // EDIT CUSTOMER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Edit(string id, Customer model)
        {
            if (id != model.Id)
                return BadRequest();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // If user is a customer, make sure it's their own account
            if (User.IsInRole("Customer") && customer.Username != User.Identity.Name)
                return Forbid();

            if (!ModelState.IsValid)
                return View(model);

            // Update only allowed fields
            customer.Name = model.Name;
            customer.Surname = model.Surname;
            customer.Email = model.Email;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Customer updated successfully!";

            return RedirectToAction(User.IsInRole("Admin") ? nameof(Index) : "Edit", new { id = customer.Id });
        }

        // DELETE CUSTOMER
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // Customer can only delete their own account
            if (User.IsInRole("Customer") && customer.Username != User.Identity.Name)
                return Forbid();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            // If a customer deleted themselves, log them out
            if (User.IsInRole("Customer"))
                return RedirectToAction("Logout", "Account");

            TempData["Success"] = "Customer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

    }
}
