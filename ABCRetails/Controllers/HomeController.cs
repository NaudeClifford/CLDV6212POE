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
            try
            {
                var productsTask = await _api.GetProductsAsync();
                var customersTask = await _api.GetCustomersAsync();
                var ordersTask = await _api.GetOrdersAsync();

                await Task.WhenAll(productsTask, customersTask, ordersTask);

                var products = productsTask.Result ?? new List<Product>();
                var customers = customersTask.Result ?? new List<Customer>();
                var orders = ordersTask.Result ?? new List<Order>();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = products.Take(5).ToList(),
                    ProductCount = products.Count,
                    CustomerCount = customers.Count,
                    OrderCount = orders.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not load data. Please try again";
                return View(new HomeViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
