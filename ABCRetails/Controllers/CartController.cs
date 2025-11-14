using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    [AllowAnonymous]
    public class CartController : Controller
    {
        private readonly IFunctionApi _api;

        public CartController(IFunctionApi api) => _api = api;
        public IActionResult Index()
        {
            // Show cart items
            return View();
        }

        // List Products
        public async Task<IActionResult> Products()
        {
            var products = await _api.GetProductsAsync();
            return View(products);
        }

        // Add to Cart
        [HttpPost]
        public IActionResult AddToCart(string productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductId = productId, Quantity = quantity });
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = "Product added to cart!";
            return RedirectToAction("Index", "Product");
        }

        // View Cart
        public async Task<IActionResult> ViewCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var productList = await _api.GetProductsAsync();

            var model = cart.Select(item =>
            {
                var product = productList.FirstOrDefault(p => p.Id == item.ProductId);
                return new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = product?.ProductName ?? "",
                    UnitPrice = (double)(product?.Price ?? 0),
                    Quantity = item.Quantity
                };
            }).ToList();

            return View(model);
        }

        // Place Order
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction(nameof(ViewCart));
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User not identified!";
                return RedirectToAction(nameof(ViewCart));
            }

            foreach (var item in cart)
            {
                await _api.CreateOrderAsync(userId, item.ProductId, item.Quantity);
            }

            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction(nameof(MyOrders));
        }

        // Customer Orders
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return RedirectToAction("Products");

            var allOrders = await _api.GetOrdersAsync();
            var myOrders = allOrders.Where(o => o.CustomerId == userId).ToList();

            return View(myOrders);
        }

        // Cancel Order

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            await _api.UpdateOrderStatusAsync(orderId, "Cancelled");
            TempData["Success"] = "Order cancelled successfully!";
            return RedirectToAction(nameof(MyOrders));
        }


        [HttpPost]
        public IActionResult EditCartItem(string productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = quantity;
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            TempData["Success"] = "Cart updated!";
            return RedirectToAction(nameof(ViewCart));
        }

        [HttpPost]
        public IActionResult RemoveCartItem(string productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            TempData["Success"] = "Item removed!";
            return RedirectToAction(nameof(ViewCart));
        }
    }
        // Session Helpers
        public static class SessionExtensions
        {
            public static void SetObjectAsJson(this ISession session, string key, object value)
                => session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));

            public static T? GetObjectFromJson<T>(this ISession session, string key)
            {
                var value = session.GetString(key);
                return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
            }
        }

        public class CartItem
        {
            public string ProductId { get; set; } = null!;
            public int Quantity { get; set; }
        }

        public class CartItemViewModel
        {
            public string ProductId { get; set; } = null!;
            public string ProductName { get; set; } = null!;
            public double UnitPrice { get; set; }
            public int Quantity { get; set; }
            public double Total => UnitPrice * Quantity;
        }
    }

