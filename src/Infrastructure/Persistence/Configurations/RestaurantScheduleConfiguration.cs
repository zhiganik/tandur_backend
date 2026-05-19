using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RestaurantScheduleConfiguration : IEntityTypeConfiguration<RestaurantSchedule>
{
    public void Configure(EntityTypeBuilder<RestaurantSchedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DayOfWeek).IsRequired();

        builder.HasOne(s => s.Restaurant)
               .WithMany(r => r.Schedules)
               .HasForeignKey(s => s.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(s => s.TimeSlots, tsb => tsb.ToJson());

        builder.HasIndex(s => new { s.RestaurantId, s.DayOfWeek }).IsUnique();
    }
}
