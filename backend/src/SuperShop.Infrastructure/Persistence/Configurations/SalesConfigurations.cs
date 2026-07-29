using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperShop.Domain.Entities;

namespace SuperShop.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.FullName).HasMaxLength(120).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(150);
        builder.Property(a => a.PostalCode).HasMaxLength(12).IsRequired();
        builder.Property(a => a.City).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(2).IsRequired().HasDefaultValue("PT");
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.UserId);
    }
}

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId).IsUnique();
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasIndex(i => new { i.CartId, i.ProductVariantId }).IsUnique();

        builder.HasOne(i => i.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductVariant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("ck_cart_item_quantity_positive", "\"Quantity\" >= 1"));
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderNumber).HasMaxLength(16).IsRequired();
        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.Subtotal).HasPrecision(10, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(10, 2);
        builder.Property(o => o.Total).HasPrecision(10, 2);

        builder.Property(o => o.ShippingFullName).HasMaxLength(120).IsRequired();
        builder.Property(o => o.ShippingLine1).HasMaxLength(150).IsRequired();
        builder.Property(o => o.ShippingLine2).HasMaxLength(150);
        builder.Property(o => o.ShippingPostalCode).HasMaxLength(12).IsRequired();
        builder.Property(o => o.ShippingCity).HasMaxLength(80).IsRequired();
        builder.Property(o => o.ShippingCountry).HasMaxLength(2).IsRequired();
        builder.Property(o => o.ShippingPhone).HasMaxLength(20).IsRequired();

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });

        builder.HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(i => i.ProductName).HasMaxLength(120).IsRequired();
        builder.Property(i => i.CollectionName).HasMaxLength(60).IsRequired();
        builder.Property(i => i.SizeLabel).HasMaxLength(6).IsRequired();
        builder.Property(i => i.Sku).HasMaxLength(24).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(10, 2);
        builder.Property(i => i.LineTotal).HasPrecision(10, 2);

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductVariant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("ck_order_item_quantity_positive", "\"Quantity\" >= 1"));
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.MbEntity).HasMaxLength(5);
        builder.Property(p => p.MbReference).HasMaxLength(9);
        builder.Property(p => p.MbWayPhone).HasMaxLength(20);
        builder.Property(p => p.CardLast4).HasMaxLength(4);

        builder.HasIndex(p => p.OrderId).IsUnique();
    }
}
