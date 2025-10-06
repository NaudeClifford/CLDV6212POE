using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Services;
using System.Text.Json;

namespace ABCRetails.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionApi _api;

        public OrderController(IFunctionApi api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetCustomersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Create() //Create action
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

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
            if (ModelState.IsValid)
            {
                try
                {
                    //get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {

                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    if (product.StockAvailable < model.Quantity) //check stock availability
                    {

                        ModelState.AddModelError("Quantity", $"Insufficient stock. Availble: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    var order = new Order //create order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = model.OrderDate,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _storageService.AddEntityAsync(order);

                    //Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    var orderMessage = new //Send queue message for new order
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = customer.Name + " " + customer.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status

                    };

                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                    var stockMessage = new //send stock update message
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity,
                        NewStock = product.StockAvailable,
                        UpdateBy = "Order System",
                        UpdateDate = DateTime.UtcNow

                    };

                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }

            }
            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id) //Details action
        {

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {

                try
                {
                    // Validate keys
                    if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
                    {
                        ModelState.AddModelError("", "Missing Order keys. Please reload and try again.");
                        return View(order);
                    }

                    // Fetch original entity from storage
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                    {
                        ModelState.AddModelError("", "Order not found.");

                        return NotFound();
                    }

                    originalOrder.OrderDate = order.OrderDate;
                    originalOrder.Status = order.Status;

                    // Update entity in storage
                    await _storageService.UpdateEntityAsync(originalOrder);
                    // Fetch existing entity from Azure Table
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
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

                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
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

        [HttpPost]
        public async Task<JsonResult> UpdateOrderStatus([FromBody] UpdateOrderStatusModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Id) || string.IsNullOrEmpty(model.NewStatus))
            {
                return Json(new { success = false, message = "Invalid input." });
            }

            var allowedStatuses = new[] { "Submitted", "Processing", "Completed", "Cancelled" };
            if (!allowedStatuses.Contains(model.NewStatus))
            {
                return Json(new { success = false, message = "Invalid status value." });
            }

            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", model.Id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                var previousStatus = order.Status;
                order.Status = model.NewStatus;
                await _storageService.UpdateEntityAsync(order);

                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = model.NewStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {model.NewStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task PopulateDropdowns(OrderCreateViewModel model) //Populate dropdowns
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }

    }

    public class UpdateOrderStatusModel
    {
        public string Id { get; set; }
        public string NewStatus { get; set; }
    }
}
