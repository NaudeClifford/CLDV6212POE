using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetails.Models
{
    public class CartItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required]
        public string ProductId { get; set; } = string.Empty;
        
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal Total => Quantity * UnitPrice;
    }


    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal Total => Items.Sum(i => i.Total);

        public void AddItem(Product p, int quantity = 1)
        {
            var existing = Items.FirstOrDefault(x => x.ProductId == p.Id);
            if (existing != null)
                existing.Quantity += quantity;
            else
                Items.Add(new CartItem
                {
                    ProductId = p.Id,
                    Product = p,
                    Quantity = quantity,
                    UnitPrice = (decimal)p.Price
                });
        }

        public void RemoveItem(string productId)
        {
            var item = Items.FirstOrDefault(x => x.ProductId == productId);
            if (item != null) Items.Remove(item);
        }

        public void UpdateQuantity(string productId, int quantity)
        {
            var item = Items.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0) Items.Remove(item);
                else item.Quantity = quantity;
            }
        }

        public void Clear() => Items.Clear();
    }


}
