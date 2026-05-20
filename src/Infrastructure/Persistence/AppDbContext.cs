using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<Restaurant>                   Restaurants                   => Set<Restaurant>();
    public DbSet<Category>                     Categories                    => Set<Category>();
    public DbSet<MenuItem>                     MenuItems                     => Set<MenuItem>();
    public DbSet<RestaurantSchedule>           RestaurantSchedules           => Set<RestaurantSchedule>();
    public DbSet<RestaurantScheduleOverride>   RestaurantScheduleOverrides   => Set<RestaurantScheduleOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}