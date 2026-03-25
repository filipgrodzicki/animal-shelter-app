using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the VolunteerCertificate entity
/// </summary>
public class VolunteerCertificateConfiguration : IEntityTypeConfiguration<VolunteerCertificate>
{
    public void Configure(EntityTypeBuilder<VolunteerCertificate> builder)
    {
        builder.ToTable("VolunteerCertificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.VolunteerId)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CertificateNumber)
            .HasMaxLength(100);

        builder.Property(c => c.IssueDate)
            .IsRequired();

        builder.Property(c => c.ExpiryDate);

        builder.Property(c => c.IssuingOrganization)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.FilePath)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.UpdatedAt);

        // Ignore computed property
        builder.Ignore(c => c.IsActive);

        // Indexes
        builder.HasIndex(c => c.VolunteerId);
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.ExpiryDate);
        builder.HasIndex(c => c.CertificateNumber);
    }
}
