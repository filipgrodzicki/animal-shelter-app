using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class AnimalStatusChangeConfiguration : IEntityTypeConfiguration<AnimalStatusChange>
{
    public void Configure(EntityTypeBuilder<AnimalStatusChange> builder)
    {
        builder.ToTable("AnimalStatusChanges");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Trigger)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Reason)
            .HasMaxLength(1000);

        builder.Property(s => s.ChangedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(s => s.AnimalId);
        builder.HasIndex(s => s.ChangedAt);
    }
}
