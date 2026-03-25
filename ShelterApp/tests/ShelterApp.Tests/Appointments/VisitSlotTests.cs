using FluentAssertions;
using ShelterApp.Domain.Appointments;
using Xunit;

namespace ShelterApp.Tests.Appointments;

public class VisitSlotTests
{
    private static VisitSlot CreateFutureTestSlot(
        int daysFromNow = 7,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        int maxCapacity = 3,
        string? notes = null)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromNow));
        var start = startTime ?? new TimeOnly(10, 0);
        var end = endTime ?? new TimeOnly(11, 0);

        var result = VisitSlot.Create(date, start, end, maxCapacity, notes);
        return result.Value;
    }

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, endTime, 5, "Notatka");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Date.Should().Be(date);
        result.Value.StartTime.Should().Be(startTime);
        result.Value.EndTime.Should().Be(endTime);
        result.Value.MaxCapacity.Should().Be(5);
        result.Value.Notes.Should().Be("Notatka");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithPastDate_ShouldFail()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, endTime, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("przeszłości");
    }

    [Fact]
    public void Create_WithEndTimeBeforeStartTime_ShouldFail()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var startTime = new TimeOnly(11, 0);
        var endTime = new TimeOnly(10, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, endTime, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("później");
    }

    [Fact]
    public void Create_WithEndTimeEqualToStartTime_ShouldFail()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var startTime = new TimeOnly(10, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, startTime, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("później");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Create_WithInvalidCapacity_ShouldFail(int capacity)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, endTime, capacity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("1 a 10");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Create_WithValidCapacity_ShouldSucceed(int capacity)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        // Act
        var result = VisitSlot.Create(date, startTime, endTime, capacity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxCapacity.Should().Be(capacity);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var slot1 = CreateFutureTestSlot();
        var slot2 = CreateFutureTestSlot();

        // Assert
        slot1.Id.Should().NotBe(Guid.Empty);
        slot2.Id.Should().NotBe(Guid.Empty);
        slot1.Id.Should().NotBe(slot2.Id);
    }

    #endregion

    #region Book Tests

    [Fact]
    public void Book_AvailableSlot_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        var applicationId = Guid.NewGuid();
        var adopterId = Guid.NewGuid();

        // Act
        var result = slot.Book(applicationId, adopterId, "Jan Kowalski", "Burek");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(applicationId);
        result.Value.AdopterId.Should().Be(adopterId);
        result.Value.AdopterName.Should().Be("Jan Kowalski");
        result.Value.AnimalName.Should().Be("Burek");
        result.Value.Status.Should().Be(BookingStatus.Confirmed);
        slot.CurrentBookings.Should().Be(1);
        slot.RemainingCapacity.Should().Be(2);
    }

    [Fact]
    public void Book_FullSlot_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 1);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");

        // Act
        var result = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Anna Nowak", "Max");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Brak wolnych miejsc");
    }

    [Fact]
    public void Book_DeactivatedSlot_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        slot.Deactivate("admin", "Test");

        // Act
        var result = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("anulowany");
    }

    [Fact]
    public void Book_SameApplicationTwice_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        var applicationId = Guid.NewGuid();
        slot.Book(applicationId, Guid.NewGuid(), "Jan Kowalski", "Burek");

        // Act
        var result = slot.Book(applicationId, Guid.NewGuid(), "Jan Kowalski", "Burek");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("już rezerwację");
    }

    #endregion

    #region CancelBooking Tests

    [Fact]
    public void CancelBooking_ExistingBooking_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var bookingResult = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");
        var bookingId = bookingResult.Value.Id;

        // Act
        var result = slot.CancelBooking(bookingId, "admin", "Zmiana planów");

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.Bookings.First().Status.Should().Be(BookingStatus.Cancelled);
        slot.CurrentBookings.Should().Be(0);
    }

    [Fact]
    public void CancelBooking_NonExistingBooking_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.CancelBooking(Guid.NewGuid(), "admin", "Powód");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void CancelBooking_AlreadyCancelledBooking_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var bookingResult = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");
        var bookingId = bookingResult.Value.Id;
        slot.CancelBooking(bookingId, "admin", "Pierwsza anulacja");

        // Act
        var result = slot.CancelBooking(bookingId, "admin", "Druga anulacja");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("anulować rezerwacji");
    }

    #endregion

    #region ConfirmAttendance Tests

    [Fact]
    public void ConfirmAttendance_ConfirmedBooking_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var bookingResult = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");
        var bookingId = bookingResult.Value.Id;

        // Act
        var result = slot.ConfirmAttendance(bookingId, "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.Bookings.First().Status.Should().Be(BookingStatus.Attended);
    }

    [Fact]
    public void ConfirmAttendance_NonExistingBooking_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.ConfirmAttendance(Guid.NewGuid(), "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void ConfirmAttendance_CancelledBooking_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var bookingResult = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");
        var bookingId = bookingResult.Value.Id;
        slot.CancelBooking(bookingId, "admin", "Anulacja");

        // Act
        var result = slot.ConfirmAttendance(bookingId, "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region MarkNoShow Tests

    [Fact]
    public void MarkNoShow_ConfirmedBooking_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var bookingResult = slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Jan Kowalski", "Burek");
        var bookingId = bookingResult.Value.Id;

        // Act
        var result = slot.MarkNoShow(bookingId, "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.Bookings.First().Status.Should().Be(BookingStatus.NoShow);
    }

    [Fact]
    public void MarkNoShow_NonExistingBooking_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.MarkNoShow(Guid.NewGuid(), "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ValidParameters_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        var newStartTime = new TimeOnly(14, 0);
        var newEndTime = new TimeOnly(16, 0);

        // Act
        var result = slot.Update(newStartTime, newEndTime, 5, "Nowe notatki");

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.StartTime.Should().Be(newStartTime);
        slot.EndTime.Should().Be(newEndTime);
        slot.MaxCapacity.Should().Be(5);
        slot.Notes.Should().Be("Nowe notatki");
    }

    [Fact]
    public void Update_EndTimeBeforeStartTime_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.Update(new TimeOnly(14, 0), new TimeOnly(12, 0), null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("później");
    }

    [Fact]
    public void Update_CapacityBelowCurrentBookings_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 1", "Zwierze 1");
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 2", "Zwierze 2");

        // Act
        var result = slot.Update(null, null, 1, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("zmniejszyć pojemności");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Update_InvalidCapacity_ShouldFail(int capacity)
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.Update(null, null, capacity, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("1 a 10");
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_ActiveSlot_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.Deactivate("admin", "Zmiana planów");

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldCancelAllActiveBookings()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 1", "Zwierze 1");
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 2", "Zwierze 2");

        // Act
        slot.Deactivate("admin", "Zmiana planów");

        // Assert
        slot.Bookings.All(b => b.Status == BookingStatus.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void Deactivate_AlreadyInactiveSlot_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        slot.Deactivate("admin", "Pierwsza dezaktywacja");

        // Act
        var result = slot.Deactivate("admin", "Druga dezaktywacja");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("już nieaktywny");
    }

    #endregion

    #region Reactivate Tests

    [Fact]
    public void Reactivate_InactiveSlot_ShouldSucceed()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        slot.Deactivate("admin", "Test");

        // Act
        var result = slot.Reactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        slot.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_AlreadyActiveSlot_ShouldFail()
    {
        // Arrange
        var slot = CreateFutureTestSlot();

        // Act
        var result = slot.Reactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("już aktywny");
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void Duration_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(12, 30);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var slot = VisitSlot.Create(date, startTime, endTime, 3).Value;

        // Act & Assert
        slot.Duration.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Fact]
    public void RemainingCapacity_ShouldCalculateCorrectly()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 5);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 1", "Zwierze 1");
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 2", "Zwierze 2");

        // Act & Assert
        slot.RemainingCapacity.Should().Be(3);
    }

    [Fact]
    public void IsAvailable_FutureActiveSlotWithCapacity_ShouldBeTrue()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 3);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 1", "Zwierze 1");

        // Act & Assert
        slot.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_FullSlot_ShouldBeFalse()
    {
        // Arrange
        var slot = CreateFutureTestSlot(maxCapacity: 1);
        slot.Book(Guid.NewGuid(), Guid.NewGuid(), "Osoba 1", "Zwierze 1");

        // Act & Assert
        slot.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_InactiveSlot_ShouldBeFalse()
    {
        // Arrange
        var slot = CreateFutureTestSlot();
        slot.Deactivate("admin", "Test");

        // Act & Assert
        slot.IsAvailable.Should().BeFalse();
    }

    #endregion
}
