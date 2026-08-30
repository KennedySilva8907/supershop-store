using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SuperShop.Infrastructure.Persistence;

public class OrderNumberCounter
{
    public int Year { get; set; }
    public int Next { get; set; }
}

public class OrderNumberCounterConfiguration : IEntityTypeConfiguration<OrderNumberCounter>
{
    public void Configure(EntityTypeBuilder<OrderNumberCounter> builder)
    {
        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).ValueGeneratedNever();
    }
}
