using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class EmployeeCompensationConfiguration : IEntityTypeConfiguration<EmployeeCompensation>
{
    public void Configure(EntityTypeBuilder<EmployeeCompensation> entity)
    {
        entity.Property(e => e.BaseSalary).HasPrecision(18, 2);
        entity.Property(e => e.SalaryCurrency).HasMaxLength(8).IsRequired();
        entity.HasIndex(e => e.EmployeeId).IsUnique();

        entity.HasOne(e => e.Employee)
            .WithOne()
            .HasForeignKey<EmployeeCompensation>(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
