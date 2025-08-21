using System.Linq.Expressions;
using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }

        public IActionResult Create() //Create action
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Customer customer) 
        {
            if (ModelState.IsValid) 
            {
                try
                {
                    await _storageService.AddEntityAsync(customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id) //Edit action
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    await _storageService.UpdateEntityAsync(customer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        [HttpPost]

        public async Task<IActionResult> Delete(string id) //Delete action
        {

                try
                {
                    await _storageService.DeleteEntityAsync<Customer>("Customer", id);
                    TempData["Success"] = "Customer deleted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error deleting customer: {ex.Message}");
                }
               
            return RedirectToAction(nameof(Index));
        }

    }
}
