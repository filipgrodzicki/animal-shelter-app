using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Notifications;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class AdminNotificationConfiguration : IEntityTypeConfiguration<AdminNotification>
{
    public void Configure(EntityTypeBuilder<AdminNotification> builder)
    {
        builder.ToTable("AdminNotifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Link)
            .HasMaxLength(500);

        builder.Property(x => x.RelatedEntityType)
            .HasMaxLength(100);

        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.IsDismissed);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.IsRead, x.IsDismissed, x.CreatedAt });
    }
}
