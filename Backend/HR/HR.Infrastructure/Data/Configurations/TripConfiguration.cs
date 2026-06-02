using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> entity)
    {
        entity.Property(t => t.ReferenceName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(t => t.Project)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(t => t.Route)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(t => t.TripType)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(t => t.TripCode)
            .HasMaxLength(32)
            .IsRequired();

        entity.HasIndex(t => t.TripCode)
            .IsUnique();

        entity.Property(t => t.RequestCode)
            .HasMaxLength(32)
            .IsRequired();

        entity.HasIndex(t => t.RequestCode)
            .IsUnique();

        entity.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
