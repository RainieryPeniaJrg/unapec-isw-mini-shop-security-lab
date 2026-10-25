using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniShop.Api.Models;

namespace MiniShop.Api.Data.Configurations;

public sealed class ProductCommentConfiguration : IEntityTypeConfiguration<ProductComment>
{
    public void Configure(EntityTypeBuilder<ProductComment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Body).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(comment => comment.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
