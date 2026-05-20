using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Address).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Currency).IsRequired().HasMaxLength(3);
        builder.Property(r => r.TimeZone).IsRequired().HasMaxLength(100);

        builder.HasMany(r => r.AssignedAdmins)
               .WithMany(u => u.AssignedRestaurants)
               .UsingEntity(j => j.ToTable("AdminRestaurantAssignments"));

        builder.HasIndex(r => new { r.IsActive, r.CreatedAt })
               .HasDatabaseName("ix_restaurants_isactive_createdat");

        builder.HasIndex(r => r.Name)
               .HasDatabaseName("ix_restaurants_name");
    }
}
