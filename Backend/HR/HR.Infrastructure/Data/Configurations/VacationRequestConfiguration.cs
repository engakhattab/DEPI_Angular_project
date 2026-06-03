using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class VacationRequestConfiguration : IEntityTypeConfiguration<VacationRequest>
{
    public void Configure(EntityTypeBuilder<VacationRequest> entity)
    {
        entity.Property(v => v.Reason)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(v => v.CreatedAt)
            .IsRequired();

        entity.Property(v => v.WorkingDayCount)
            .IsRequired();

        entity.HasIndex(v => new { v.EmployeeId, v.Status, v.StartDate, v.EndDate });

        entity.HasIndex(v => v.ReviewedByEmployeeId);

        entity.HasOne(v => v.Employee)
            .WithMany()
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(v => v.ReviewedBy)
            .WithMany()
            .HasForeignKey(v => v.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
