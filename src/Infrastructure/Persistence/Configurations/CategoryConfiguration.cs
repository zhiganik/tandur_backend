using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // GetAllByRestaurantAsync: WHERE RestaurantId=X ORDER BY SortOrder
        builder.HasIndex(c => new { c.RestaurantId, c.SortOrder })
               .HasDatabaseName("ix_categories_restaurantid_sortorder");

        // GetVisibleByRestaurantAsync: WHERE RestaurantId=X AND IsVisible=true ORDER BY SortOrder
        builder.HasIndex(c => new { c.RestaurantId, c.IsVisible, c.SortOrder })
               .HasDatabaseName("ix_categories_restaurantid_isvisible_sortorder");
    }
}
