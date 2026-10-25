using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniShop.Api.Models;

namespace MiniShop.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Username).IsRequired();
        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.Role).HasMaxLength(20).IsRequired();
        builder.HasIndex(user => user.Username).IsUnique();
    }
}
