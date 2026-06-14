using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on foreign key to speed up joins and lookups
        builder.HasIndex(i => i.ProductId).HasDatabaseName("IX_Items_ProductId");
    }
}
