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

        // ------------------------------
        // LIST ALL ORDERS
        // ------------------------------
        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        // ------------------------------
        // CREATE ORDER (GET)
        // ------------------------------
        public async Task<IActionResult> Create()
        {
            var viewModel = new OrderCreateViewModel
            {
                Customers = await _api.GetCustomersAsync(),
                Products = await _api.GetProductsAsync()
            };

            return View(viewModel);
        }

        // ------------------------------
        // CREATE ORDER (POST)
        // ------------------------------
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
                // Get customer and product details from API
                var customer = await _api.GetCustomerAsync(model.CustomerId);
                var product = await _api.GetProductAsync(model.ProductId);

                if (customer == null || product == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                // Call API to create order
                await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);

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

        // ------------------------------
        // DETAILS
        // ------------------------------
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var order = await _api.GetOrderAsync(id);
            return order == null ? NotFound() : View(order);
        }

        // ------------------------------
        // EDIT ORDER (GET)
        // ------------------------------
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var order = await _api.GetOrderAsync(id);
            if (order == null) return NotFound();

            // Populate enum dropdown for status in the view
            ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus))
                                   .Cast<OrderStatus>()
                                   .Select(s => new { Value = s.ToString(), Text = s.ToString() })
                                   .ToList();

            return View(order);
        }

        // ------------------------------
        // EDIT ORDER (POST)
        // ------------------------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus))
                                       .Cast<OrderStatus>()
                                       .Select(s => new { Value = s.ToString(), Text = s.ToString() })
                                       .ToList();
                return View(order);
            }

            try
            {
                // Update order status via API
                await _api.UpdateOrderStatusAsync(order.Id, order.Status.ToString());

                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus))
                                       .Cast<OrderStatus>()
                                       .Select(s => new { Value = s.ToString(), Text = s.ToString() })
                                       .ToList();
                return View(order);
            }
        }

        // ------------------------------
        // DELETE ORDER
        // ------------------------------
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

        // ------------------------------
        // AJAX: Get product price & stock
        // ------------------------------
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
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

        // ------------------------------
        // AJAX: Update Order Status
        // ------------------------------
        [HttpPost("Order/UpdateOrderStatus/{id}")]
        public async Task<JsonResult> UpdateOrderStatus(string id, [FromBody] JsonElement body)
        {
            try
            {
                var newStatusString = body.GetProperty("status").GetString();

                if (!Enum.TryParse<OrderStatus>(newStatusString, out var newStatus))
                {
                    return Json(new { success = false, message = "Invalid status value" });
                }

                await _api.UpdateOrderStatusAsync(id, newStatus.ToString());

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ------------------------------
        // HELPER: Populate dropdowns
        // ------------------------------
        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}
