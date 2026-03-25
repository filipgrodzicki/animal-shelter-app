using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Cms;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("ContentPages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.MetaDescription)
            .HasMaxLength(300);

        builder.Property(x => x.MetaKeywords)
            .HasMaxLength(200);

        builder.Property(x => x.LastEditedBy)
            .HasMaxLength(100);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.IsPublished);
    }
}
