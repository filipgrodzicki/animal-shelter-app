using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShelterApp.Api.Features.Adoptions;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Infrastructure.Persistence;
using Xunit;

namespace ShelterApp.Tests.Integration;

/// <summary>
/// Test 1: Full online adoption flow integration test
/// - User registration
/// - Application submission
/// - Staff verification
/// - Visit reservation
/// - Adoption finalization
/// - Animal and application status verification
/// </summary>
[Collection("PostgreSql")]
public class AdoptionFlowIntegrationTests : IntegrationTestBase
{
    public AdoptionFlowIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task FullAdoptionFlow_FromRegistrationToCompletion_ShouldSucceed()
    {
        // =====================================================
        // ARRANGE: Create test animal
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Burek");

        // Verify animal is available
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);

        // =====================================================
        // STEP 1: User Registration
        // =====================================================
        var userTokens = await RegisterUserAsync(
            email: "adopter@test.com",
            password: "Adopter123!",
            firstName: "Jan",
            lastName: "Kowalski");

        userTokens.AccessToken.Should().NotBeNullOrEmpty();
        SetAuthToken(userTokens.AccessToken);

        // =====================================================
        // STEP 2: Submit Adoption Application
        // =====================================================
        var submitRequest = new
        {
            animalId,
            firstName = "Jan",
            lastName = "Kowalski",
            email = "adopter@test.com",
            phone = "+48123456789",
            address = "ul. Testowa 1",
            city = "Warszawa",
            postalCode = "00-001",
            dateOfBirth = "1990-05-15",
            rodoConsent = true,
            motivation = "Zawsze chciałem mieć psa. Mam duży ogród i dużo czasu.",
            livingConditions = "Dom z ogrodem 500m2, ogrodzony",
            experience = "Miałem psa przez 15 lat",
            otherPetsInfo = "Brak innych zwierząt"
        };

        var submitResponse = await Client.PostAsJsonAsync("/api/adoptions", submitRequest);

        // Debug: Log response details if failed
        if (submitResponse.StatusCode != HttpStatusCode.OK)
        {
            var content = await submitResponse.Content.ReadAsStringAsync();
            var authHeader = Client.DefaultRequestHeaders.Authorization?.ToString() ?? "NONE";
            throw new Exception($"Request failed with {submitResponse.StatusCode}. Auth header: {authHeader}. Response: {content}");
        }

        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        submitResult.Should().NotBeNull();
        submitResult!.ApplicationStatus.Should().Be("Submitted");
        submitResult.AnimalStatus.Should().Be("Reserved");

        var applicationId = submitResult.ApplicationId;
        var adopterId = submitResult.AdopterId;

        // Verify animal is now reserved
        animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);

        // =====================================================
        // STEP 3: Staff Takes Application for Review
        // =====================================================
        var staffTokens = await RegisterStaffUserAsync("staff@test.com");
        SetAuthToken(staffTokens.AccessToken);

        var reviewRequest = new TakeForReviewRequest(
            ReviewerUserId: Guid.NewGuid(),
            ReviewerName: "Anna Nowak"
        );

        var reviewResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/review",
            reviewRequest);
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewResult = await reviewResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        reviewResult!.Status.Should().Be("UnderReview");

        // =====================================================
        // STEP 4: Staff Approves Application
        // =====================================================
        var approveRequest = new ApproveRequest(
            ReviewerName: "Anna Nowak",
            Notes: "Zgłoszenie kompletne, warunki mieszkaniowe odpowiednie"
        );

        var approveResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/approve",
            approveRequest);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approveResult = await approveResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        approveResult!.Status.Should().Be("Accepted");

        // =====================================================
        // STEP 5: Schedule Visit
        // =====================================================
        var visitDate = DateTime.UtcNow.AddDays(7).Date.AddHours(10);

        var scheduleVisitRequest = new ScheduleVisitRequest(
            VisitDate: visitDate,
            ScheduledByName: "Anna Nowak",
            Notes: "Proszę zabrać dokument tożsamości"
        );

        var scheduleResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/schedule-visit",
            scheduleVisitRequest);
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var scheduleResult = await scheduleResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        scheduleResult!.Status.Should().Be("VisitScheduled");
        scheduleResult.ScheduledVisitDate.Should().NotBeNull();

        // =====================================================
        // STEP 6: Record Visit Attendance
        // =====================================================
        var attendanceRequest = new RecordAttendanceRequest(
            ConductedByUserId: Guid.NewGuid(),
            ConductedByName: "Anna Nowak"
        );

        var attendanceResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/record-attendance",
            attendanceRequest);
        attendanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // =====================================================
        // STEP 7: Record Positive Visit Result
        // =====================================================
        var visitResultRequest = new RecordVisitResultRequest(
            IsPositive: true,
            Assessment: 5,
            Notes: "Wizyta przebiegła bardzo pozytywnie. Adoptujący wykazał się wiedzą o potrzebach psa.",
            RecordedByName: "Anna Nowak"
        );

        var visitResultResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/record-visit",
            visitResultRequest);
        visitResultResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var visitResultResult = await visitResultResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        // Po pozytywnej ocenie wizyty status przechodzi bezpośrednio do PendingFinalization
        visitResultResult!.Status.Should().Be("PendingFinalization");
        visitResultResult.VisitAssessment.Should().Be(5);

        // =====================================================
        // STEP 8: Generate Contract
        // =====================================================
        var generateContractRequest = new GenerateContractRequest(
            GeneratedByName: "Anna Nowak"
        );

        var contractResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/generate-contract",
            generateContractRequest);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var contractResult = await contractResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        // Po wygenerowaniu umowy status pozostaje PendingFinalization
        contractResult!.Status.Should().Be("PendingFinalization");
        contractResult.ContractNumber.Should().NotBeNullOrEmpty();

        // =====================================================
        // STEP 9: Finalize Adoption
        // =====================================================
        var finalizeRequest = new FinalizeAdoptionRequest(
            ContractFilePath: $"/contracts/2024/umowa_{contractResult.ContractNumber}.pdf",
            SignedByName: "Anna Nowak"
        );

        var finalizeResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/complete",
            finalizeRequest);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalizeResult = await finalizeResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        finalizeResult!.Status.Should().Be("Completed");
        finalizeResult.CompletionDate.Should().NotBeNull();

        // =====================================================
        // FINAL VERIFICATION
        // =====================================================

        // Verify animal is now adopted
        animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Adopted);

        // Verify application via API
        var getApplicationResponse = await Client.GetAsync(
            $"/api/adoptions/{applicationId}?includeStatusHistory=true");
        getApplicationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify adopter status
        // Po zakończonej adopcji status wraca do Registered (może adoptować ponownie)
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var adopter = await dbContext.Adopters.FindAsync(adopterId);
        adopter!.Status.Should().Be(AdopterStatus.Registered);
    }

    [Fact]
    public async Task AdoptionFlow_RejectedApplication_ShouldReleaseAnimal()
    {
        // Arrange
        var animalId = await CreateTestAnimalAsync("Azor");
        var userTokens = await RegisterUserAsync("user2@test.com", "User123!", "Piotr", "Nowak");
        SetAuthToken(userTokens.AccessToken);

        // Submit application
        var submitRequest = new
        {
            animalId,
            firstName = "Piotr",
            lastName = "Nowak",
            email = "user2@test.com",
            phone = "+48987654321",
            dateOfBirth = "1985-03-20",
            rodoConsent = true
        };

        var submitResponse = await Client.PostAsJsonAsync("/api/adoptions", submitRequest);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        var applicationId = submitResult!.ApplicationId;

        // Animal should be reserved
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);

        // Staff takes for review and rejects
        var staffTokens = await RegisterStaffUserAsync("staff2@test.com");
        SetAuthToken(staffTokens.AccessToken);

        await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/review",
            new TakeForReviewRequest(Guid.NewGuid(), "Staff Member"));

        var rejectRequest = new RejectRequest(
            ReviewerName: "Staff Member",
            Reason: "Nieodpowiednie warunki mieszkaniowe"
        );

        var rejectResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/reject",
            rejectRequest);

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejectResult = await rejectResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        rejectResult!.Status.Should().Be("Rejected");
        rejectResult.RejectionReason.Should().Be("Nieodpowiednie warunki mieszkaniowe");

        // Animal should be available again
        animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);
    }

    [Fact]
    public async Task AdoptionFlow_CancelledByUser_ShouldReleaseAnimal()
    {
        // Arrange
        var animalId = await CreateTestAnimalAsync("Max");
        var userTokens = await RegisterUserAsync("user3@test.com", "User123!", "Maria", "Kowalska");
        SetAuthToken(userTokens.AccessToken);

        // Submit application
        var submitRequest = new
        {
            animalId,
            firstName = "Maria",
            lastName = "Kowalska",
            email = "user3@test.com",
            phone = "+48111222333",
            dateOfBirth = "1992-07-10",
            rodoConsent = true
        };

        var submitResponse = await Client.PostAsJsonAsync("/api/adoptions", submitRequest);
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        var applicationId = submitResult!.ApplicationId;

        // Cancel application
        var cancelRequest = new CancelApplicationRequest(
            Reason: "Zmiana planów życiowych",
            UserName: "Maria Kowalska"
        );

        var cancelResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/cancel",
            cancelRequest);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelResult = await cancelResponse.Content.ReadFromJsonAsync<AdoptionApplicationDto>(JsonOptions);
        cancelResult!.Status.Should().Be("Cancelled");

        // Animal should be available again
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);
    }
}
