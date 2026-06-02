using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> entity)
    {
        entity.Property(d => d.Name)
            .HasMaxLength(100)
            .IsRequired();

        entity.HasIndex(d => d.Name)
            .IsUnique();
    }
}
