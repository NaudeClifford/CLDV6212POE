using ABCRetails.Models;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;

namespace ABCRetails.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IFunctionApi _api;

        public CustomerController(IFunctionApi api) => _api = api;

        // LIST CUSTOMERS WITH SEARCH
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["SearchString"] = searchString;

            var customers = await _api.GetCustomersAsync();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                customers = customers.Where(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Surname) && c.Surname.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Username) && c.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return View(customers);
        }

        // CREATE CUSTOMER (GET)
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE CUSTOMER (POST)
        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                await _api.CreateCustomerAsync(customer);
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        // EDIT CUSTOMER (GET)
        [Authorize(Roles = "Customer")]

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var customer = await _api.GetCustomerAsync(id);
            return customer == null ? NotFound() : View(customer);
        }

        // EDIT CUSTOMER (POST)
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Customer")]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                await _api.UpdateCustomerAsync(customer.Id, customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        // DELETE CUSTOMER
        [Authorize(Roles = "Customer")]
        [Authorize(Roles = "Admin")]

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid customer ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _api.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
