using System.Diagnostics;
using ABCRetails.Models;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Models.ViewModels;
using ABCRetails.Services;

namespace ABCRetails.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionApi _api;

        public HomeController(IFunctionApi api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var products = await _api.GetProductsAsync();
            var customers = await _api.GetCustomersAsync();
            var orders = await _api.GetOrdersAsync();

            await Task.WhenAll(products, customers, orders);

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                ProductCount = products.Count,
                CustomerCount = customers.Count,
                OrderCount = orders.Count
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                await _api.GetCustomersAsync(); // trigger initialization
                TempData["Success"] = "Azure Storage initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize storage: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
