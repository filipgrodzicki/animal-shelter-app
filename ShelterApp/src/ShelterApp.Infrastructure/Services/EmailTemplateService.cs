using Fluid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Emails;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Service for rendering email templates using Fluid template engine
/// </summary>
public interface IEmailTemplateService
{
    Task<(string Subject, string HtmlBody, string? TextBody)> RenderTemplateAsync<T>(
        EmailType emailType,
        T model,
        CancellationToken cancellationToken = default) where T : EmailTemplateModel;
}

public class EmailTemplateService : IEmailTemplateService
{
    private readonly FluidParser _parser;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly ShelterOptions _shelterOptions;
    private readonly Dictionary<EmailType, EmailTemplateDefinition> _templates;

    public EmailTemplateService(
        ILogger<EmailTemplateService> logger,
        IOptions<ShelterOptions> shelterOptions)
    {
        _logger = logger;
        _shelterOptions = shelterOptions.Value;
        _parser = new FluidParser();
        _templates = InitializeTemplates();
    }

    public async Task<(string Subject, string HtmlBody, string? TextBody)> RenderTemplateAsync<T>(
        EmailType emailType,
        T model,
        CancellationToken cancellationToken = default) where T : EmailTemplateModel
    {
        if (!_templates.TryGetValue(emailType, out var template))
        {
            throw new ArgumentException($"No template found for email type: {emailType}");
        }

        // Enrich model with shelter info
        EnrichModel(model);

        var context = new TemplateContext(model);
        context.Options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();

        var subject = await RenderAsync(template.SubjectTemplate, context);
        var htmlBody = await RenderAsync(template.HtmlTemplate, context);
        var textBody = template.TextTemplate != null
            ? await RenderAsync(template.TextTemplate, context)
            : null;

        return (subject, htmlBody, textBody);
    }

    private void EnrichModel(EmailTemplateModel model)
    {
        model.ShelterName = _shelterOptions.Name;
        model.ShelterEmail = _shelterOptions.Email;
        model.ShelterPhone = _shelterOptions.Phone;
        model.ShelterAddress = _shelterOptions.Address;
        model.ShelterWebsite = _shelterOptions.Website;
        model.CurrentYear = DateTime.UtcNow.Year;
    }

    private async Task<string> RenderAsync(string templateSource, TemplateContext context)
    {
        if (_parser.TryParse(templateSource, out var template, out var error))
        {
            return await template.RenderAsync(context);
        }

        _logger.LogError("Failed to parse template: {Error}", error);
        throw new InvalidOperationException($"Failed to parse template: {error}");
    }

    private Dictionary<EmailType, EmailTemplateDefinition> InitializeTemplates()
    {
        return new Dictionary<EmailType, EmailTemplateDefinition>
        {
            [EmailType.AdoptionApplicationConfirmation] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Potwierdzenie zgłoszenia adopcyjnego - {{ AnimalName }}",
                HtmlTemplate = GetAdoptionApplicationConfirmationTemplate(),
                TextTemplate = null
            },
            [EmailType.ApplicationAccepted] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Twoje zgłoszenie adopcyjne zostało zaakceptowane!",
                HtmlTemplate = GetApplicationAcceptedTemplate(),
                TextTemplate = null
            },
            [EmailType.ApplicationRejected] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Informacja o zgłoszeniu adopcyjnym",
                HtmlTemplate = GetApplicationRejectedTemplate(),
                TextTemplate = null
            },
            [EmailType.VisitScheduled] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Potwierdzenie wizyty - {{ AnimalName }}",
                HtmlTemplate = GetVisitScheduledTemplate(),
                TextTemplate = null
            },
            [EmailType.VisitReminder] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Przypomnienie o wizycie - jutro o {{ VisitDate | date: '%H:%M' }}",
                HtmlTemplate = GetVisitReminderTemplate(),
                TextTemplate = null
            },
            [EmailType.VisitApproved] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Wizyta zakończona pomyślnie - {{ AnimalName }}",
                HtmlTemplate = GetVisitApprovedTemplate(),
                TextTemplate = null
            },
            [EmailType.VisitRejected] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Informacja o wyniku wizyty",
                HtmlTemplate = GetVisitRejectedTemplate(),
                TextTemplate = null
            },
            [EmailType.AdoptionCompleted] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Gratulacje! Adopcja {{ AnimalName }} zakończona",
                HtmlTemplate = GetAdoptionCompletedTemplate(),
                TextTemplate = null
            },
            [EmailType.ApplicationCancelled] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Anulowanie zgłoszenia adopcyjnego",
                HtmlTemplate = GetApplicationCancelledTemplate(),
                TextTemplate = null
            },
            [EmailType.VolunteerApplicationConfirmation] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Potwierdzenie zgłoszenia wolontariatu",
                HtmlTemplate = GetVolunteerApplicationConfirmationTemplate(),
                TextTemplate = null
            },
            [EmailType.VolunteerApproval] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Twoje zgłoszenie wolontariatu zostało zaakceptowane!",
                HtmlTemplate = GetVolunteerApprovalTemplate(),
                TextTemplate = null
            },
            [EmailType.VolunteerActivation] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Witaj w zespole wolontariuszy!",
                HtmlTemplate = GetVolunteerActivationTemplate(),
                TextTemplate = null
            },
            [EmailType.PasswordReset] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Resetowanie hasła - {{ ShelterName }}",
                HtmlTemplate = GetPasswordResetTemplate(),
                TextTemplate = null
            },
            [EmailType.WelcomeEmail] = new EmailTemplateDefinition
            {
                SubjectTemplate = "Witaj w {{ ShelterName }}!",
                HtmlTemplate = GetWelcomeEmailTemplate(),
                TextTemplate = null
            }
        };
    }

    #region Email Templates

    private string GetBaseTemplate(string content)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{{{{ ShelterName }}}}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
        .header {{ background-color: #2563eb; color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .button:hover {{ background-color: #1d4ed8; }}
        .info-box {{ background-color: #f0f9ff; border-left: 4px solid #2563eb; padding: 15px; margin: 20px 0; }}
        .warning-box {{ background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; }}
        .success-box {{ background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0; }}
        h2 {{ color: #1f2937; margin-top: 0; }}
        p {{ margin: 10px 0; }}
        .detail {{ margin: 5px 0; }}
        .detail strong {{ display: inline-block; min-width: 120px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{{{{ ShelterName }}}}</h1>
        </div>
        <div class=""content"">
            {content}
        </div>
        <div class=""footer"">
            <p>{{{{ ShelterName }}}}</p>
            <p>{{{{ ShelterAddress }}}}</p>
            <p>Tel: {{{{ ShelterPhone }}}} | Email: {{{{ ShelterEmail }}}}</p>
            <p>&copy; {{{{ CurrentYear }}}} {{{{ ShelterName }}}}. Wszelkie prawa zastrzeżone.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetAdoptionApplicationConfirmationTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Dziękujemy za złożenie zgłoszenia adopcyjnego!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>
            <p>Otrzymaliśmy Twoje zgłoszenie adopcyjne dotyczące <strong>{{ AnimalName }}</strong>.</p>

            <div class=""info-box"">
                <p class=""detail""><strong>Numer zgłoszenia:</strong> {{ ApplicationNumber }}</p>
                <p class=""detail""><strong>Data zgłoszenia:</strong> {{ ApplicationDate | date: '%d.%m.%Y %H:%M' }}</p>
                <p class=""detail""><strong>Zwierzę:</strong> {{ AnimalName }}</p>
            </div>

            <h3>Co dalej?</h3>
            <ol>
                <li>Nasz zespół przeanalizuje Twoje zgłoszenie</li>
                <li>Skontaktujemy się z Tobą w ciągu 3-5 dni roboczych</li>
                <li>Jeśli zgłoszenie zostanie zaakceptowane, umówimy wizytę zapoznawczą</li>
            </ol>

            <p>Jeśli masz pytania, skontaktuj się z nami pod adresem {{ ShelterEmail }} lub telefonicznie {{ ShelterPhone }}.</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetApplicationAcceptedTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Świetne wiadomości!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <div class=""success-box"">
                <p>Twoje zgłoszenie adopcyjne dotyczące <strong>{{ AnimalName }}</strong> zostało <strong>zaakceptowane</strong>!</p>
            </div>

            <h3>Następne kroki</h3>
            <p>Teraz możesz umówić się na wizytę zapoznawczą z {{ AnimalName }}. Podczas wizyty będziesz mógł/mogła:</p>
            <ul>
                <li>Poznać {{ AnimalName }} osobiście</li>
                <li>Porozmawiać z opiekunami o charakterze i potrzebach zwierzęcia</li>
                <li>Zadać wszystkie nurtujące Cię pytania</li>
            </ul>

            <p style=""text-align: center;"">
                <a href=""{{ NextStepsUrl }}"" class=""button"">Umów wizytę</a>
            </p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetApplicationRejectedTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Informacja o zgłoszeniu adopcyjnym</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Dziękujemy za zainteresowanie adopcją <strong>{{ AnimalName }}</strong> z naszego schroniska.</p>

            <p>Po dokładnej analizie Twojego zgłoszenia, niestety nie możemy kontynuować procesu adopcyjnego.</p>

            {% if Reason != '' %}
            <div class=""info-box"">
                <p><strong>Uzasadnienie:</strong></p>
                <p>{{ Reason }}</p>
            </div>
            {% endif %}

            <p>Zachęcamy do odwiedzenia naszego schroniska i poznania innych zwierząt, które czekają na nowy dom.</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVisitScheduledTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Potwierdzenie wizyty</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Twoja wizyta zapoznawcza z <strong>{{ AnimalName }}</strong> została potwierdzona!</p>

            <div class=""info-box"">
                <p class=""detail""><strong>Data i godzina:</strong> {{ VisitDate | date: '%d.%m.%Y' }} o godz. {{ VisitDate | date: '%H:%M' }}</p>
                <p class=""detail""><strong>Miejsce:</strong> {{ VisitAddress }}</p>
                <p class=""detail""><strong>Zwierzę:</strong> {{ AnimalName }}</p>
            </div>

            <h3>Przygotowanie do wizyty</h3>
            <ul>
                <li>Przynieś dokument tożsamości</li>
                <li>Ubierz wygodne ubranie (zwierzęta mogą być aktywne!)</li>
                <li>Przygotuj pytania, które chcesz zadać opiekunom</li>
            </ul>

            {% if AdditionalInfo != '' %}
            <div class=""warning-box"">
                <p><strong>Dodatkowe informacje:</strong></p>
                <p>{{ AdditionalInfo }}</p>
            </div>
            {% endif %}

            <p>W razie potrzeby zmiany terminu, prosimy o kontakt minimum 24 godziny przed planowaną wizytą.</p>

            <p>Do zobaczenia!<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVisitReminderTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Przypomnienie o jutrzejszej wizycie</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Przypominamy o zaplanowanej wizycie zapoznawczej z <strong>{{ AnimalName }}</strong>.</p>

            <div class=""warning-box"">
                <p class=""detail""><strong>Kiedy:</strong> Jutro, {{ VisitDate | date: '%d.%m.%Y' }} o godz. {{ VisitDate | date: '%H:%M' }}</p>
                <p class=""detail""><strong>Gdzie:</strong> {{ VisitAddress }}</p>
            </div>

            <p>{{ AnimalName }} już nie może się doczekać spotkania z Tobą!</p>

            <p>Jeśli musisz odwołać lub przełożyć wizytę, prosimy o pilny kontakt.</p>

            <p>Do zobaczenia!<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVisitApprovedTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Wizyta zakończona pomyślnie!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <div class=""success-box"">
                <p>Twoja wizyta z <strong>{{ AnimalName }}</strong> przebiegła pomyślnie!</p>
            </div>

            <p>Cieszymy się, że spotkanie z {{ AnimalName }} było udane. Jesteśmy przekonani, że będziecie świetnym zespołem!</p>

            {% if NextSteps != '' %}
            <h3>Następne kroki</h3>
            <p>{{ NextSteps }}</p>
            {% endif %}

            <p>Wkrótce skontaktujemy się w sprawie finalizacji adopcji.</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVisitRejectedTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Informacja o wyniku wizyty</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Dziękujemy za wizytę w naszym schronisku i zainteresowanie adopcją <strong>{{ AnimalName }}</strong>.</p>

            <p>Po analizie przebiegu wizyty, niestety nie możemy kontynuować procesu adopcyjnego.</p>

            {% if Reason != '' %}
            <div class=""info-box"">
                <p><strong>Uzasadnienie:</strong></p>
                <p>{{ Reason }}</p>
            </div>
            {% endif %}

            <p>Zachęcamy do poznania innych zwierząt z naszego schroniska.</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetAdoptionCompletedTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Gratulacje! Adopcja zakończona!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <div class=""success-box"">
                <p style=""font-size: 18px;"">Oficjalnie jesteś teraz opiekunem/opiekunką <strong>{{ AnimalName }}</strong>!</p>
            </div>

            <div class=""info-box"">
                <p class=""detail""><strong>Numer umowy:</strong> {{ ContractNumber }}</p>
                <p class=""detail""><strong>Data adopcji:</strong> {{ AdoptionDate | date: '%d.%m.%Y' }}</p>
            </div>

            <p>Życzymy Wam wielu wspaniałych chwil razem!</p>

            {% if CareInstructions != '' %}
            <h3>Wskazówki dotyczące opieki</h3>
            <p>{{ CareInstructions }}</p>
            {% endif %}

            <h3>Pamiętaj</h3>
            <ul>
                <li>Regularnie odwiedzaj weterynarza</li>
                <li>Zapewnij odpowiednią dietę i aktywność fizyczną</li>
                <li>Daj {{ AnimalName }} czas na adaptację w nowym domu</li>
            </ul>

            <p>W razie pytań jesteśmy do Twojej dyspozycji!</p>

            <p>Z serdecznymi pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetApplicationCancelledTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Anulowanie zgłoszenia adopcyjnego</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Informujemy, że zgłoszenie adopcyjne dotyczące <strong>{{ AnimalName }}</strong> zostało anulowane.</p>

            {% if Reason != '' %}
            <div class=""info-box"">
                <p><strong>Powód:</strong></p>
                <p>{{ Reason }}</p>
            </div>
            {% endif %}

            <p>Jeśli jesteś nadal zainteresowany/a adopcją, zapraszamy do ponownego zgłoszenia lub kontaktu z nami.</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVolunteerApplicationConfirmationTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Dziękujemy za zgłoszenie wolontariatu!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Otrzymaliśmy Twoje zgłoszenie do programu wolontariatu w naszym schronisku.</p>

            <div class=""info-box"">
                <p class=""detail""><strong>Data zgłoszenia:</strong> {{ ApplicationDate | date: '%d.%m.%Y %H:%M' }}</p>
            </div>

            <h3>Co dalej?</h3>
            <ol>
                <li>Przeanalizujemy Twoje zgłoszenie</li>
                <li>Skontaktujemy się w ciągu 5-7 dni roboczych</li>
                <li>Zaprosimy na szkolenie wprowadzające</li>
            </ol>

            <p>Dziękujemy za chęć pomocy naszym podopiecznym!</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVolunteerApprovalTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Twoje zgłoszenie wolontariatu zostało zaakceptowane!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <div class=""success-box"">
                <p>Z radością informujemy, że zostałeś/aś przyjęty/a do programu wolontariatu!</p>
            </div>

            <h3>Szkolenie wprowadzające</h3>
            <div class=""info-box"">
                <p class=""detail""><strong>Data:</strong> {{ TrainingStartDate | date: '%d.%m.%Y' }} o godz. {{ TrainingStartDate | date: '%H:%M' }}</p>
                {% if TrainingLocation != '' %}
                <p class=""detail""><strong>Miejsce:</strong> {{ TrainingLocation }}</p>
                {% endif %}
            </div>

            {% if TrainingInstructions != '' %}
            <p>{{ TrainingInstructions }}</p>
            {% endif %}

            <p>Cieszymy się, że dołączasz do naszego zespołu!</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetVolunteerActivationTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Witaj w zespole wolontariuszy!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <div class=""success-box"">
                <p>Gratulacje! Jesteś teraz aktywnym wolontariuszem naszego schroniska!</p>
            </div>

            <div class=""info-box"">
                <p class=""detail""><strong>Numer umowy:</strong> {{ ContractNumber }}</p>
            </div>

            {% if VolunteerPortalUrl != '' %}
            <p style=""text-align: center;"">
                <a href=""{{ VolunteerPortalUrl }}"" class=""button"">Panel wolontariusza</a>
            </p>
            {% endif %}

            <p>Dziękujemy za Twoje zaangażowanie w pomoc zwierzętom!</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetPasswordResetTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Resetowanie hasła</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta.</p>

            <p style=""text-align: center;"">
                <a href=""{{ ResetUrl }}"" class=""button"">Zresetuj hasło</a>
            </p>

            <div class=""warning-box"">
                <p>Link jest ważny przez {{ ExpirationMinutes }} minut.</p>
                <p>Jeśli nie prosiłeś/aś o reset hasła, zignoruj tę wiadomość.</p>
            </div>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    private string GetWelcomeEmailTemplate()
    {
        return GetBaseTemplate(@"
            <h2>Witaj w {{ ShelterName }}!</h2>
            <p>Drogi/Droga {{ RecipientName }},</p>

            <p>Dziękujemy za założenie konta w naszym serwisie!</p>

            <div class=""info-box"">
                <p>Teraz możesz:</p>
                <ul>
                    <li>Przeglądać zwierzęta do adopcji</li>
                    <li>Składać wnioski adopcyjne</li>
                    <li>Zgłosić się jako wolontariusz</li>
                    <li>Śledzić status swoich zgłoszeń</li>
                </ul>
            </div>

            <p style=""text-align: center;"">
                <a href=""{{ LoginUrl }}"" class=""button"">Przejdź do serwisu</a>
            </p>

            <p>Mamy nadzieję, że znajdziesz u nas swojego nowego przyjaciela!</p>

            <p>Z pozdrowieniami,<br>Zespół {{ ShelterName }}</p>
        ");
    }

    #endregion
}

internal class EmailTemplateDefinition
{
    public required string SubjectTemplate { get; set; }
    public required string HtmlTemplate { get; set; }
    public string? TextTemplate { get; set; }
}
