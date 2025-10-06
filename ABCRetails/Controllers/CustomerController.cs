using ABCRetails.Models;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Services;


namespace ABCRetails.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IFunctionApi _api;

        public CustomerController(IFunctionApi api) => _api = api;
        
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["SearchString"] = searchString;
            
            var customers = await _api.GetCustomersAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    c.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Surname.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return View(customers);

        }


        public IActionResult Create() //Create action
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Customer customer) 
        {
            if (!ModelState.IsValid) return View(customer);

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

        public async Task<IActionResult> Edit(string id) //Edit action
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            
            var customer = await _api.GetCustomerAsync(id);
            
            if (customer == null) return NotFound();
           
            return View(customer);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {

            if (!ModelState.IsValid) return View(customer);

            try
            {
                await _api.UpdateCustomerAsync(customer.CustomerId, customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        [HttpPost]

        public async Task<IActionResult> Delete(string id) //Delete action
        {

            try
            {
                await _api.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting customer: {ex.Message}");
            }
               
            return RedirectToAction(nameof(Index));
        }
    }
}
