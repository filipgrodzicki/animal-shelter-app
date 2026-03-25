using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Volunteer entity
/// </summary>
public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> builder)
    {
        builder.ToTable("Volunteers");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.UserId);

        builder.Property(v => v.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.DateOfBirth);

        builder.Property(v => v.Address)
            .HasMaxLength(300);

        builder.Property(v => v.City)
            .HasMaxLength(100);

        builder.Property(v => v.PostalCode)
            .HasMaxLength(10);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.ApplicationDate);

        builder.Property(v => v.TrainingStartDate);

        builder.Property(v => v.TrainingEndDate);

        builder.Property(v => v.ContractSignedDate);

        builder.Property(v => v.ContractNumber)
            .HasMaxLength(50);

        builder.Property(v => v.EmergencyContactName)
            .HasMaxLength(200);

        builder.Property(v => v.EmergencyContactPhone)
            .HasMaxLength(20);

        // Skills - stored as JSON
        builder.Property(v => v.Skills)
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnName("Skills")
            .HasMaxLength(2000);

        // Availability - stored as JSON
        builder.Property(v => v.Availability)
            .HasConversion(
                v => string.Join(",", v.Select(d => (int)d)),
                v => v.Split(",", StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => (DayOfWeek)int.Parse(s)).ToList())
            .HasColumnName("Availability")
            .HasMaxLength(100);

        builder.Property(v => v.TotalHoursWorked)
            .HasPrecision(10, 2);

        builder.Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Property(v => v.CreatedAt);
        builder.Property(v => v.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(v => v.FullName);
        builder.Ignore(v => v.Age);
        builder.Ignore(v => v.DomainEvents);

        // Relationship with status history
        builder.HasMany(v => v.StatusHistory)
            .WithOne()
            .HasForeignKey(s => s.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with certificates
        builder.HasMany(v => v.Certificates)
            .WithOne()
            .HasForeignKey(c => c.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation property backing fields
        builder.Navigation(v => v.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(v => v.Certificates).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(v => v.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
        builder.HasIndex(v => v.Email).IsUnique();
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.ApplicationDate);
        builder.HasIndex(v => v.ContractNumber).IsUnique().HasFilter("\"ContractNumber\" IS NOT NULL");
    }
}

/// <summary>
/// EF Core configuration for the VolunteerStatusChange entity
/// </summary>
public class VolunteerStatusChangeConfiguration : IEntityTypeConfiguration<VolunteerStatusChange>
{
    public void Configure(EntityTypeBuilder<VolunteerStatusChange> builder)
    {
        builder.ToTable("VolunteerStatusChanges");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.VolunteerId);

        builder.Property(s => s.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Trigger)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.ChangedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Reason)
            .HasMaxLength(500);

        builder.Property(s => s.ChangedAt);

        builder.HasIndex(s => s.VolunteerId);
        builder.HasIndex(s => s.ChangedAt);
    }
}
