using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the AnimalNote entity (WB-19)
/// </summary>
public class AnimalNoteConfiguration : IEntityTypeConfiguration<AnimalNote>
{
    public void Configure(EntityTypeBuilder<AnimalNote> builder)
    {
        builder.ToTable("AnimalNotes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.AnimalId);

        builder.Property(n => n.VolunteerId);

        builder.Property(n => n.UserId);

        builder.Property(n => n.AuthorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.NoteType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(n => n.IsImportant);

        builder.Property(n => n.ObservationDate);

        builder.Property(n => n.CreatedAt);
        builder.Property(n => n.UpdatedAt);

        // Relationship with animal
        builder.HasOne<Animal>()
            .WithMany()
            .HasForeignKey(n => n.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with volunteer (optional)
        builder.HasOne<Volunteer>()
            .WithMany()
            .HasForeignKey(n => n.VolunteerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(n => n.AnimalId);
        builder.HasIndex(n => n.VolunteerId);
        builder.HasIndex(n => n.NoteType);
        builder.HasIndex(n => n.IsImportant);
        builder.HasIndex(n => n.ObservationDate);
        builder.HasIndex(n => new { n.AnimalId, n.ObservationDate });
    }
}
