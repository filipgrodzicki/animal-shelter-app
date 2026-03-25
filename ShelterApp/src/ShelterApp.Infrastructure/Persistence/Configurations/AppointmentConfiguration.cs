using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Appointments;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

public class VisitSlotConfiguration : IEntityTypeConfiguration<VisitSlot>
{
    public void Configure(EntityTypeBuilder<VisitSlot> builder)
    {
        builder.ToTable("VisitSlots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.MaxCapacity)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Ignore computed properties
        builder.Ignore(x => x.CurrentBookings);
        builder.Ignore(x => x.IsAvailable);
        builder.Ignore(x => x.IsPast);
        builder.Ignore(x => x.Duration);
        builder.Ignore(x => x.RemainingCapacity);

        // Relationship with bookings
        builder.HasMany(x => x.Bookings)
            .WithOne()
            .HasForeignKey(x => x.SlotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Bookings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Index for querying available slots
        builder.HasIndex(x => new { x.Date, x.IsActive });
        builder.HasIndex(x => x.Date);

        // Audit properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}

public class VisitBookingConfiguration : IEntityTypeConfiguration<VisitBooking>
{
    public void Configure(EntityTypeBuilder<VisitBooking> builder)
    {
        builder.ToTable("VisitBookings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SlotId)
            .IsRequired();

        builder.Property(x => x.ApplicationId)
            .IsRequired();

        builder.Property(x => x.AdopterId)
            .IsRequired();

        builder.Property(x => x.AdopterName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AnimalName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.BookedAt)
            .IsRequired();

        builder.Property(x => x.CancelledBy)
            .HasMaxLength(200);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Property(x => x.CancelledAt);

        builder.Property(x => x.AttendanceConfirmedBy)
            .HasMaxLength(200);

        builder.Property(x => x.AttendanceConfirmedAt);

        // Indexes
        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => x.AdopterId);
        builder.HasIndex(x => new { x.SlotId, x.Status });

        // Audit properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
