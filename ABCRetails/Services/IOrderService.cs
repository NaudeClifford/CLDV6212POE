using ABCRetails.Models;

namespace ABCRetails.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrdersAsync(); // Admin view
        Task<List<Order>> GetOrdersByCustomerAsync(string customerId); // Customer view
        Task<Order> GetByIdAsync(string orderId);
        Task AddOrderAsync(Order order);
        Task UpdateStatusAsync(string orderId, string status);
    }
}
