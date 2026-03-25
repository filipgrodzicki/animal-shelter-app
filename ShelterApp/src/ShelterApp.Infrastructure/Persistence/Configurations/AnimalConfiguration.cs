using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animals");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Species)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Breed)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Gender)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Size)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Color)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ChipNumber)
            .HasMaxLength(50);

        builder.Property(a => a.AdmissionCircumstances)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Description)
            .HasMaxLength(4000);

        builder.Property(a => a.ExperienceLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.ChildrenCompatibility)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.AnimalCompatibility)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.SpaceRequirement)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.CareTime)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.SpecialNeeds)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(a => a.RegistrationNumber).IsUnique();
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Species);
        builder.HasIndex(a => a.ChipNumber).IsUnique().HasFilter("\"ChipNumber\" IS NOT NULL");
        builder.HasIndex(a => a.AdmissionDate);

        // Relationships
        builder.HasMany(a => a.Photos)
            .WithOne()
            .HasForeignKey(p => p.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StatusHistory)
            .WithOne()
            .HasForeignKey(s => s.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.MedicalRecords)
            .WithOne()
            .HasForeignKey(m => m.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation property backing fields
        builder.Navigation(a => a.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.MedicalRecords).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore state machine
        builder.Ignore("_stateMachine");
    }
}
