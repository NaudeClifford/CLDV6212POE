﻿using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class ProductController : Controller
    {
        private readonly IFunctionApi _api;

        public ProductController(IFunctionApi api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var products = await _api.GetProductsAsync();
            return View(products);
        }

        public IActionResult Create() //Create action
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {

            if (!ModelState.IsValid) return View(product);
                        
            try
            {
                var saved = await _api.CreateProductAsync(product, imageFile);
                
                TempData["Success"] = $"Product '{saved.ProductName}' created successfully with price {saved.Price:C}!";
                
                return RedirectToAction(nameof(Index));
            }
            
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                
                return View(product);
            }
        }

        public async Task<IActionResult> Edit(string id) //Edit action
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var product = await _api.GetProductAsync(id);
 
            return product == null ? NotFound() : View(product);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {

            if (!ModelState.IsValid) return View(product);
            
            try
            {

                var updated = await _api.UpdateProductAsync(product.ProductId, product, imageFile);
                TempData["Success"] = $"Product '{updated.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product);
            }
        }

        [HttpPost]

        public async Task<IActionResult> Delete(string id) //Delete action
        {

            try
            {
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
