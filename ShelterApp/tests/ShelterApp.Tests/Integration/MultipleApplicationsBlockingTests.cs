using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Infrastructure.Persistence;
using Xunit;

namespace ShelterApp.Tests.Integration;

/// <summary>
/// Test 3: Multiple applications blocking integration test
/// - Submit application for animal
/// - Try to submit second application for same animal
/// - Expected 400 error
/// </summary>
[Collection("PostgreSql")]
public class MultipleApplicationsBlockingTests : IntegrationTestBase
{
    public MultipleApplicationsBlockingTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SubmitApplication_WhenUserAlreadyHasActiveApplicationForAnimal_ShouldReturn400()
    {
        // =====================================================
        // ARRANGE: Create test animal and register user
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Luna");

        var userTokens = await RegisterUserAsync(
            email: "duplicate.test@example.com",
            password: "Test123!",
            firstName: "Adam",
            lastName: "Testowy");

        SetAuthToken(userTokens.AccessToken);

        // =====================================================
        // ACT 1: Submit first application - should succeed
        // =====================================================
        var firstRequest = new
        {
            animalId,
            firstName = "Adam",
            lastName = "Testowy",
            email = "duplicate.test@example.com",
            phone = "+48123456789",
            dateOfBirth = "1990-05-15",
            rodoConsent = true,
            motivation = "Chcę adoptować to zwierzę"
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/adoptions", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        firstResult.Should().NotBeNull();
        firstResult!.ApplicationStatus.Should().Be("Submitted");

        // Verify animal is reserved
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);

        // =====================================================
        // ACT 2: Try to submit second application - should fail
        // =====================================================
        var secondRequest = new
        {
            animalId,
            firstName = "Adam",
            lastName = "Testowy",
            email = "duplicate.test@example.com",
            phone = "+48123456789",
            dateOfBirth = "1990-05-15",
            rodoConsent = true,
            motivation = "Druga próba adopcji tego samego zwierzęcia"
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/adoptions", secondRequest);

        // =====================================================
        // ASSERT: Second application should be rejected with 400
        // =====================================================
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await secondResponse.Content.ReadAsStringAsync();
        errorContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitApplication_WhenAnimalAlreadyReservedByAnotherUser_ShouldReturn400()
    {
        // =====================================================
        // ARRANGE: Create test animal
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Bella");

        // First user submits application
        var firstUserTokens = await RegisterUserAsync(
            email: "first.user@example.com",
            password: "Test123!",
            firstName: "Pierwszy",
            lastName: "Użytkownik");

        SetAuthToken(firstUserTokens.AccessToken);

        var firstRequest = new
        {
            animalId,
            firstName = "Pierwszy",
            lastName = "Użytkownik",
            email = "first.user@example.com",
            phone = "+48111111111",
            dateOfBirth = "1985-03-20",
            rodoConsent = true
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/adoptions", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify animal is reserved
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);

        // =====================================================
        // ACT: Second user tries to submit application
        // =====================================================
        var secondUserTokens = await RegisterUserAsync(
            email: "second.user@example.com",
            password: "Test123!",
            firstName: "Drugi",
            lastName: "Użytkownik");

        SetAuthToken(secondUserTokens.AccessToken);

        var secondRequest = new
        {
            animalId,
            firstName = "Drugi",
            lastName = "Użytkownik",
            email = "second.user@example.com",
            phone = "+48222222222",
            dateOfBirth = "1992-07-10",
            rodoConsent = true
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/adoptions", secondRequest);

        // =====================================================
        // ASSERT: Second application should be rejected
        // =====================================================
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitApplication_WhenPreviousApplicationWasCancelled_ShouldSucceed()
    {
        // =====================================================
        // ARRANGE: Create test animal and submit first application
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Rocky");

        var userTokens = await RegisterUserAsync(
            email: "cancelled.test@example.com",
            password: "Test123!",
            firstName: "Tomasz",
            lastName: "Anulowany");

        SetAuthToken(userTokens.AccessToken);

        // Submit first application
        var firstRequest = new
        {
            animalId,
            firstName = "Tomasz",
            lastName = "Anulowany",
            email = "cancelled.test@example.com",
            phone = "+48333333333",
            dateOfBirth = "1988-11-05",
            rodoConsent = true
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/adoptions", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        var applicationId = firstResult!.ApplicationId;

        // Cancel the first application
        var cancelRequest = new
        {
            reason = "Zmiana planów",
            userName = "Tomasz Anulowany"
        };

        var cancelResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/cancel",
            cancelRequest);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify animal is available again
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);

        // =====================================================
        // ACT: Submit new application after cancellation
        // =====================================================
        var secondRequest = new
        {
            animalId,
            firstName = "Tomasz",
            lastName = "Anulowany",
            email = "cancelled.test@example.com",
            phone = "+48333333333",
            dateOfBirth = "1988-11-05",
            rodoConsent = true,
            motivation = "Jednak chcę adoptować"
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/adoptions", secondRequest);

        // =====================================================
        // ASSERT: Second application should succeed
        // =====================================================
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResult = await secondResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        secondResult.Should().NotBeNull();
        secondResult!.ApplicationStatus.Should().Be("Submitted");

        // Animal should be reserved again
        animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);
    }

    [Fact]
    public async Task SubmitApplication_WhenPreviousApplicationWasRejected_ShouldSucceed()
    {
        // =====================================================
        // ARRANGE: Create test animal and submit first application
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Charlie");

        var userTokens = await RegisterUserAsync(
            email: "rejected.test@example.com",
            password: "Test123!",
            firstName: "Karol",
            lastName: "Odrzucony");

        SetAuthToken(userTokens.AccessToken);

        // Submit first application
        var firstRequest = new
        {
            animalId,
            firstName = "Karol",
            lastName = "Odrzucony",
            email = "rejected.test@example.com",
            phone = "+48444444444",
            dateOfBirth = "1995-02-14",
            rodoConsent = true
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/adoptions", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        var applicationId = firstResult!.ApplicationId;

        // Staff takes for review and rejects
        var staffTokens = await RegisterStaffUserAsync("staff.reject@example.com");
        SetAuthToken(staffTokens.AccessToken);

        await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/review",
            new { reviewerUserId = Guid.NewGuid(), reviewerName = "Staff" });

        var rejectRequest = new
        {
            reviewerName = "Staff",
            reason = "Nieodpowiednie warunki"
        };

        var rejectResponse = await Client.PutAsJsonAsync(
            $"/api/adoptions/{applicationId}/reject",
            rejectRequest);
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify animal is available again
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);

        // =====================================================
        // ACT: Same user submits new application after rejection
        // =====================================================
        SetAuthToken(userTokens.AccessToken);

        var secondRequest = new
        {
            animalId,
            firstName = "Karol",
            lastName = "Odrzucony",
            email = "rejected.test@example.com",
            phone = "+48444444444",
            dateOfBirth = "1995-02-14",
            rodoConsent = true,
            motivation = "Poprawiłem warunki mieszkaniowe"
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/adoptions", secondRequest);

        // =====================================================
        // ASSERT: Second application should succeed
        // =====================================================
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResult = await secondResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        secondResult.Should().NotBeNull();
        secondResult!.ApplicationStatus.Should().Be("Submitted");

        // Animal should be reserved again
        animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Reserved);
    }

    [Fact]
    public async Task SubmitApplication_ForDifferentAnimals_ShouldSucceed()
    {
        // =====================================================
        // ARRANGE: Create two test animals and register user
        // =====================================================
        var animalId1 = await CreateTestAnimalAsync("Puszek");
        var animalId2 = await CreateTestAnimalAsync("Mruczek2");

        var userTokens = await RegisterUserAsync(
            email: "multi.animal@example.com",
            password: "Test123!",
            firstName: "Anna",
            lastName: "Wielozwierzak");

        SetAuthToken(userTokens.AccessToken);

        // =====================================================
        // ACT: Submit applications for both animals
        // =====================================================
        var firstRequest = new
        {
            animalId = animalId1,
            firstName = "Anna",
            lastName = "Wielozwierzak",
            email = "multi.animal@example.com",
            phone = "+48555555555",
            dateOfBirth = "1991-09-30",
            rodoConsent = true
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/adoptions", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondRequest = new
        {
            animalId = animalId2,
            firstName = "Anna",
            lastName = "Wielozwierzak",
            email = "multi.animal@example.com",
            phone = "+48555555555",
            dateOfBirth = "1991-09-30",
            rodoConsent = true
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/adoptions", secondRequest);

        // =====================================================
        // ASSERT: Both applications should succeed
        // =====================================================
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResult = await secondResponse.Content.ReadFromJsonAsync<SubmitApplicationResultDto>(JsonOptions);
        secondResult.Should().NotBeNull();
        secondResult!.ApplicationStatus.Should().Be("Submitted");

        // Both animals should be reserved
        var animal1 = await GetAnimalAsync(animalId1);
        var animal2 = await GetAnimalAsync(animalId2);

        animal1!.Status.Should().Be(AnimalStatus.Reserved);
        animal2!.Status.Should().Be(AnimalStatus.Reserved);
    }
}
