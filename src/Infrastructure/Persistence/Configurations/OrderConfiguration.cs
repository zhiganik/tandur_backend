using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Total).HasPrecision(18, 2);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.StripePaymentIntentId).HasMaxLength(100);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Restaurant)
               .WithMany()
               .HasForeignKey(o => o.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.UserId, o.CreatedAt })
               .HasDatabaseName("ix_orders_userid_createdat");

        builder.HasIndex(o => new { o.RestaurantId, o.Status, o.CreatedAt })
               .HasDatabaseName("ix_orders_restaurantid_status_createdat");

        builder.HasIndex(o => new { o.Status, o.CreatedAt })
               .HasDatabaseName("ix_orders_status_createdat");

        builder.HasIndex(o => o.StripePaymentIntentId)
               .HasDatabaseName("ix_orders_stripe_paymentintentid");
    }
}
