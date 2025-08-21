﻿using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Product>();
            return View(customers);
        }

        public IActionResult Create() //Create action
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (Request.Form.TryGetValue("Price", out var priceFormValue)) //price parsing
            {
                _logger.LogInformation("Raw price from form: '{PriceFormValue}'", priceFormValue.ToString());

                if (decimal.TryParse(priceFormValue, out var parsedPrice))
                {

                    product.Price = parsedPrice;
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);

                }
                else
                {

                    _logger.LogWarning("Failed to parse price: {PriceFormValue}", priceFormValue.ToString());
                }
            }

            _logger.LogInformation("Final product price: {Price}", product.Price);

            if(ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0) 
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    //Upload image
                    if (imageFile != null && imageFile.Length > 0) 
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }

                    await _storageService.AddEntityAsync(product);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");

                }
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string id) //Edit action
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {

            if (Request.Form.TryGetValue("Price", out var priceFormValue)) //price parsing
            {

                if (decimal.TryParse(priceFormValue, out var parsedPrice))
                {

                    product.Price = parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);

                }
                
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //get original product for ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    
                    if (originalProduct == null)
                    {
                       
                        return NotFound();
                    }

                    //update properties but keep the first ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvailable = product.StockAvailable;

                    //upload new image
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updated product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");

                }
            }
            return View(product);
        }

        [HttpPost]

        public async Task<IActionResult> Delete(string id) //Delete action
        {

            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
