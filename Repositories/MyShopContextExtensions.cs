// Partial implementation of OnModelCreatingPartial to configure the columns
// added in Entities/UserRole.cs without touching the auto-generated context file.
using Microsoft.EntityFrameworkCore;

namespace Repositories;

public partial class MyShopContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.User>(entity =>
        {
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");

            entity.Property(e => e.RefreshToken)
                .HasMaxLength(512);

            entity.Property(e => e.RefreshTokenExpiry)
                .HasColumnType("datetime");
        });
    }
}
