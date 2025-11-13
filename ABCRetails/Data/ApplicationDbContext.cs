using ABCRetails.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options) {}

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CartItem> Cart { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User <-> Admin relationship
            builder.Entity<Admin>()
               .HasOne(a => a.User)
               .WithOne(u => u.Admin)
               .HasForeignKey<Admin>(a => a.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

            // Customer <-> Order relationship
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(b => b.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order <-> Product relationship
            builder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Customer)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            builder.Entity<Order>()
                .Property(o => o.UnitPrice)
                .HasColumnType("decimal(10,2)");

            builder.Entity<CartItem>()
                .Property(ci => ci.UnitPrice)
                .HasColumnType("decimal(10,2)");
        }
    }
}
