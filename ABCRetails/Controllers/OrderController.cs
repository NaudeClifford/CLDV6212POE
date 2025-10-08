using System.Text.Json;
using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using ABCRetails.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionApi _api;

        public OrderController(IFunctionApi api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        public async Task<IActionResult> Create() //Create action
        {
            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }
            try
            {

                //get customer and product details
                var customer = await _api.GetCustomerAsync(model.CustomerId);
                var product = await _api.GetProductAsync(model.ProductId);

                if (customer == null || product == null)
                {

                    ModelState.AddModelError(string.Empty, "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity) //check stock availability
                {

                    ModelState.AddModelError("Quantity", $"Insufficient stock. Availble: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                var saved = await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);

                TempData["Success"] = "Order created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) 
            {
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                await PopulateDropdowns(model);
                return View(model);
            }

        }        

        //Details
        public async Task<IActionResult> Details(string id) //Details action
        {

            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var order = await _api.GetOrderAsync(id);

            return order == null ? NotFound() : View(order);
        }

        //Edit
        public async Task<IActionResult> Edit(string id)
        {

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var order = await _api.GetOrderAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        //Edit Post

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Model error on '{key}': {error.ErrorMessage}");
                    }
                }
                return View(order);
            }

            try
            {

                // Update entity in storage
                await _api.UpdateOrderStatusAsync(order.Id, order.Status.ToString());

                TempData["Success"] = "Order updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                return View(order);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId) //Get product price action
        {
            try
            {

                var product = await _api.GetProductAsync(productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost("Order/UpdateOrderStatus/{id}")]
        public async Task<JsonResult> UpdateOrderStatus(string id, [FromBody] JsonElement body)
        {
            try
            {
                var newStatus = body.GetProperty("status").GetString();
                await _api.UpdateOrderStatusAsync(id, newStatus);

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public async Task PopulateDropdowns(OrderCreateViewModel model) //Populate dropdowns
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}
