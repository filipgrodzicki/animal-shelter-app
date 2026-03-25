using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Animals.Entities;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("MedicalRecords");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.Diagnosis)
            .HasMaxLength(2000);

        builder.Property(m => m.Treatment)
            .HasMaxLength(2000);

        builder.Property(m => m.Medications)
            .HasMaxLength(2000);

        builder.Property(m => m.VeterinarianName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Notes)
            .HasMaxLength(4000);

        builder.Property(m => m.Cost)
            .HasPrecision(10, 2);

        // Dane osoby wprowadzającej (WF-06)
        builder.Property(m => m.EnteredBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.EnteredByUserId);

        // Relacja z załącznikami
        builder.HasMany(m => m.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(m => m.AnimalId);
        builder.HasIndex(m => m.RecordDate);
        builder.HasIndex(m => m.Type);
    }
}
