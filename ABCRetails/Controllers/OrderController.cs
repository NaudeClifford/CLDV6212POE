using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ABCRetails.Services;
using System.Text.Json;

namespace ABCRetails.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
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

        [HttpPost]
        [ValidateAntiForgeryToken]

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

        public async Task<IActionResult> Edit(string id) //Edit action
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    await _storageService.UpdateEntityAsync(order);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]

        public async Task<IActionResult> Delete(string id) //Delete action
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

        [HttpPost]

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

        public async Task<JsonResult> UpdateOrderStatus(string id, string newStatus) //Upload order status action
        {

            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

                //Send queue message for the status update
                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
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
}
