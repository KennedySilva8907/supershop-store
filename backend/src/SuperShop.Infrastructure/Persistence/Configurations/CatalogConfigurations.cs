using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperShop.Domain.Entities;

namespace SuperShop.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(60).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(60).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

public class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure(EntityTypeBuilder<Size> builder)
    {
        builder.Property(s => s.Label).HasMaxLength(6).IsRequired();
        builder.HasIndex(s => new { s.SizeSystem, s.Label }).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(140).IsRequired();
        builder.Property(p => p.Description).IsRequired();
        builder.Property(p => p.Price).HasPrecision(10, 2);
        builder.Property(p => p.CompareAtPrice).HasPrecision(10, 2);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.CollectionId);
        builder.HasIndex(p => p.IsActive);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Collection)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(p => p.HasStock);
        builder.Ignore(p => p.IsOnSale);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.Sku).HasMaxLength(24).IsRequired();

        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => new { v.ProductId, v.SizeId }).IsUnique();

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Size)
            .WithMany(s => s.Variants)
            .HasForeignKey(v => v.SizeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("ck_variant_stock_not_negative", "\"Stock\" >= 0"));

        builder.Ignore(v => v.IsInStock);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.Property(i => i.PublicId).HasMaxLength(120).IsRequired();
        builder.Property(i => i.AltText).HasMaxLength(150).IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
