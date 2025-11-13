using ABCRetails.Data;
using ABCRetails.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.Include(o => o.Product).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerId)
        {
            return await _context.Orders
                .Include(o => o.Product)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<Order> GetByIdAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new Exception("Order not found");
        }

        public async Task AddOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}
