using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Adoptions;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Adopter entity
/// </summary>
public class AdopterConfiguration : IEntityTypeConfiguration<Adopter>
{
    public void Configure(EntityTypeBuilder<Adopter> builder)
    {
        builder.ToTable("Adopters");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId);

        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Address)
            .HasMaxLength(300);

        builder.Property(a => a.City)
            .HasMaxLength(100);

        builder.Property(a => a.PostalCode)
            .HasMaxLength(10);

        builder.Property(a => a.DateOfBirth);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.RodoConsentDate);

        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(a => a.FullName);
        builder.Ignore(a => a.Age);
        builder.Ignore(a => a.DomainEvents);

        // Relationship with status history
        builder.HasMany(a => a.StatusHistory)
            .WithOne()
            .HasForeignKey(s => s.AdopterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(a => a.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
        builder.HasIndex(a => a.Email);
        builder.HasIndex(a => a.Status);
    }
}

/// <summary>
/// EF Core configuration for the AdopterStatusChange entity
/// </summary>
public class AdopterStatusChangeConfiguration : IEntityTypeConfiguration<AdopterStatusChange>
{
    public void Configure(EntityTypeBuilder<AdopterStatusChange> builder)
    {
        builder.ToTable("AdopterStatusChanges");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AdopterId);

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

        builder.HasIndex(s => s.AdopterId);
        builder.HasIndex(s => s.ChangedAt);
    }
}

/// <summary>
/// EF Core configuration for the AdoptionApplication entity
/// </summary>
public class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> builder)
    {
        builder.ToTable("AdoptionApplications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdopterId);
        builder.Property(a => a.AnimalId);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.ApplicationDate);

        // Review Information
        builder.Property(a => a.ReviewedByUserId);
        builder.Property(a => a.ReviewDate);
        builder.Property(a => a.ReviewNotes).HasMaxLength(2000);

        // Visit Information
        builder.Property(a => a.ScheduledVisitDate);
        builder.Property(a => a.VisitDate);
        builder.Property(a => a.VisitNotes).HasMaxLength(2000);
        builder.Property(a => a.VisitAssessment);
        builder.Property(a => a.VisitConductedByUserId);

        // Contract Information
        builder.Property(a => a.ContractGeneratedDate);
        builder.Property(a => a.ContractNumber).HasMaxLength(50);
        builder.Property(a => a.ContractSignedDate);
        builder.Property(a => a.ContractFilePath).HasMaxLength(500);

        // Additional Information
        builder.Property(a => a.AdoptionMotivation).HasMaxLength(2000);
        builder.Property(a => a.PetExperience).HasMaxLength(2000);
        builder.Property(a => a.LivingConditions).HasMaxLength(2000);
        builder.Property(a => a.OtherPetsInfo).HasMaxLength(1000);

        // Structured matching fields
        builder.Property(a => a.HousingType).HasMaxLength(50);
        builder.Property(a => a.HasChildren);
        builder.Property(a => a.HasOtherAnimals);
        builder.Property(a => a.ExperienceLevelApplicant).HasMaxLength(50);
        builder.Property(a => a.AvailableCareTime).HasMaxLength(50);

        builder.Property(a => a.RejectionReason).HasMaxLength(1000);
        builder.Property(a => a.CompletionDate);

        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);

        // Ignore properties
        builder.Ignore(a => a.DomainEvents);

        // Relationship with status history
        builder.HasMany(a => a.StatusHistory)
            .WithOne()
            .HasForeignKey(s => s.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(a => a.AdopterId);
        builder.HasIndex(a => a.AnimalId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ApplicationDate);
        builder.HasIndex(a => a.ContractNumber).IsUnique().HasFilter("\"ContractNumber\" IS NOT NULL");
    }
}

/// <summary>
/// EF Core configuration for the AdoptionApplicationStatusChange entity
/// </summary>
public class AdoptionApplicationStatusChangeConfiguration : IEntityTypeConfiguration<AdoptionApplicationStatusChange>
{
    public void Configure(EntityTypeBuilder<AdoptionApplicationStatusChange> builder)
    {
        builder.ToTable("AdoptionApplicationStatusChanges");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ApplicationId);

        builder.Property(s => s.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Trigger)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.ChangedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Reason)
            .HasMaxLength(500);

        builder.Property(s => s.ChangedAt);

        builder.HasIndex(s => s.ApplicationId);
        builder.HasIndex(s => s.ChangedAt);
    }
}
