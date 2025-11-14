using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly IFunctionApi _api;

        public CartController(CartService cartService, IFunctionApi api)
        {
            _cartService = cartService;
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cartItems = await _cartService.GetCartItemsAsync(userId);
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var product = await _api.GetProductAsync(productId);
            if (product == null) return NotFound();

            await _cartService.AddToCartAsync(userId, product, quantity);
            TempData["Success"] = "Product added to cart!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(string productId, int quantity)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _cartService.UpdateQuantityAsync(userId!, productId, quantity);
            TempData["Success"] = "Cart updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(string productId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _cartService.RemoveItemAsync(userId!, productId);
            TempData["Success"] = "Item removed!";
            return RedirectToAction(nameof(Index));
        }
    }
}
