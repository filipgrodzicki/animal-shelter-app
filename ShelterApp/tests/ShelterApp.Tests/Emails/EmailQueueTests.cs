using FluentAssertions;
using ShelterApp.Domain.Emails;
using Xunit;

namespace ShelterApp.Tests.Emails;

public class EmailQueueTests
{
    private static EmailQueue CreateTestEmail(
        EmailType emailType = EmailType.WelcomeEmail,
        DateTime? scheduledAt = null)
    {
        return EmailQueue.Create(
            recipientEmail: "test@example.com",
            recipientName: "Jan Kowalski",
            subject: "Test Subject",
            htmlBody: "<html><body>Test</body></html>",
            emailType: emailType,
            textBody: "Test",
            scheduledAt: scheduledAt,
            metadata: "{\"key\": \"value\"}"
        );
    }

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Act
        var email = EmailQueue.Create(
            recipientEmail: "user@example.com",
            recipientName: "Anna Nowak",
            subject: "Witaj w schronisku",
            htmlBody: "<html><body>Witaj!</body></html>",
            emailType: EmailType.WelcomeEmail,
            textBody: "Witaj!",
            scheduledAt: DateTime.UtcNow.AddHours(1),
            metadata: "{\"source\": \"registration\"}"
        );

        // Assert
        email.RecipientEmail.Should().Be("user@example.com");
        email.RecipientName.Should().Be("Anna Nowak");
        email.Subject.Should().Be("Witaj w schronisku");
        email.HtmlBody.Should().Be("<html><body>Witaj!</body></html>");
        email.TextBody.Should().Be("Witaj!");
        email.EmailType.Should().Be(EmailType.WelcomeEmail);
        email.Status.Should().Be(EmailStatus.Pending);
        email.RetryCount.Should().Be(0);
        email.Metadata.Should().Be("{\"source\": \"registration\"}");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var email1 = CreateTestEmail();
        var email2 = CreateTestEmail();

        // Assert
        email1.Id.Should().NotBe(Guid.Empty);
        email2.Id.Should().NotBe(Guid.Empty);
        email1.Id.Should().NotBe(email2.Id);
    }

    [Fact]
    public void Create_ShouldSetCreatedAtToNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var email = CreateTestEmail();
        var after = DateTime.UtcNow;

        // Assert
        email.CreatedAt.Should().BeOnOrAfter(before);
        email.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Theory]
    [InlineData(EmailType.AdoptionApplicationConfirmation)]
    [InlineData(EmailType.ApplicationAccepted)]
    [InlineData(EmailType.ApplicationRejected)]
    [InlineData(EmailType.VisitScheduled)]
    [InlineData(EmailType.VisitReminder)]
    [InlineData(EmailType.VolunteerApplicationConfirmation)]
    [InlineData(EmailType.PasswordReset)]
    public void Create_WithDifferentEmailTypes_ShouldSucceed(EmailType emailType)
    {
        // Act
        var email = CreateTestEmail(emailType: emailType);

        // Assert
        email.EmailType.Should().Be(emailType);
    }

    #endregion

    #region MarkAsSent Tests

    [Fact]
    public void MarkAsSent_ShouldSetStatusToSent()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        email.MarkAsSent();

        // Assert
        email.Status.Should().Be(EmailStatus.Sent);
    }

    [Fact]
    public void MarkAsSent_ShouldSetSentAtToNow()
    {
        // Arrange
        var email = CreateTestEmail();
        var before = DateTime.UtcNow;

        // Act
        email.MarkAsSent();
        var after = DateTime.UtcNow;

        // Assert
        email.SentAt.Should().NotBeNull();
        email.SentAt.Should().BeOnOrAfter(before);
        email.SentAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsSent_ShouldSetLastAttemptAtToNow()
    {
        // Arrange
        var email = CreateTestEmail();
        var before = DateTime.UtcNow;

        // Act
        email.MarkAsSent();
        var after = DateTime.UtcNow;

        // Assert
        email.LastAttemptAt.Should().NotBeNull();
        email.LastAttemptAt.Should().BeOnOrAfter(before);
        email.LastAttemptAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsSent_ShouldClearLastError()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsFailed("Previous error");

        // Act
        email.MarkAsSent();

        // Assert
        email.LastError.Should().BeNull();
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_FirstAttempt_ShouldIncrementRetryCountAndStayPending()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        email.MarkAsFailed("Connection error");

        // Assert
        email.RetryCount.Should().Be(1);
        email.Status.Should().Be(EmailStatus.Pending);
        email.LastError.Should().Be("Connection error");
    }

    [Fact]
    public void MarkAsFailed_FifthAttempt_ShouldSetStatusToFailed()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");
        email.MarkAsFailed("Error 3");
        email.MarkAsFailed("Error 4");
        email.MarkAsFailed("Error 5");

        // Assert
        email.RetryCount.Should().Be(5);
        email.Status.Should().Be(EmailStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_BeforeFifthAttempt_ShouldStayPending()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");
        email.MarkAsFailed("Error 3");
        email.MarkAsFailed("Error 4");

        // Assert
        email.RetryCount.Should().Be(4);
        email.Status.Should().Be(EmailStatus.Pending);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetLastAttemptAtToNow()
    {
        // Arrange
        var email = CreateTestEmail();
        var before = DateTime.UtcNow;

        // Act
        email.MarkAsFailed("Error");
        var after = DateTime.UtcNow;

        // Assert
        email.LastAttemptAt.Should().NotBeNull();
        email.LastAttemptAt.Should().BeOnOrAfter(before);
        email.LastAttemptAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region MarkAsProcessing Tests

    [Fact]
    public void MarkAsProcessing_ShouldSetStatusToProcessing()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        email.MarkAsProcessing();

        // Assert
        email.Status.Should().Be(EmailStatus.Processing);
    }

    [Fact]
    public void MarkAsProcessing_ShouldSetLastAttemptAtToNow()
    {
        // Arrange
        var email = CreateTestEmail();
        var before = DateTime.UtcNow;

        // Act
        email.MarkAsProcessing();
        var after = DateTime.UtcNow;

        // Assert
        email.LastAttemptAt.Should().NotBeNull();
        email.LastAttemptAt.Should().BeOnOrAfter(before);
        email.LastAttemptAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region ShouldProcess Tests

    [Fact]
    public void ShouldProcess_PendingWithNoSchedule_ShouldReturnTrue()
    {
        // Arrange
        var email = CreateTestEmail(scheduledAt: null);

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcess_PendingWithPastSchedule_ShouldReturnTrue()
    {
        // Arrange
        var email = CreateTestEmail(scheduledAt: DateTime.UtcNow.AddHours(-1));

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcess_PendingWithFutureSchedule_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateTestEmail(scheduledAt: DateTime.UtcNow.AddHours(1));

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldProcess_NotPending_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsProcessing();

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldProcess_SentEmail_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsSent();

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldProcess_FailedEmail_ShouldReturnFalse()
    {
        // Arrange
        var email = CreateTestEmail();
        for (int i = 0; i < 5; i++)
        {
            email.MarkAsFailed("Error");
        }

        // Act
        var result = email.ShouldProcess();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetNextRetryDelay Tests

    [Fact]
    public void GetNextRetryDelay_RetryCount0_ShouldReturnZero()
    {
        // Arrange
        var email = CreateTestEmail();

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetNextRetryDelay_RetryCount1_ShouldReturn1Minute()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsFailed("Error");

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetNextRetryDelay_RetryCount2_ShouldReturn5Minutes()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetNextRetryDelay_RetryCount3_ShouldReturn15Minutes()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");
        email.MarkAsFailed("Error 3");

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetNextRetryDelay_RetryCount4_ShouldReturn30Minutes()
    {
        // Arrange
        var email = CreateTestEmail();
        email.MarkAsFailed("Error 1");
        email.MarkAsFailed("Error 2");
        email.MarkAsFailed("Error 3");
        email.MarkAsFailed("Error 4");

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void GetNextRetryDelay_RetryCount5OrMore_ShouldReturn1Hour()
    {
        // Arrange
        var email = CreateTestEmail();
        for (int i = 0; i < 5; i++)
        {
            email.MarkAsFailed($"Error {i + 1}");
        }

        // Act
        var delay = email.GetNextRetryDelay();

        // Assert
        delay.Should().Be(TimeSpan.FromHours(1));
    }

    #endregion
}
