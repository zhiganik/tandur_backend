using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RestaurantScheduleOverrideConfiguration : IEntityTypeConfiguration<RestaurantScheduleOverride>
{
    public void Configure(EntityTypeBuilder<RestaurantScheduleOverride> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Reason).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Date).IsRequired();

        builder.HasOne(o => o.Restaurant)
               .WithMany(r => r.Overrides)
               .HasForeignKey(o => o.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.CreatedByAdmin)
               .WithMany()
               .HasForeignKey(o => o.CreatedByAdminId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(o => o.TimeSlots, tsb => tsb.ToJson());

        builder.HasIndex(o => new { o.RestaurantId, o.Date });
    }
}
