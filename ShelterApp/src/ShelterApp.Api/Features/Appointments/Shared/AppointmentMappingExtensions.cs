using ShelterApp.Domain.Appointments;

namespace ShelterApp.Api.Features.Appointments.Shared;

public static class AppointmentMappingExtensions
{
    public static VisitSlotDto ToDto(this VisitSlot slot, bool includeBookings = false)
    {
        return new VisitSlotDto(
            Id: slot.Id,
            Date: slot.Date,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            MaxCapacity: slot.MaxCapacity,
            CurrentBookings: slot.CurrentBookings,
            RemainingCapacity: slot.RemainingCapacity,
            IsAvailable: slot.IsAvailable,
            Notes: slot.Notes,
            Bookings: includeBookings
                ? slot.Bookings.Select(b => b.ToDto())
                : null
        );
    }

    public static VisitBookingDto ToDto(this VisitBooking booking)
    {
        return new VisitBookingDto(
            Id: booking.Id,
            SlotId: booking.SlotId,
            ApplicationId: booking.ApplicationId,
            AdopterId: booking.AdopterId,
            AdopterName: booking.AdopterName,
            AnimalName: booking.AnimalName,
            Status: booking.Status.ToString(),
            BookedAt: booking.BookedAt,
            CancelledBy: booking.CancelledBy,
            CancellationReason: booking.CancellationReason,
            CancelledAt: booking.CancelledAt,
            AttendanceConfirmedBy: booking.AttendanceConfirmedBy,
            AttendanceConfirmedAt: booking.AttendanceConfirmedAt
        );
    }
}
