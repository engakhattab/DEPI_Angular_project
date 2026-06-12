using HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.Infrastructure.Data.Configurations;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> entity)
    {
        entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
        entity.Property(e => e.StoredFileName).HasMaxLength(255).IsRequired();
        entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        entity.Property(e => e.FileExtension).HasMaxLength(16).IsRequired();
        entity.Property(e => e.StorageRelativePath).HasMaxLength(500).IsRequired();
        entity.HasIndex(e => new { e.EmployeeId, e.RemovedAt, e.UploadedAt });
        entity.HasIndex(e => e.StoredFileName).IsUnique();

        entity.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RemovedBy)
            .WithMany()
            .HasForeignKey(e => e.RemovedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
