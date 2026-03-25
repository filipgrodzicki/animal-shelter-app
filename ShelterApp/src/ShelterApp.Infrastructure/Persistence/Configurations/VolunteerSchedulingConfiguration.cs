using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ScheduleSlot entity
/// </summary>
public class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("ScheduleSlots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date);

        builder.Property(s => s.StartTime);

        builder.Property(s => s.EndTime);

        builder.Property(s => s.MaxVolunteers);

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.CreatedByUserId);

        builder.Property(s => s.IsActive);

        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(s => s.CurrentVolunteers);
        builder.Ignore(s => s.HasAvailableSpots);
        builder.Ignore(s => s.DurationHours);

        // Relationship with assignments
        builder.HasMany(s => s.Assignments)
            .WithOne()
            .HasForeignKey(a => a.ScheduleSlotId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.Date);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => new { s.Date, s.StartTime });
        builder.HasIndex(s => s.CreatedByUserId);
    }
}

/// <summary>
/// EF Core configuration for the VolunteerAssignment entity
/// </summary>
public class VolunteerAssignmentConfiguration : IEntityTypeConfiguration<VolunteerAssignment>
{
    public void Configure(EntityTypeBuilder<VolunteerAssignment> builder)
    {
        builder.ToTable("VolunteerAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ScheduleSlotId);

        builder.Property(a => a.VolunteerId);

        builder.Property(a => a.AssignedByUserId);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.AssignedAt);

        builder.Property(a => a.ConfirmedAt);

        builder.Property(a => a.CancelledAt);

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);

        // Relationship with volunteer
        builder.HasOne<Volunteer>()
            .WithMany()
            .HasForeignKey(a => a.VolunteerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.ScheduleSlotId);
        builder.HasIndex(a => a.VolunteerId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.ScheduleSlotId, a.VolunteerId, a.Status });
    }
}

/// <summary>
/// EF Core configuration for the Attendance entity (WB-18)
/// </summary>
public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.VolunteerId);

        builder.Property(a => a.ScheduleSlotId);

        builder.Property(a => a.CheckInTime);

        builder.Property(a => a.CheckOutTime);

        builder.Property(a => a.Notes)
            .HasMaxLength(2000);

        builder.Property(a => a.WorkDescription)
            .HasMaxLength(1000);

        builder.Property(a => a.ApprovedByUserId);

        builder.Property(a => a.ApprovedAt);

        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(a => a.HoursWorked);
        builder.Ignore(a => a.IsApproved);
        builder.Ignore(a => a.IsCheckedIn);

        // Relationship with volunteer
        builder.HasOne<Volunteer>()
            .WithMany()
            .HasForeignKey(a => a.VolunteerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with slot (optional)
        builder.HasOne<ScheduleSlot>()
            .WithMany()
            .HasForeignKey(a => a.ScheduleSlotId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(a => a.VolunteerId);
        builder.HasIndex(a => a.ScheduleSlotId);
        builder.HasIndex(a => a.CheckInTime);
        builder.HasIndex(a => a.ApprovedAt);
        builder.HasIndex(a => new { a.VolunteerId, a.CheckInTime });
    }
}
