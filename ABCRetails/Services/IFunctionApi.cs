using ABCRetails.Models;

namespace ABCRetails.Services;

public interface IFunctionApi
{
    //Customers
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerAsync(string id);
    Task<Customer> CreateCustomerAsync(Customer c);
    Task<Customer> UpdateCustomerAsync(string id, Customer c);
    Task DeleteCustomerAsync(string id);

    //Product
    Task<List<Product>> GetProductsAsync();
    Task<Product?> GetProductAsync(string id);
    Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
    Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
    Task DeleteProductAsync(string id);

    //Orders
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderAsync(string id);
    Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
    Task UpdateOrderStatusAsync(string id, string newStatus);
    Task DeleteOrderAsync(string id);

    //Uploads
    Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);

    // Cart operations
    Task AddToCartAsync(string customerId, string productId, int quantity);
    Task<List<CartItem>> GetCartAsync(string customerId);
    Task RemoveFromCartAsync(string customerId, string productId);
    Task ClearCartAsync(string customerId);

}

