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

            try
            {
                // Save to database first
                var product = new Product
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Price = model.Price,
                    StockAvailable = model.StockAvailable,
                    ImageUrl = model.ImageUrl,
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Save to cloud storage
                if (imageFile != null && imageFile.Length > 0)
                {
                    var savedProduct = await _api.CreateProductAsync(product, imageFile);
                    product.ImageUrl = savedProduct.ImageUrl;

                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();
                }

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
        public async Task<IActionResult> Edit(string id, Product model, IFormFile? imageFile)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Load the existing product from the DB
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return NotFound();

                // Update product fields
                product.ProductName = model.ProductName;
                product.Description = model.Description;
                product.Price = model.Price;
                product.StockAvailable = model.StockAvailable;
                product.ImageUrl = model.ImageUrl;

                // Handle new image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Save new image to cloud storage
                    var savedProduct = await _api.CreateProductAsync(product, imageFile);

                    // Delete old image if it exists
                    if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                    {
                        try
                        {
                            await _api.DeleteProductAsync(product.Id); // Or a specific DeleteImageAsync method
                        }
                        catch
                        {
                            // Log error but continue
                            Console.WriteLine("Failed to delete old image from cloud storage");
                        }
                    }

                    // Update ImageUrl
                    product.ImageUrl = savedProduct.ImageUrl;
                }

                // Save changes to the database
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Product '{product.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "This product was updated or deleted by someone else. Please reload and try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(model);
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
