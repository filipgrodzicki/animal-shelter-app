using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Animals.Entities;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class AnimalPhotoConfiguration : IEntityTypeConfiguration<AnimalPhoto>
{
    public void Configure(EntityTypeBuilder<AnimalPhoto> builder)
    {
        builder.ToTable("AnimalPhotos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(p => p.ContentType)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.HasIndex(p => p.AnimalId);
        builder.HasIndex(p => new { p.AnimalId, p.IsMain });
    }
}
