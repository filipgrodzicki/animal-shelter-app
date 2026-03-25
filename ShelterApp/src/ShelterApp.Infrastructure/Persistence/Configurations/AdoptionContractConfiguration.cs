using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Adoptions;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the AdoptionContract entity
/// </summary>
public class AdoptionContractConfiguration : IEntityTypeConfiguration<AdoptionContract>
{
    public void Configure(EntityTypeBuilder<AdoptionContract> builder)
    {
        builder.ToTable("AdoptionContracts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AdoptionApplicationId)
            .IsRequired();

        builder.Property(c => c.ContractNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.GeneratedDate)
            .IsRequired();

        builder.Property(c => c.SignedDate);

        builder.Property(c => c.FilePath)
            .HasMaxLength(500);

        builder.Property(c => c.ShelterSignatory)
            .HasMaxLength(200);

        builder.Property(c => c.AdopterSignatory)
            .HasMaxLength(200);

        builder.Property(c => c.AdoptionFee)
            .HasPrecision(10, 2);

        builder.Property(c => c.FeePaid)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.FeePaidDate);

        builder.Property(c => c.AdditionalTerms)
            .HasMaxLength(4000);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.UpdatedAt);

        // Ignore computed property
        builder.Ignore(c => c.IsSigned);

        // Indexes
        builder.HasIndex(c => c.AdoptionApplicationId).IsUnique();
        builder.HasIndex(c => c.ContractNumber).IsUnique();
        builder.HasIndex(c => c.SignedDate);

        // Relationship with AdoptionApplication (1:1)
        builder.HasOne<AdoptionApplication>()
            .WithOne()
            .HasForeignKey<AdoptionContract>(c => c.AdoptionApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
