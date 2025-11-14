using ABCRetails.Models;
using ABCRetails.Data;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context) => _context = context;

        public async Task<List<CartItem>> GetCartItemsAsync(string customerId)
        {
            return await _context.Cart
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task AddToCartAsync(string customerId, Product product, int quantity)
        {
            var existing = await _context.Cart
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == product.Id);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _context.Cart.Add(new CartItem
                {
                    CustomerId = customerId,
                    ProductId = product.Id,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(string customerId, string productId, int quantity)
        {
            var item = await _context.Cart
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                    _context.Cart.Remove(item);
                else
                    item.Quantity = quantity;

                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveItemAsync(string customerId, string productId)
        {
            var item = await _context.Cart
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == productId);

            if (item != null)
            {
                _context.Cart.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string customerId)
        {
            var items = _context.Cart.Where(c => c.CustomerId == customerId);
            _context.Cart.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
