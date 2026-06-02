using HR.Domain.Entities;
using HR.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> entity)
    {
        entity.Property(e => e.EmployeeNumber)
            .HasMaxLength(20)
            .IsRequired();

        entity.HasIndex(e => e.EmployeeNumber)
            .IsUnique();

        entity.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(e => e.JobTitle)
            .HasMaxLength(150);

        entity.Property(e => e.Email)
            .HasMaxLength(256);

        entity.Property(e => e.PhoneNumber)
            .HasMaxLength(30);

        entity.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
