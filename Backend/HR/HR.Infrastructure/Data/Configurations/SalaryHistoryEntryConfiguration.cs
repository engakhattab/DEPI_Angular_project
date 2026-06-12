using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class SalaryHistoryEntryConfiguration : IEntityTypeConfiguration<SalaryHistoryEntry>
{
    public void Configure(EntityTypeBuilder<SalaryHistoryEntry> entity)
    {
        entity.Property(e => e.PreviousBaseSalary).HasPrecision(18, 2);
        entity.Property(e => e.NewBaseSalary).HasPrecision(18, 2);
        entity.Property(e => e.PreviousCurrency).HasMaxLength(8);
        entity.Property(e => e.NewCurrency).HasMaxLength(8);
        entity.HasIndex(e => new { e.EmployeeId, e.ChangedAt });
        entity.HasIndex(e => new { e.ChangedByEmployeeId, e.ChangedAt });

        entity.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ChangedBy)
            .WithMany()
            .HasForeignKey(e => e.ChangedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
