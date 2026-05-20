using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasIndex(u => u.CreatedAt)
               .HasDatabaseName("ix_users_createdat");

        builder.HasIndex(u => new { u.FirstName, u.CreatedAt })
               .HasDatabaseName("ix_users_firstname_createdat");

        builder.HasIndex(u => new { u.LastName, u.CreatedAt })
               .HasDatabaseName("ix_users_lastname_createdat");

        builder.HasIndex(u => u.PhoneNumber)
               .HasDatabaseName("ix_users_phonenumber");
    }
}
