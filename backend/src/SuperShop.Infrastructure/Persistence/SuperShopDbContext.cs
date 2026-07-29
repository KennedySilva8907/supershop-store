using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SuperShop.Domain.Entities;
using SuperShop.Infrastructure.Identity;

namespace SuperShop.Infrastructure.Persistence;

public class SuperShopDbContext(DbContextOptions<SuperShopDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SuperShopDbContext).Assembly);
    }
}
