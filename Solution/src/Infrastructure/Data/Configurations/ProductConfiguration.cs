using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(p => p.CreatedOn)
            .IsRequired()
            .HasColumnType("datetime");

        builder.Property(p => p.ModifiedBy)
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(p => p.ModifiedOn)
            .HasColumnType("datetime");

        builder.HasMany(p => p.Items)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for frequently queried columns
        builder.HasIndex(p => p.ProductName).HasDatabaseName("IX_Products_ProductName");
    }
}
