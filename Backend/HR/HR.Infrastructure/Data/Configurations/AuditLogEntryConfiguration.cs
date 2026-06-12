using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> entity)
    {
        entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        entity.Property(e => e.ActionType).HasConversion<string>().HasMaxLength(64).IsRequired();
        entity.Property(e => e.ActorMarker).HasMaxLength(64);
        entity.Property(e => e.ChangedFields).IsRequired();
        entity.HasIndex(e => new { e.EntityType, e.EntityId, e.PerformedAt });
        entity.HasIndex(e => new { e.ActorEmployeeId, e.PerformedAt });
        entity.HasIndex(e => new { e.ActorMarker, e.PerformedAt });
        entity.HasIndex(e => new { e.ActionType, e.PerformedAt });

        entity.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
