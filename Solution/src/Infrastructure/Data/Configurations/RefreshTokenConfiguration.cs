using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(r => r.Username)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime");

        builder.Property(r => r.Revoked)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime");
    }
}
