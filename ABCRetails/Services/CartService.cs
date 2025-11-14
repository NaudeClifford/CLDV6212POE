using ABCRetails.Data;
using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.EntityFrameworkCore;

public class CartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CartItem>> GetCartItemsAsync(string userId)
    {
        return await _context.Cart
            .Where(c => c.CustomerId == userId)
            .ToListAsync();
    }

    public async Task AddToCartAsync(string userId, Product product, int quantity)
    {
        var existingItem = await _context.Cart
            .FirstOrDefaultAsync(c => c.CustomerId == userId && c.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                CustomerId = userId,
                ProductId = product.Id,
                UnitPrice = product.Price,
                Quantity = quantity
            };
            await _context.Cart.AddAsync(cartItem);
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(string userId, string productId, int quantity)
    {
        var item = await _context.Cart
            .FirstOrDefaultAsync(c => c.CustomerId == userId && c.ProductId == productId);

        if (item != null)
        {
            if (quantity <= 0) _context.Cart.Remove(item);
            else item.Quantity = quantity;

            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveItemAsync(string userId, string productId)
    {
        var item = await _context.Cart
            .FirstOrDefaultAsync(c => c.CustomerId == userId && c.ProductId == productId);

        if (item != null)
        {
            _context.Cart.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
