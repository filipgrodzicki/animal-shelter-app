namespace ShelterApp.Domain.Emails;

/// <summary>
/// Base model for all email templates
/// </summary>
public abstract class EmailTemplateModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ShelterName { get; set; } = "Schronisko dla Zwierząt";
    public string ShelterEmail { get; set; } = string.Empty;
    public string ShelterPhone { get; set; } = string.Empty;
    public string ShelterAddress { get; set; } = string.Empty;
    public string ShelterWebsite { get; set; } = string.Empty;
    public int CurrentYear { get; set; } = DateTime.UtcNow.Year;
}

/// <summary>
/// Model for adoption application confirmation email
/// </summary>
public class AdoptionApplicationConfirmationModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public string ApplicationNumber { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
}

/// <summary>
/// Model for application accepted email
/// </summary>
public class ApplicationAcceptedModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public string NextStepsUrl { get; set; } = string.Empty;
}

/// <summary>
/// Model for application rejected email
/// </summary>
public class ApplicationRejectedModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Model for visit scheduled email
/// </summary>
public class VisitScheduledModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitAddress { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Model for visit reminder email
/// </summary>
public class VisitReminderModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitAddress { get; set; } = string.Empty;
    public int HoursUntilVisit { get; set; }
}

/// <summary>
/// Model for visit result emails (approved/rejected)
/// </summary>
public class VisitResultModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? Reason { get; set; }
    public string? NextSteps { get; set; }
}

/// <summary>
/// Model for adoption completed email
/// </summary>
public class AdoptionCompletedModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime AdoptionDate { get; set; }
    public string? CareInstructions { get; set; }
}

/// <summary>
/// Model for application cancelled email
/// </summary>
public class ApplicationCancelledModel : EmailTemplateModel
{
    public string AnimalName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool CancelledByUser { get; set; }
}

/// <summary>
/// Model for volunteer application confirmation email
/// </summary>
public class VolunteerApplicationConfirmationModel : EmailTemplateModel
{
    public DateTime ApplicationDate { get; set; }
}

/// <summary>
/// Model for volunteer approval email
/// </summary>
public class VolunteerApprovalModel : EmailTemplateModel
{
    public DateTime TrainingStartDate { get; set; }
    public string? TrainingLocation { get; set; }
    public string? TrainingInstructions { get; set; }
}

/// <summary>
/// Model for volunteer activation email
/// </summary>
public class VolunteerActivationModel : EmailTemplateModel
{
    public string ContractNumber { get; set; } = string.Empty;
    public string? VolunteerPortalUrl { get; set; }
}

/// <summary>
/// Model for password reset email
/// </summary>
public class PasswordResetModel : EmailTemplateModel
{
    public string ResetUrl { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Model for welcome email
/// </summary>
public class WelcomeEmailModel : EmailTemplateModel
{
    public string LoginUrl { get; set; } = string.Empty;
}
