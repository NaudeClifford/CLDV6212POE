using ABCRetails.Data;
using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Controllers
{
    public class ProductController : Controller
    {
        private readonly IFunctionApi _api;
        private readonly ApplicationDbContext _context;

        public ProductController(IFunctionApi api, ApplicationDbContext context)
        {
            _api = api;
            _context = context;
        }
        // LIST PRODUCTS
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var products = await _api.GetProductsAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products
                    .Where(p => p.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                             || p.Id.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewData["CurrentFilter"] = searchString;
            return View(products);
        }

        // CREATE PRODUCT (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        // CREATE PRODUCT (POST)
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Optional: Validate file if one is uploaded
            if (imageFile != null && imageFile.Length > 0)
            {
                // Check file size (limit: 5MB)
                long maxSize = 5 * 1024 * 1024;
                if (imageFile.Length > maxSize)
                {
                    ModelState.AddModelError("ImageFile", "File size must be under 5MB.");
                    return View(model);
                }

                // Check allowed types
                var extension = Path.GetExtension(imageFile.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Only JPG, PNG, or GIF files are allowed.");
                    return View(model);
                }
            }

            try
            {
                // 1️⃣ Save to database first
                var product = new Product
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Price = model.Price,
                    StockAvailable = model.StockAvailable,
                    ImageUrl = model.ImageUrl
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 2️⃣ Save to API / cloud storage
                Product savedProduct;
                if (imageFile != null && imageFile.Length > 0)
                {
                    savedProduct = await _api.CreateProductAsync(product, imageFile);
                }
                else
                {
                    savedProduct = await _api.CreateProductAsync(product, null);
                }

                // 3️⃣ Optional: Update DB with cloud info if returned
                product.ImageUrl = savedProduct.ImageUrl;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(model);
            }
        }

        // EDIT PRODUCT (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var product = await _api.GetProductAsync(id);
            return product == null ? NotFound() : View(product);
        }

        // EDIT PRODUCT (POST)
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            try
            {
                // Update DB first
                _context.Update(product);
                await _context.SaveChangesAsync();

                // Update cloud via API
                var updatedFromApi = await _api.UpdateProductAsync(product.Id, product, imageFile);

                // Sync ImageUrl if changed
                if (!string.IsNullOrEmpty(updatedFromApi.ImageUrl) && updatedFromApi.ImageUrl != product.ImageUrl)
                {
                    product.ImageUrl = updatedFromApi.ImageUrl;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Product '{product.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product);
            }
        }


        // DELETE PRODUCT
        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid product ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Delete from database
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                }

                // Delete from API/cloud
                await _api.DeleteProductAsync(id);

                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
