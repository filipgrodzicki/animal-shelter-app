using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Appointments;
using ShelterApp.Domain.Cms;
using ShelterApp.Domain.Notifications;
using ShelterApp.Domain.Users;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Common;

/// <summary>
/// Class responsible for seeding sample data into the database
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds sample data into the database (Development environment only)
    /// </summary>
    public static async Task SeedSampleDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Seed test users
            await SeedUsersAsync(userManager, logger);

            // Seed animals only if database is empty
            if (!await dbContext.Animals.AnyAsync())
            {
                await SeedAnimalsAsync(dbContext, userManager, logger);
            }
            else
            {
                logger.LogInformation("Przykładowe zwierzęta już istnieją w bazie - pomijam seeding");
            }

            // Seed volunteer and attendance history
            await SeedVolunteerAttendanceAsync(dbContext, userManager, logger);

            // Seed volunteer duty schedule
            await SeedVolunteerScheduleAsync(dbContext, userManager, logger);

            // Seed adoption visit slots
            await SeedVisitSlotsAsync(dbContext, logger);

            // Seed CMS content (blog posts, FAQ)
            await SeedCmsContentAsync(dbContext, logger);

            // Seed admin notifications
            await SeedAdminNotificationsAsync(dbContext, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas seedowania przykładowych danych");
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        // ==================== GŁÓWNE KONTA TESTOWE ====================

        // 1. Administrator - Krzystof Dąbrowski
        const string adminEmail = "schronisko.dla.zwierzat+krzystof.dabrowski@outlook.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = ApplicationUser.Create(
                email: adminEmail,
                firstName: "Krzysztof",
                lastName: "Dąbrowski",
                phoneNumber: "+48 444 444 444"
            );
            admin.UpdateProfile(
                firstName: "Krzysztof",
                lastName: "Dąbrowski",
                phoneNumber: "+48 444 444 444",
                dateOfBirth: new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Polna 5",
                city: "Biała Podlaska",
                postalCode: "21-500"
            );

            var result = await userManager.CreateAsync(admin, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(admin);
                await userManager.ConfirmEmailAsync(admin, token);
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                logger.LogInformation("Utworzono administratora: {Email} / Schronisko123!", adminEmail);
            }
        }

        // 2. Pracownik schroniska - Piotr Zieliński
        const string staffEmail = "schronisko.dla.zwierzat+piotr.zielinski@outlook.com";
        if (await userManager.FindByEmailAsync(staffEmail) is null)
        {
            var staff = ApplicationUser.Create(
                email: staffEmail,
                firstName: "Piotr",
                lastName: "Zieliński",
                phoneNumber: "+48 333 333 333"
            );
            staff.UpdateProfile(
                firstName: "Piotr",
                lastName: "Zieliński",
                phoneNumber: "+48 333 333 333",
                dateOfBirth: new DateTime(1990, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Ogrodowa 22/10",
                city: "Warszawa",
                postalCode: "00-456"
            );

            var result = await userManager.CreateAsync(staff, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(staff);
                await userManager.ConfirmEmailAsync(staff, token);
                await userManager.AddToRoleAsync(staff, AppRoles.Staff);
                logger.LogInformation("Utworzono pracownika: {Email} / Schronisko123!", staffEmail);
            }
        }

        // 3. Wolontariusz - Anna Nowak (Active)
        const string volunteerEmail = "schronisko.dla.zwierzat+anna.nowak@outlook.com";
        if (await userManager.FindByEmailAsync(volunteerEmail) is null)
        {
            var volunteer = ApplicationUser.Create(
                email: volunteerEmail,
                firstName: "Anna",
                lastName: "Nowak",
                phoneNumber: "+48 222 222 222"
            );
            volunteer.UpdateProfile(
                firstName: "Anna",
                lastName: "Nowak",
                phoneNumber: "+48 222 222 222",
                dateOfBirth: new DateTime(1995, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Kwiatowa 8",
                city: "Lublin",
                postalCode: "20-100"
            );

            var result = await userManager.CreateAsync(volunteer, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(volunteer);
                await userManager.ConfirmEmailAsync(volunteer, token);
                await userManager.AddToRoleAsync(volunteer, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email} / Schronisko123!", volunteerEmail);
            }
        }

        // ==================== DODATKOWI WOLONTARIUSZE ====================

        // Wolontariusz 2 - Michał Kowalczyk (Active)
        const string volunteer2Email = "schronisko.dla.zwierzat+michal.kowalczyk@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer2Email) is null)
        {
            var vol2 = ApplicationUser.Create(
                email: volunteer2Email,
                firstName: "Michał",
                lastName: "Kowalczyk",
                phoneNumber: "+48 512 345 678"
            );
            vol2.UpdateProfile(
                firstName: "Michał",
                lastName: "Kowalczyk",
                phoneNumber: "+48 512 345 678",
                dateOfBirth: new DateTime(1998, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Parkowa 15",
                city: "Warszawa",
                postalCode: "02-100"
            );
            var result = await userManager.CreateAsync(vol2, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol2);
                await userManager.ConfirmEmailAsync(vol2, token);
                await userManager.AddToRoleAsync(vol2, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer2Email);
            }
        }

        // Wolontariusz 3 - Ewa Wiśniewska (InTraining)
        const string volunteer3Email = "schronisko.dla.zwierzat+ewa.wisniewska@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer3Email) is null)
        {
            var vol3 = ApplicationUser.Create(
                email: volunteer3Email,
                firstName: "Ewa",
                lastName: "Wiśniewska",
                phoneNumber: "+48 513 456 789"
            );
            vol3.UpdateProfile(
                firstName: "Ewa",
                lastName: "Wiśniewska",
                phoneNumber: "+48 513 456 789",
                dateOfBirth: new DateTime(2000, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Słoneczna 22",
                city: "Kraków",
                postalCode: "30-200"
            );
            var result = await userManager.CreateAsync(vol3, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol3);
                await userManager.ConfirmEmailAsync(vol3, token);
                await userManager.AddToRoleAsync(vol3, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer3Email);
            }
        }

        // Wolontariusz 4 - Tomasz Lewandowski (Candidate)
        const string volunteer4Email = "schronisko.dla.zwierzat+tomasz.lewandowski@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer4Email) is null)
        {
            var vol4 = ApplicationUser.Create(
                email: volunteer4Email,
                firstName: "Tomasz",
                lastName: "Lewandowski",
                phoneNumber: "+48 514 567 890"
            );
            vol4.UpdateProfile(
                firstName: "Tomasz",
                lastName: "Lewandowski",
                phoneNumber: "+48 514 567 890",
                dateOfBirth: new DateTime(1992, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Górna 8",
                city: "Gdańsk",
                postalCode: "80-100"
            );
            var result = await userManager.CreateAsync(vol4, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol4);
                await userManager.ConfirmEmailAsync(vol4, token);
                await userManager.AddToRoleAsync(vol4, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer4Email);
            }
        }

        // Wolontariusz 5 - Karolina Dąbrowska (Active)
        const string volunteer5Email = "schronisko.dla.zwierzat+karolina.dabrowska@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer5Email) is null)
        {
            var vol5 = ApplicationUser.Create(
                email: volunteer5Email,
                firstName: "Karolina",
                lastName: "Dąbrowska",
                phoneNumber: "+48 515 678 901"
            );
            vol5.UpdateProfile(
                firstName: "Karolina",
                lastName: "Dąbrowska",
                phoneNumber: "+48 515 678 901",
                dateOfBirth: new DateTime(1997, 6, 18, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Leśna 33",
                city: "Poznań",
                postalCode: "60-300"
            );
            var result = await userManager.CreateAsync(vol5, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol5);
                await userManager.ConfirmEmailAsync(vol5, token);
                await userManager.AddToRoleAsync(vol5, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer5Email);
            }
        }

        // Wolontariusz 6 - Paweł Kaczmarek (Suspended)
        const string volunteer6Email = "schronisko.dla.zwierzat+pawel.kaczmarek@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer6Email) is null)
        {
            var vol6 = ApplicationUser.Create(
                email: volunteer6Email,
                firstName: "Paweł",
                lastName: "Kaczmarek",
                phoneNumber: "+48 516 789 012"
            );
            vol6.UpdateProfile(
                firstName: "Paweł",
                lastName: "Kaczmarek",
                phoneNumber: "+48 516 789 012",
                dateOfBirth: new DateTime(1994, 9, 25, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Krótka 5",
                city: "Wrocław",
                postalCode: "50-400"
            );
            var result = await userManager.CreateAsync(vol6, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol6);
                await userManager.ConfirmEmailAsync(vol6, token);
                await userManager.AddToRoleAsync(vol6, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer6Email);
            }
        }

        // Wolontariusz 7 - Magdalena Szymańska (Candidate)
        const string volunteer7Email = "schronisko.dla.zwierzat+magdalena.szymanska@outlook.com";
        if (await userManager.FindByEmailAsync(volunteer7Email) is null)
        {
            var vol7 = ApplicationUser.Create(
                email: volunteer7Email,
                firstName: "Magdalena",
                lastName: "Szymańska",
                phoneNumber: "+48 517 890 123"
            );
            vol7.UpdateProfile(
                firstName: "Magdalena",
                lastName: "Szymańska",
                phoneNumber: "+48 517 890 123",
                dateOfBirth: new DateTime(2001, 1, 30, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Cicha 12",
                city: "Łódź",
                postalCode: "90-500"
            );
            var result = await userManager.CreateAsync(vol7, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(vol7);
                await userManager.ConfirmEmailAsync(vol7, token);
                await userManager.AddToRoleAsync(vol7, AppRoles.Volunteer);
                logger.LogInformation("Utworzono wolontariusza: {Email}", volunteer7Email);
            }
        }

        // 4. Użytkownik zainteresowany adopcją - Jan Kowalski
        const string userEmail = "schronisko.dla.zwierzat+jan.kowalski@outlook.com";
        if (await userManager.FindByEmailAsync(userEmail) is null)
        {
            var user = ApplicationUser.Create(
                email: userEmail,
                firstName: "Jan",
                lastName: "Kowalski",
                phoneNumber: "+48 111 111 111"
            );
            user.UpdateProfile(
                firstName: "Jan",
                lastName: "Kowalski",
                phoneNumber: "+48 111 111 111",
                dateOfBirth: new DateTime(1988, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Lipowa 15/3",
                city: "Warszawa",
                postalCode: "01-234"
            );

            var result = await userManager.CreateAsync(user, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                await userManager.ConfirmEmailAsync(user, token);
                await userManager.AddToRoleAsync(user, AppRoles.User);
                logger.LogInformation("Utworzono użytkownika: {Email} / Schronisko123!", userEmail);
            }
        }

        // ==================== DODATKOWI ADOPTERZY (do historii adopcji) ====================

        // Adopter 1 - adoptował Orzeszka - Tomasz Wiśniewski
        const string adopter1Email = "schronisko.dla.zwierzat+tomasz.wisniewski@outlook.com";
        if (await userManager.FindByEmailAsync(adopter1Email) is null)
        {
            var adopter1 = ApplicationUser.Create(
                email: adopter1Email,
                firstName: "Tomasz",
                lastName: "Wiśniewski",
                phoneNumber: "+48 501 234 567"
            );
            adopter1.UpdateProfile(
                firstName: "Tomasz",
                lastName: "Wiśniewski",
                phoneNumber: "+48 501 234 567",
                dateOfBirth: new DateTime(1982, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Słoneczna 12",
                city: "Warszawa",
                postalCode: "02-758"
            );

            var result = await userManager.CreateAsync(adopter1, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(adopter1);
                await userManager.ConfirmEmailAsync(adopter1, token);
                await userManager.AddToRoleAsync(adopter1, AppRoles.User);
                logger.LogInformation("Utworzono adoptera: {Email} / Schronisko123!", adopter1Email);
            }
        }

        // Adopter 2 - adoptowała Biankę - Katarzyna Malinowska
        const string adopter2Email = "schronisko.dla.zwierzat+katarzyna.malinowska@outlook.com";
        if (await userManager.FindByEmailAsync(adopter2Email) is null)
        {
            var adopter2 = ApplicationUser.Create(
                email: adopter2Email,
                firstName: "Katarzyna",
                lastName: "Malinowska",
                phoneNumber: "+48 502 345 678"
            );
            adopter2.UpdateProfile(
                firstName: "Katarzyna",
                lastName: "Malinowska",
                phoneNumber: "+48 502 345 678",
                dateOfBirth: new DateTime(1991, 9, 25, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Morska 45/8",
                city: "Gdańsk",
                postalCode: "80-298"
            );

            var result = await userManager.CreateAsync(adopter2, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(adopter2);
                await userManager.ConfirmEmailAsync(adopter2, token);
                await userManager.AddToRoleAsync(adopter2, AppRoles.User);
                logger.LogInformation("Utworzono adoptera: {Email} / Schronisko123!", adopter2Email);
            }
        }

        // Adopter 3 - adoptowała Mruczka - Maria Zielińska (seniorka)
        const string adopter3Email = "schronisko.dla.zwierzat+maria.zielinska@outlook.com";
        if (await userManager.FindByEmailAsync(adopter3Email) is null)
        {
            var adopter3 = ApplicationUser.Create(
                email: adopter3Email,
                firstName: "Maria",
                lastName: "Zielińska",
                phoneNumber: "+48 503 456 789"
            );
            adopter3.UpdateProfile(
                firstName: "Maria",
                lastName: "Zielińska",
                phoneNumber: "+48 503 456 789",
                dateOfBirth: new DateTime(1958, 2, 14, 0, 0, 0, DateTimeKind.Utc),
                address: "ul. Długa 7/2",
                city: "Kraków",
                postalCode: "31-147"
            );

            var result = await userManager.CreateAsync(adopter3, "Schronisko123!");
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(adopter3);
                await userManager.ConfirmEmailAsync(adopter3, token);
                await userManager.AddToRoleAsync(adopter3, AppRoles.User);
                logger.LogInformation("Utworzono adoptera: {Email} / Schronisko123!", adopter3Email);
            }
        }
    }

    private static async Task SeedAnimalsAsync(ShelterDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var animals = new List<Animal>();

        // ==================== DOGS (15) ====================

        // 1. Burek - Available
        var burek = Animal.Create(
            registrationNumber: "PSY-2024-001",
            species: Species.Dog,
            breed: "Kundelek",
            name: "Burek",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Brązowy z białymi łatami",
            admissionCircumstances: "Znaleziony na ulicy, bez obroży i chipa",
            ageInMonths: 36,
            chipNumber: "616093900012345",
            admissionDate: DateTime.UtcNow.AddDays(-1),
            description: "Burek to niezwykle przyjazny i towarzyski pies. Uwielbia spacery i zabawę z piłką. Dobrze dogaduje się z innymi psami i dziećmi. Zna podstawowe komendy.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        burek.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        burek.AddPhoto("burek1.jpg", "https://images.unsplash.com/photo-1537151625747-768eb6cf92b2?w=600&h=600&fit=crop", "image/jpeg", 50000, true, "Burek w schronisku");
        // Medical records
        burek.AddMedicalRecord(
            MedicalRecordType.Vaccination,
            "Szczepienie przeciw wściekliźnie",
            "Podano szczepionkę Nobivac Rabies",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddMonths(-2),
            notes: "Pies zniósł szczepienie dobrze"
        );
        burek.AddMedicalRecord(
            MedicalRecordType.Deworming,
            "Odrobaczanie",
            "Podano preparat Milbemax",
            "lek. wet. Anna Nowak",
            DateTime.UtcNow.AddMonths(-1)
        );
        burek.AddMedicalRecord(
            MedicalRecordType.Sterilization,
            "Kastracja",
            "Przeprowadzono zabieg kastracji",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddMonths(-3),
            notes: "Zabieg przebiegł bez komplikacji, gojenie prawidłowe"
        );
        animals.Add(burek);

        // 2. Luna - Available (surrendered by owner)
        var luna = Animal.Create(
            registrationNumber: "PSY-2024-002",
            species: Species.Dog,
            breed: "Labrador Retriever",
            name: "Luna",
            gender: Gender.Female,
            size: Size.Large,
            color: "Złoty",
            admissionCircumstances: "Oddana przez właściciela z powodu alergii w rodzinie",
            ageInMonths: 18,
            chipNumber: "616093900012346",
            admissionDate: DateTime.UtcNow.AddDays(-3),
            description: "Luna to energiczna i inteligentna labradorka. Wymaga dużo ruchu i aktywności. Idealna dla aktywnej rodziny z ogrodem. Uwielbia wodę i aportowanie.",
            experienceLevel: ExperienceLevel.Advanced,
            surrenderedByFirstName: "Paweł",
            surrenderedByLastName: "Nowicki",
            surrenderedByPhone: "512345678",
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        luna.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        luna.AddPhoto("luna1.jpg", "https://images.unsplash.com/photo-1552053831-71594a27632d?w=600&h=600&fit=crop", "image/jpeg", 48000, true, "Luna - złoty labrador");
        // Medical records
        luna.AddMedicalRecord(
            MedicalRecordType.Vaccination,
            "Szczepienie kompleksowe",
            "Podano szczepionkę DHPPiL + Rabies",
            "lek. wet. Maria Wiśniewska",
            DateTime.UtcNow.AddMonths(-1),
            notes: "Szczepienie przypomniane, pies w dobrej kondycji"
        );
        luna.AddMedicalRecord(
            MedicalRecordType.Examination,
            "Badanie ogólne przy przyjęciu",
            "Przeprowadzono pełne badanie kliniczne",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddMonths(-2),
            diagnosis: "Stan zdrowia dobry, bez niepokojących objawów"
        );
        animals.Add(luna);

        // 3. Max - Available (surrendered by family after owner's death)
        var max = Animal.Create(
            registrationNumber: "PSY-2024-003",
            species: Species.Dog,
            breed: "Owczarek niemiecki",
            name: "Max",
            gender: Gender.Male,
            size: Size.Large,
            color: "Czarno-podpalany",
            admissionCircumstances: "Właściciel zmarł, rodzina nie mogła się nim zająć",
            ageInMonths: 96,
            chipNumber: "616093900012347",
            description: "Max to spokojny i doświadczony pies. Doskonale wyszkolony, zna wiele komend. Idealny towarzysz dla spokojnej osoby lub pary. Wymaga regularnych spacerów.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.HouseWithGarden,
            surrenderedByFirstName: "Krzysztof",
            surrenderedByLastName: "Adamski",
            surrenderedByPhone: "504567890",
            distinguishingMarks: "Ciemniejsza łata na prawym boku"
        );
        max.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        max.AddPhoto("max1.jpg", "https://images.unsplash.com/photo-1589941013453-ec89f33b5e95?w=600&h=600&fit=crop", "image/jpeg", 55000, true, "Max - owczarek niemiecki");
        max.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie kompleksowe", "Szczepienie DHPPi + Rabies", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-6));
        max.AddMedicalRecord(MedicalRecordType.Examination, "Badanie geriatryczne", "Badanie kontrolne psa senioralnego", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-1), diagnosis: "Stan zdrowia dobry jak na wiek");
        animals.Add(max);

        // 4. Reksio - Available (puppy)
        var reksio = Animal.Create(
            registrationNumber: "PSY-2024-004",
            species: Species.Dog,
            breed: "Mieszaniec",
            name: "Reksio",
            gender: Gender.Male,
            size: Size.Small,
            color: "Biało-rudy",
            admissionCircumstances: "Znaleziony w kartonie przy drodze wraz z rodzeństwem",
            ageInMonths: 4,
            admissionDate: DateTime.UtcNow.AddDays(-5),
            description: "Reksio to uroczy szczeniak pełen energii. Wymaga cierpliwości w wychowaniu i socjalizacji. Będzie średniej wielkości psem. Szuka kochającego domu.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        reksio.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        reksio.AddPhoto("reksio1.jpg", "https://images.unsplash.com/photo-1546975490-e8b92a360b24?w=600&h=600&fit=crop", "image/jpeg", 42000, true, "Reksio - uroczy szczeniak");
        reksio.AddMedicalRecord(MedicalRecordType.Vaccination, "Pierwsza szczepionka", "Szczepienie szczeniąt - Nobivac Puppy", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-1));
        reksio.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Podano Drontal Junior", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddDays(-14));
        animals.Add(reksio);

        // 5. Azor - UnderVeterinaryCare
        var azor = Animal.Create(
            registrationNumber: "PSY-2024-005",
            species: Species.Dog,
            breed: "Amstaff mix",
            name: "Azor",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Szary",
            admissionCircumstances: "Odebrany interwencyjnie, zaniedbany",
            ageInMonths: 48,
            description: "Azor przechodzi rehabilitację po trudnych doświadczeniach. Jest nieufny wobec obcych, ale bardzo oddany opiekunom. Wymaga doświadczonego właściciela.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.No,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.MoreThan3Hours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        azor.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        azor.ChangeStatus(AnimalStatusTrigger.WykrycieChoroby, "System");
        azor.AddPhoto("azor1.jpg", "https://images.unsplash.com/photo-1477884213360-7e9d7dcc1e48?w=600&h=600&fit=crop", "image/jpeg", 47000, true, "Azor w trakcie rehabilitacji");
        // Medical records - treatment case
        azor.AddMedicalRecord(
            MedicalRecordType.Examination,
            "Badanie przy przyjęciu",
            "Pies w stanie niedożywienia, liczne rany na skórze",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddMonths(-2),
            diagnosis: "Niedożywienie, infekcje skórne, możliwe pasożyty"
        );
        azor.AddMedicalRecord(
            MedicalRecordType.Treatment,
            "Rozpoczęcie leczenia dermatologicznego",
            "Wdrożono antybiotykoterapię i leczenie miejscowe ran",
            "lek. wet. Anna Nowak",
            DateTime.UtcNow.AddMonths(-2),
            treatment: "Amoksycylina 2x dziennie, maść gojąca na rany",
            medications: "Amoksycylina 500mg, Bepanthen"
        );
        azor.AddMedicalRecord(
            MedicalRecordType.Deworming,
            "Odrobaczanie",
            "Podano preparat przeciwpasożytniczy",
            "lek. wet. Anna Nowak",
            DateTime.UtcNow.AddMonths(-1),
            notes: "Potwierdzono obecność pasożytów - leczenie zakończone sukcesem"
        );
        azor.AddMedicalRecord(
            MedicalRecordType.Examination,
            "Badanie kontrolne",
            "Kontrola postępu leczenia",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddDays(-14),
            diagnosis: "Znaczna poprawa stanu zdrowia, rany w trakcie gojenia",
            nextVisitDate: DateTime.UtcNow.AddDays(14)
        );
        animals.Add(azor);

        // 6. Tofik - InAdoptionProcess
        var tofik = Animal.Create(
            registrationNumber: "PSY-2024-006",
            species: Species.Dog,
            breed: "Jack Russell Terrier",
            name: "Tofik",
            gender: Gender.Male,
            size: Size.Small,
            color: "Biało-brązowy",
            admissionCircumstances: "Właściciel się wyprowadził i zostawił psa",
            ageInMonths: 24,
            chipNumber: "616093900012348",
            description: "Tofik to energiczny i wesoły piesek. Uwielbia zabawę i bieganie. Idealny dla aktywnej rodziny.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        tofik.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        tofik.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        tofik.AddPhoto("tofik1.jpg", "https://images.unsplash.com/photo-1517849845537-4d257902454a?w=600&h=600&fit=crop", "image/jpeg", 45000, true, "Tofik - Jack Russell");
        tofik.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie roczne", "Szczepienie DHPPi + L4", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-4));
        tofik.AddMedicalRecord(MedicalRecordType.Microchipping, "Wszczepienie chipa", "Implantacja mikrochipa", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-4));
        animals.Add(tofik);

        // 7. Bella - Available
        var bella = Animal.Create(
            registrationNumber: "PSY-2024-007",
            species: Species.Dog,
            breed: "Siberian Husky",
            name: "Bella",
            gender: Gender.Female,
            size: Size.Large,
            color: "Szaro-biały",
            admissionCircumstances: "Znaleziona błąkająca się po mieście",
            ageInMonths: 30,
            chipNumber: "616093900012349",
            admissionDate: DateTime.UtcNow.AddDays(-7),
            description: "Bella to piękna husky z niebieskimi oczami. Bardzo przyjazna, ale wymaga dużo ruchu.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        bella.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        bella.AddPhoto("bella1.jpg", "https://images.unsplash.com/photo-1605568427561-40dd23c2acea?w=600&h=600&fit=crop", "image/jpeg", 52000, true, "Bella - husky");
        bella.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Badanie ogólne stanu zdrowia", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2));
        bella.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Podano Milbemax", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2));
        bella.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie", "Szczepienie przeciw wściekliźnie", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-1));
        animals.Add(bella);

        // 8. Bruno - Reserved (surrendered by owner)
        var bruno = Animal.Create(
            registrationNumber: "PSY-2024-008",
            species: Species.Dog,
            breed: "Buldog francuski",
            name: "Bruno",
            gender: Gender.Male,
            size: Size.Small,
            color: "Cynamonowy",
            admissionCircumstances: "Oddany z powodu problemów zdrowotnych właściciela",
            ageInMonths: 48,
            chipNumber: "616093900012350",
            description: "Bruno to spokojny i kochany buldog. Idealny towarzysz do mieszkania. Zarezerwowany przez rodzinę z Krakowa.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Stanisław",
            surrenderedByLastName: "Kowalczyk",
            surrenderedByPhone: "505678901"
        );
        bruno.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        bruno.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        bruno.AddPhoto("bruno1.jpg", "https://images.unsplash.com/photo-1583337130417-3346a1be7dee?w=600&h=600&fit=crop", "image/jpeg", 48000, true, "Bruno - buldog");
        bruno.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie kompleksowe", "DHPPi + Rabies", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-3));
        bruno.AddMedicalRecord(MedicalRecordType.Examination, "Badanie układu oddechowego", "Kontrola typowych problemów brachycefalicznych", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-1), diagnosis: "Łagodne zwężenie nozdrzy, monitorowanie");
        animals.Add(bruno);

        // 9. Maja - InAdoptionProcess (surrendered by family after owner's death)
        var maja = Animal.Create(
            registrationNumber: "PSY-2024-009",
            species: Species.Dog,
            breed: "Golden Retriever",
            name: "Maja",
            gender: Gender.Female,
            size: Size.Large,
            color: "Złoty",
            admissionCircumstances: "Poprzedni właściciel zmarł, rodzina nie mogła zapewnić opieki",
            ageInMonths: 60,
            chipNumber: "616093900012351",
            description: "Maja to niezwykle łagodna i kochająca sunia. Idealna dla rodziny z dziećmi. W trakcie procesu adopcyjnego.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.HouseWithGarden,
            surrenderedByFirstName: "Barbara",
            surrenderedByLastName: "Mazur",
            surrenderedByPhone: "506789012"
        );
        maja.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        maja.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        maja.AddPhoto("maja1.jpg", "https://images.unsplash.com/photo-1633722715463-d30f4f325e24?w=600&h=600&fit=crop", "image/jpeg", 51000, true, "Maja - golden retriever");
        maja.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie roczne", "DHPPi + Rabies + L4", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        maja.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji", "lek. wet. Anna Nowak", DateTime.UtcNow.AddYears(-3));
        maja.AddMedicalRecord(MedicalRecordType.DentalCare, "Higiena jamy ustnej", "Usunięcie kamienia nazębnego", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-1));
        animals.Add(maja);

        // 10. Rocky - Available
        var rocky = Animal.Create(
            registrationNumber: "PSY-2024-010",
            species: Species.Dog,
            breed: "Beagle",
            name: "Rocky",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Tricolor",
            admissionCircumstances: "Porzucony przy schronisku",
            ageInMonths: 15,
            description: "Rocky to młody beagle pełen energii. Uwielbia tropić i bawić się.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        rocky.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        rocky.AddPhoto("rocky1.jpg", "https://images.unsplash.com/photo-1505628346881-b72b27e84530?w=600&h=600&fit=crop", "image/jpeg", 46000, true, "Rocky - beagle");
        rocky.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie szczeniąt", "Nobivac Puppy DP", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-3));
        rocky.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Drontal Plus", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-2));
        animals.Add(rocky);

        // 11. Coco - Available (surrendered by family)
        var coco = Animal.Create(
            registrationNumber: "PSY-2024-011",
            species: Species.Dog,
            breed: "Shih Tzu",
            name: "Coco",
            gender: Gender.Female,
            size: Size.Small,
            color: "Biało-brązowy",
            admissionCircumstances: "Oddana przez rodzinę z powodu alergii dziecka",
            ageInMonths: 72,
            chipNumber: "616093900012352",
            description: "Coco to spokojna i elegancka dama. Wymaga regularnej pielęgnacji sierści.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Ewa",
            surrenderedByLastName: "Jankowska",
            surrenderedByPhone: "507890123"
        );
        coco.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        coco.AddPhoto("coco1.jpg", "https://images.unsplash.com/photo-1586671267731-da2cf3ceeb80?w=600&h=600&fit=crop", "image/jpeg", 44000, true, "Coco - shih tzu");
        coco.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie", "Szczepienie DHPPi + Rabies", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-5));
        coco.AddMedicalRecord(MedicalRecordType.DentalCare, "Czyszczenie zębów", "Profesjonalne czyszczenie i polerowanie", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2));
        coco.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddYears(-4));
        animals.Add(coco);

        // 12. Rex - Quarantine
        var rex = Animal.Create(
            registrationNumber: "PSY-2024-012",
            species: Species.Dog,
            breed: "Owczarek belgijski",
            name: "Rex",
            gender: Gender.Male,
            size: Size.Large,
            color: "Czarny",
            admissionCircumstances: "Nowo przyjęty, wymaga kwarantanny",
            ageInMonths: 42,
            description: "Rex to inteligentny i lojalny pies. Obecnie w kwarantannie. Po jej zakończeniu będzie dostępny do adopcji.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.No,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.MoreThan3Hours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        rex.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        rex.AddPhoto("rex1.jpg", "https://images.unsplash.com/photo-1553882809-a4f57e59501d?w=600&h=600&fit=crop", "image/jpeg", 53000, true, "Rex - owczarek belgijski");
        rex.AddMedicalRecord(MedicalRecordType.Examination, "Badanie kwarantannowe", "Wstępne badanie przy przyjęciu do kwarantanny", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-5));
        rex.AddMedicalRecord(MedicalRecordType.Laboratory, "Badania laboratoryjne", "Morfologia i biochemia krwi", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-5), diagnosis: "Wyniki w normie");
        animals.Add(rex);

        // 13. Milo - Available (surrendered due to divorce)
        var milo = Animal.Create(
            registrationNumber: "PSY-2024-013",
            species: Species.Dog,
            breed: "Welsh Corgi Pembroke",
            name: "Milo",
            gender: Gender.Male,
            size: Size.Small,
            color: "Rudy z białym",
            admissionCircumstances: "Właściciele się rozwodzili, żadne z nich nie mogło zatrzymać psa",
            ageInMonths: 20,
            description: "Milo to wesoły corgi z charakterystycznymi krótkimi nóżkami. Bardzo towarzyski.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Joanna",
            surrenderedByLastName: "Kamińska",
            surrenderedByPhone: "508901234"
        );
        milo.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        milo.AddPhoto("milo1.jpg", "https://images.unsplash.com/photo-1612536057832-2ff7ead58194?w=600&h=600&fit=crop", "image/jpeg", 47000, true, "Milo - corgi");
        milo.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie roczne", "DHPPi + Rabies", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-4));
        milo.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-6));
        animals.Add(milo);

        // 14. Orzeszek - Adopted
        var orzeszek = Animal.Create(
            registrationNumber: "PSY-2024-014",
            species: Species.Dog,
            breed: "Cocker Spaniel",
            name: "Orzeszek",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Złoty",
            admissionCircumstances: "Znaleziony na autostradzie",
            ageInMonths: 36,
            chipNumber: "616093900012353",
            description: "Orzeszek znalazł swój dom! Został adoptowany przez kochającą rodzinę z Warszawy.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        orzeszek.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        orzeszek.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        orzeszek.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "System");
        orzeszek.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "System");
        orzeszek.AddPhoto("orzeszek1.jpg", "https://images.unsplash.com/photo-1530281700549-e82e7bf110d6?w=600&h=600&fit=crop", "image/jpeg", 48000, true, "Orzeszek - cocker spaniel");
        orzeszek.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie przed adopcją", "DHPPi + Rabies", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        orzeszek.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji przed adopcją", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2));
        orzeszek.AddMedicalRecord(MedicalRecordType.Microchipping, "Wszczepienie chipa", "Implantacja mikrochipa", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        animals.Add(orzeszek);

        // 15. Rudi - Adopted (found on the street in winter - blog story)
        var rudi = Animal.Create(
            registrationNumber: "PSY-2024-016",
            species: Species.Dog,
            breed: "Kundelek",
            name: "Rudi",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Rudy z białymi łatami",
            admissionCircumstances: "Znaleziony pod śmietnikiem w mroźny listopadowy wieczór 2024, wychłodzony i wychudzony",
            ageInMonths: 42,
            chipNumber: "616093900012354",
            description: "Rudi znalazł swój wymarzony dom! Po trudnych początkach na ulicy, teraz cieszy się życiem z kochającą rodziną Kamińskich. Waży 22 kg, ma lśniącą sierść i uwielbia gonić piłkę w ogrodzie.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.HouseWithGarden
        );
        rudi.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        rudi.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        rudi.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "System");
        rudi.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "System");
        rudi.AddPhoto("rudi1.jpg", "https://images.unsplash.com/photo-1561037404-61cd46aa615b?w=600&h=600&fit=crop", "image/jpeg", 49000, true, "Rudi - szczęśliwy w nowym domu");
        rudi.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Pies wychłodzony, wychudzony (12 kg), odmrożenia na uszach i łapach, infekcje skórne", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-14), diagnosis: "Niedożywienie, odmrożenia, robaki, infekcja skóry");
        rudi.AddMedicalRecord(MedicalRecordType.Treatment, "Leczenie odmrożeń i infekcji", "Wdrożono intensywne leczenie: specjalna dieta, antybiotyki, opatrunki", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-14), treatment: "Dieta wysokokaloryczna, antybiotykoterapia, leczenie miejscowe ran");
        rudi.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Podano preparat przeciwpasożytniczy", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-13));
        rudi.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie", "Szczepienie DHPPi + Rabies po wyzdrowieniu", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-12));
        rudi.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji przed adopcją", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-11));
        rudi.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przed adopcją", "Pies w pełni zdrowy, waga 20 kg, gotowy do adopcji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-11), diagnosis: "Stan zdrowia bardzo dobry");
        animals.Add(rudi);

        // 16. Puszek (dog) - Deceased
        var puszekPies = Animal.Create(
            registrationNumber: "PSY-2024-015",
            species: Species.Dog,
            breed: "Mieszaniec",
            name: "Puszek",
            gender: Gender.Male,
            size: Size.Small,
            color: "Czarny",
            admissionCircumstances: "Znaleziony w bardzo złym stanie",
            ageInMonths: 144,
            description: "Puszek zmarł z powodu starości i chorób. Był z nami przez 2 lata. Odpoczywaj w spokoju.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        puszekPies.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        puszekPies.ChangeStatus(AnimalStatusTrigger.WykrycieChoroby, "System");
        puszekPies.ChangeStatus(AnimalStatusTrigger.Zgon, "System");
        puszekPies.AddPhoto("puszekPies1.jpg", "https://images.unsplash.com/photo-1518020382113-a7e8fc38eac9?w=600&h=600&fit=crop", "image/jpeg", 40000, true, "Puszek - pies");
        puszekPies.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Pies w złym stanie - wychudzony, apatyczny", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddYears(-2));
        puszekPies.AddMedicalRecord(MedicalRecordType.Treatment, "Leczenie paliatywne", "Wdrożono leczenie wspomagające", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-6));
        puszekPies.AddMedicalRecord(MedicalRecordType.Examination, "Ostatnie badanie", "Pogorszenie stanu zdrowia, niewydolność narządów", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-14), diagnosis: "Stan terminalny");
        animals.Add(puszekPies);

        // ==================== CATS (15) ====================

        // 1. Mruczka - Available (surrendered by owner)
        var mruczka = Animal.Create(
            registrationNumber: "KOT-2024-001",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Mruczka",
            gender: Gender.Female,
            size: Size.Small,
            color: "Szylkretowa",
            admissionCircumstances: "Oddana przez właściciela - przeprowadzka za granicę",
            ageInMonths: 60,
            chipNumber: "616093900022345",
            admissionDate: DateTime.UtcNow.AddDays(-2),
            description: "Mruczka to spokojna i zrównoważona kotka. Lubi być głaskana i spać na kolanach. Idealna dla osoby pracującej z domu lub seniora.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Agnieszka",
            surrenderedByLastName: "Dąbrowska",
            surrenderedByPhone: "509012345"
        );
        mruczka.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        mruczka.AddPhoto("mruczka1.jpg", "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=600&h=600&fit=crop", "image/jpeg", 41000, true, "Mruczka - spokojna kotka");
        // Medical records
        mruczka.AddMedicalRecord(
            MedicalRecordType.Vaccination,
            "Szczepienie FeLV + FIV",
            "Podano szczepionkę przeciw białaczce i FIV",
            "lek. wet. Maria Wiśniewska",
            DateTime.UtcNow.AddMonths(-3),
            notes: "Kotka zdrowa, szczepienie profilaktyczne"
        );
        mruczka.AddMedicalRecord(
            MedicalRecordType.Sterilization,
            "Sterylizacja",
            "Przeprowadzono zabieg sterylizacji",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddYears(-2),
            notes: "Zabieg wykonany u poprzedniego właściciela"
        );
        mruczka.AddMedicalRecord(
            MedicalRecordType.DentalCare,
            "Czyszczenie zębów",
            "Usunięto kamień nazębny",
            "lek. wet. Anna Nowak",
            DateTime.UtcNow.AddMonths(-1),
            notes: "Zęby w dobrym stanie, zalecana regularna higiena"
        );
        animals.Add(mruczka);

        // 2. Filemon - Available
        var filemon = Animal.Create(
            registrationNumber: "KOT-2024-002",
            species: Species.Cat,
            breed: "Dachowiec",
            name: "Filemon",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Rudy pręgowany",
            admissionCircumstances: "Znaleziony na działkach, prawdopodobnie porzucony",
            ageInMonths: 24,
            chipNumber: "616093900022346",
            admissionDate: DateTime.UtcNow.AddDays(-4),
            description: "Filemon to charyzmatyczny rudzielec z charakterem. Lubi zabawę i eksplorowanie. Czasem marudny, ale bardzo czuły. Akceptuje inne koty.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        filemon.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        filemon.AddPhoto("filemon1.jpg", "https://images.unsplash.com/photo-1495360010541-f48722b34f7d?w=600&h=600&fit=crop", "image/jpeg", 44000, true, "Filemon - rudy kocur");
        filemon.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP", "Szczepienie podstawowe dla kotów", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-3));
        filemon.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-4));
        filemon.AddMedicalRecord(MedicalRecordType.Microchipping, "Wszczepienie chipa", "Implantacja mikrochipa", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-4));
        animals.Add(filemon);

        // 3. Puszek (cat) - Available
        var puszek = Animal.Create(
            registrationNumber: "KOT-2024-003",
            species: Species.Cat,
            breed: "Pers mix",
            name: "Puszek",
            gender: Gender.Male,
            size: Size.Small,
            color: "Biały z szarymi znaczeniami",
            admissionCircumstances: "Urodzony w schronisku, matka była bezdomna",
            ageInMonths: 6,
            admissionDate: DateTime.UtcNow.AddDays(-6),
            description: "Puszek to uroczy puchaty kotek. Bardzo ciekawski i zabawowy. Jego długa sierść wymaga regularnego szczotkowania. Szuka cierpliwego opiekuna.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        puszek.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        puszek.AddPhoto("puszek1.jpg", "https://images.unsplash.com/photo-1690107560098-401c83d2330a?w=600&h=600&fit=crop", "image/jpeg", 40000, true, "Puszek - puchaty kotek");
        puszek.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie kociąt", "Pierwsza szczepionka FVRCP", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-1));
        puszek.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Profender spot-on", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddDays(-21));
        animals.Add(puszek);

        // 4. Kicia - Reserved
        var kicia = Animal.Create(
            registrationNumber: "KOT-2024-004",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Kicia",
            gender: Gender.Female,
            size: Size.Small,
            color: "Czarna",
            admissionCircumstances: "Znaleziona w piwnicy bloku mieszkalnego",
            ageInMonths: 36,
            description: "Kicia jest nieśmiała i potrzebuje czasu na oswojenie. Po aklimatyzacji staje się bardzo przywiązana. Zarezerwowana przez panią Marię.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.No,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        kicia.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        kicia.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        kicia.AddPhoto("kicia1.jpg", "https://images.unsplash.com/photo-1618826411640-d6df44dd3f7a?w=600&h=600&fit=crop", "image/jpeg", 43000, true, "Kicia - czarna kotka");
        kicia.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP + FeLV", "Pełne szczepienie", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        kicia.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-3));
        animals.Add(kicia);

        // 5. Misia - Quarantine
        var misia = Animal.Create(
            registrationNumber: "KOT-2024-005",
            species: Species.Cat,
            breed: "Dachowiec",
            name: "Misia",
            gender: Gender.Female,
            size: Size.Small,
            color: "Szara",
            admissionCircumstances: "Nowo przyjęta, wymaga obserwacji weterynaryjnej",
            ageInMonths: 12,
            description: "Misia jest w trakcie kwarantanny. Po jej zakończeniu będzie dostępna do adopcji. Wydaje się być towarzyska i przyjazna.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Partially,
            animalCompatibility: AnimalCompatibility.Partially,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        misia.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        misia.AddPhoto("misia1.jpg", "https://images.unsplash.com/photo-1533738363-b7f9aef128ce?w=600&h=600&fit=crop", "image/jpeg", 37000, true, "Misia - szara kotka");
        misia.AddMedicalRecord(MedicalRecordType.Examination, "Badanie kwarantannowe", "Wstępne badanie przy przyjęciu", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-3));
        misia.AddMedicalRecord(MedicalRecordType.Laboratory, "Test FeLV/FIV", "Badanie w kierunku białaczki i FIV", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-3), diagnosis: "Wynik ujemny");
        animals.Add(misia);

        // 6. Simba - Available
        var simba = Animal.Create(
            registrationNumber: "KOT-2024-006",
            species: Species.Cat,
            breed: "Pers",
            name: "Simba",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Rudy",
            admissionCircumstances: "Właścicielka trafiła do domu opieki",
            ageInMonths: 84,
            chipNumber: "616093900022347",
            admissionDate: DateTime.UtcNow.AddDays(-8),
            description: "Simba to majestatyczny pers o pięknej rudej sierści. Spokojny i zrównoważony.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        simba.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        simba.AddPhoto("simba1.jpg", "https://images.unsplash.com/photo-1591429939960-b7d5add10b5c?w=600&h=600&fit=crop", "image/jpeg", 49000, true, "Simba - pers");
        simba.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP + FeLV", "Pełne szczepienie kotów", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-6));
        simba.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddYears(-5));
        simba.AddMedicalRecord(MedicalRecordType.DentalCare, "Higiena jamy ustnej", "Usunięcie kamienia nazębnego u seniora", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2), notes: "Zęby w dobrym stanie jak na wiek");
        animals.Add(simba);

        // 7. Nala - InAdoptionProcess
        var nala = Animal.Create(
            registrationNumber: "KOT-2024-007",
            species: Species.Cat,
            breed: "Maine Coon mix",
            name: "Nala",
            gender: Gender.Female,
            size: Size.Large,
            color: "Srebrny tabby",
            admissionCircumstances: "Znaleziona w opuszczonym budynku",
            ageInMonths: 18,
            description: "Nala to piękna kotka o dużym rozmiarze. Bardzo przywiązuje się do opiekuna. W trakcie procesu adopcyjnego.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        nala.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        nala.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        nala.AddPhoto("nala1.jpg", "https://images.unsplash.com/photo-1606214174585-fe31582dc6ee?w=600&h=600&fit=crop", "image/jpeg", 46000, true, "Nala - maine coon mix");
        nala.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Pełne badanie kliniczne", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2), diagnosis: "Stan zdrowia dobry");
        nala.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP", "Podstawowe szczepienie kotów", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        nala.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-1));
        animals.Add(nala);

        // 8. Leo - Available
        var leo = Animal.Create(
            registrationNumber: "KOT-2024-008",
            species: Species.Cat,
            breed: "Brytyjski krótkowłosy",
            name: "Leo",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Niebieski (szary)",
            admissionCircumstances: "Urodzony w schronisku",
            ageInMonths: 8,
            admissionDate: DateTime.UtcNow.AddDays(-9),
            description: "Leo to uroczy brytyjczyk o pluszowej sierści. Bardzo łagodny i spokojny.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        leo.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        leo.AddPhoto("leo1.jpg", "https://images.unsplash.com/photo-1584396888493-06386077e877?w=600&h=600&fit=crop", "image/jpeg", 42000, true, "Leo - brytyjczyk");
        leo.AddMedicalRecord(MedicalRecordType.Vaccination, "Pierwsza szczepionka", "Szczepienie kociąt FVRCP", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-2));
        leo.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Profender spot-on", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-2));
        leo.AddMedicalRecord(MedicalRecordType.Examination, "Badanie kontrolne", "Kontrola rozwoju kociaka", "lek. wet. Anna Nowak", DateTime.UtcNow.AddDays(-14), diagnosis: "Zdrowy, prawidłowy rozwój");
        animals.Add(leo);

        // 9. Bianka - Adopted
        var bianka = Animal.Create(
            registrationNumber: "KOT-2024-009",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Bianka",
            gender: Gender.Female,
            size: Size.Small,
            color: "Biała",
            admissionCircumstances: "Porzucona w parku",
            ageInMonths: 30,
            chipNumber: "616093900022348",
            description: "Bianka znalazła kochający dom! Została adoptowana przez rodzinę z Gdańska.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment
        );
        bianka.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        bianka.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        bianka.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "System");
        bianka.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "System");
        bianka.AddPhoto("bianka1.jpg", "https://images.unsplash.com/photo-1513245543132-31f507417b26?w=600&h=600&fit=crop", "image/jpeg", 41000, true, "Bianka - biała kotka");
        bianka.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Kotka znaleziona w parku - badanie ogólne", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-3));
        bianka.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP + FeLV", "Pełne szczepienie przed adopcją", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        bianka.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji przed adopcją", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-2));
        bianka.AddMedicalRecord(MedicalRecordType.Microchipping, "Wszczepienie chipa", "Implantacja mikrochipa", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2));
        animals.Add(bianka);

        // 10. Tiger - Available (surrendered by owner)
        var tiger = Animal.Create(
            registrationNumber: "KOT-2024-010",
            species: Species.Cat,
            breed: "Dachowiec",
            name: "Tiger",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Brązowy tabby",
            admissionCircumstances: "Właściciel wyjechał za granicę i nie mógł zabrać kota",
            ageInMonths: 42,
            description: "Tiger to niezależny kocur o pięknym umaszczeniu. Lubi być sam, ale docenia pieszczoty.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.No,
            animalCompatibility: AnimalCompatibility.No,
            careTime: CareTime.OneToThreeHours,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Michał",
            surrenderedByLastName: "Lewandowski",
            surrenderedByPhone: "510123456"
        );
        tiger.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        tiger.AddPhoto("tiger1.jpg", "https://images.unsplash.com/photo-1494256997604-768d1f608cac?w=600&h=600&fit=crop", "image/jpeg", 45000, true, "Tiger - pręgowany");
        tiger.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie roczne", "Szczepienie FVRCP + Rabies", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-4));
        tiger.AddMedicalRecord(MedicalRecordType.Sterilization, "Kastracja", "Zabieg kastracji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddYears(-2));
        tiger.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Milbemax dla kotów", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-1));
        animals.Add(tiger);

        // 11. Mruczek - Adopted (surrendered by family after owner's death)
        var mruczek = Animal.Create(
            registrationNumber: "KOT-2024-011",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Mruczek",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Czarno-biały",
            admissionCircumstances: "Właściciel zmarł, rodzina nie mogła się zająć",
            ageInMonths: 120,
            chipNumber: "616093900022349",
            description: "Mruczek znalazł swój wymarzony dom! Został adoptowany przez seniorkę z Krakowa.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Elżbieta",
            surrenderedByLastName: "Wójcik",
            surrenderedByPhone: "511234567"
        );
        mruczek.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        mruczek.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "System");
        mruczek.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "System");
        mruczek.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "System");
        mruczek.AddPhoto("mruczek1.jpg", "https://images.unsplash.com/photo-1478098711619-5ab0b478d6e6?w=600&h=600&fit=crop", "image/jpeg", 43000, true, "Mruczek - senior");
        mruczek.AddMedicalRecord(MedicalRecordType.Examination, "Badanie geriatryczne", "Pełne badanie kota senioralnego", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2), diagnosis: "Stan zdrowia dobry jak na wiek, niewielka nadwaga");
        mruczek.AddMedicalRecord(MedicalRecordType.Laboratory, "Badania krwi", "Morfologia i biochemia", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-2), diagnosis: "Wyniki w normie");
        mruczek.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie przed adopcją", "FVRCP + Rabies", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-1));
        mruczek.AddMedicalRecord(MedicalRecordType.DentalCare, "Czyszczenie zębów", "Usunięcie kamienia nazębnego", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-1));
        animals.Add(mruczek);

        // 12. Figa - Available (surrendered by family)
        var figa = Animal.Create(
            registrationNumber: "KOT-2024-012",
            species: Species.Cat,
            breed: "Ragdoll mix",
            name: "Figa",
            gender: Gender.Female,
            size: Size.Medium,
            color: "Seal point",
            admissionCircumstances: "Oddana z powodu alergii noworodka",
            ageInMonths: 36,
            description: "Figa to spokojna i delikatna kotka. Uwielbia być noszona i przytulana.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment,
            surrenderedByFirstName: "Monika",
            surrenderedByLastName: "Szymańska",
            surrenderedByPhone: "512345678"
        );
        figa.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        figa.AddPhoto("figa1.jpg", "https://images.unsplash.com/photo-1518791841217-8f162f1e1131?w=600&h=600&fit=crop", "image/jpeg", 44000, true, "Figa - ragdoll mix");
        figa.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie FVRCP + FeLV", "Pełne szczepienie", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-3));
        figa.AddMedicalRecord(MedicalRecordType.Sterilization, "Sterylizacja", "Zabieg sterylizacji", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddYears(-1));
        figa.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Kotka w dobrym stanie, zadbana", "lek. wet. Anna Nowak", DateTime.UtcNow.AddMonths(-3), diagnosis: "Zdrowa, dobrze odżywiona");
        animals.Add(figa);

        // 13. Chmurka - Deceased
        var chmurka = Animal.Create(
            registrationNumber: "KOT-2024-013",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Chmurka",
            gender: Gender.Female,
            size: Size.Small,
            color: "Biało-szara",
            admissionCircumstances: "Znaleziona w ciężkim stanie zdrowia",
            ageInMonths: 180,
            description: "Chmurka zmarła z powodu niewydolności nerek. Była z nami przez 6 miesięcy. Będziemy o niej pamiętać.",
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        chmurka.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        chmurka.ChangeStatus(AnimalStatusTrigger.WykrycieChoroby, "System");
        chmurka.ChangeStatus(AnimalStatusTrigger.Zgon, "System");
        chmurka.AddPhoto("chmurka1.jpg", "https://images.unsplash.com/photo-1511044568932-338cba0ad803?w=600&h=600&fit=crop", "image/jpeg", 39000, true, "Chmurka");
        chmurka.AddMedicalRecord(MedicalRecordType.Examination, "Badanie przy przyjęciu", "Kotka w ciężkim stanie - apatyczna, odwodniona", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-6), diagnosis: "Podejrzenie niewydolności nerek");
        chmurka.AddMedicalRecord(MedicalRecordType.Laboratory, "Badania krwi", "Biochemia krwi - kreatynina, mocznik", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddMonths(-6), diagnosis: "Zaawansowana przewlekła niewydolność nerek (stadium IV)");
        chmurka.AddMedicalRecord(MedicalRecordType.Treatment, "Leczenie wspomagające", "Wdrożono terapię płynową i dietę nerkową", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-6), treatment: "Kroplówki podskórne, dieta Royal Canin Renal", medications: "Semintra, Ipakitine");
        chmurka.AddMedicalRecord(MedicalRecordType.Examination, "Badanie końcowe", "Pogorszenie stanu zdrowia, odmowa przyjmowania pokarmu", "lek. wet. Jan Kowalski", DateTime.UtcNow.AddDays(-7), diagnosis: "Stan terminalny, decyzja o eutanazji", notes: "Kotka odeszła spokojnie w obecności opiekunów");
        animals.Add(chmurka);

        // 14. Kleks - Available
        var kleks = Animal.Create(
            registrationNumber: "KOT-2024-014",
            species: Species.Cat,
            breed: "Dachowiec",
            name: "Kleks",
            gender: Gender.Male,
            size: Size.Small,
            color: "Biało-czarny (z czarną plamą na oku)",
            admissionCircumstances: "Urodzony w schronisku",
            ageInMonths: 5,
            description: "Kleks to młody, energiczny kotek z charakterystyczną czarną plamą wokół oka. Uwielbia się bawić.",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        kleks.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "System");
        kleks.AddPhoto("kleks1.jpg", "https://images.unsplash.com/photo-1574144611937-0df059b5ef3e?w=600&h=600&fit=crop", "image/jpeg", 38000, true, "Kleks - kotek z plamą");
        kleks.AddMedicalRecord(MedicalRecordType.Vaccination, "Pierwsza szczepionka", "Szczepienie kociąt FVRCP", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddMonths(-1));
        kleks.AddMedicalRecord(MedicalRecordType.Deworming, "Odrobaczanie", "Profender spot-on dla kociąt", "lek. wet. Maria Wiśniewska", DateTime.UtcNow.AddDays(-21));
        kleks.AddMedicalRecord(MedicalRecordType.Examination, "Badanie kontrolne", "Kontrola rozwoju kociaka", "lek. wet. Anna Nowak", DateTime.UtcNow.AddDays(-7), diagnosis: "Zdrowy, prawidłowy rozwój, gotowy do adopcji");
        animals.Add(kleks);

        // 15. Tosiek - UnderVeterinaryCare
        var tosiek = Animal.Create(
            registrationNumber: "KOT-2024-015",
            species: Species.Cat,
            breed: "Europejska krótkowłosa",
            name: "Tosiek",
            gender: Gender.Male,
            size: Size.Medium,
            color: "Pręgowany szary",
            admissionCircumstances: "Znaleziony ze złamaną łapą",
            ageInMonths: 24,
            description: "Tosiek jest obecnie pod opieką weterynarza po operacji złamanej łapy. Rokowania są dobre, wkrótce będzie dostępny do adopcji.",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            careTime: CareTime.LessThan1Hour,
            spaceRequirement: SpaceRequirement.Apartment
        );
        tosiek.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "System");
        tosiek.ChangeStatus(AnimalStatusTrigger.WykrycieChoroby, "System");
        tosiek.AddPhoto("tosiek1.jpg", "https://images.unsplash.com/photo-1543852786-1cf6624b9987?w=600&h=600&fit=crop", "image/jpeg", 42000, true, "Tosiek - w trakcie leczenia");
        // Medical records - broken leg
        tosiek.AddMedicalRecord(
            MedicalRecordType.Examination,
            "Badanie przy przyjęciu - uraz",
            "Kot znaleziony ze złamaną przednią łapą, prawdopodobnie potrącony przez samochód",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddDays(-21),
            diagnosis: "Złamanie kości promieniowej prawej przedniej kończyny"
        );
        tosiek.AddMedicalRecord(
            MedicalRecordType.Laboratory,
            "Badania przedoperacyjne",
            "Wykonano morfologię i biochemię krwi przed operacją",
            "lek. wet. Maria Wiśniewska",
            DateTime.UtcNow.AddDays(-21),
            diagnosis: "Wyniki w normie, pacjent kwalifikuje się do zabiegu"
        );
        tosiek.AddMedicalRecord(
            MedicalRecordType.Surgery,
            "Operacja złamania",
            "Przeprowadzono osteosyntezę płytkową kości promieniowej",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddDays(-14),
            treatment: "Osteosynteza płytkowa, założono opatrunek gipsowy",
            medications: "Metacam 0.5mg/kg raz dziennie przez 7 dni",
            notes: "Operacja przebiegła pomyślnie, rokowanie dobre",
            cost: 1500m
        );
        tosiek.AddMedicalRecord(
            MedicalRecordType.Examination,
            "Kontrola pooperacyjna",
            "Kontrola gojenia się rany i zrostu kości",
            "lek. wet. Jan Kowalski",
            DateTime.UtcNow.AddDays(-7),
            diagnosis: "Rana goi się prawidłowo, kot w dobrej kondycji",
            nextVisitDate: DateTime.UtcNow.AddDays(14),
            notes: "Zalecono ograniczenie aktywności, kontynuacja antybiotyku"
        );
        animals.Add(tosiek);

        // Add all animals to the database
        dbContext.Animals.AddRange(animals);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Dodano {Count} przykładowych zwierząt do bazy danych", animals.Count);

        // ==================== ADOPTION APPLICATIONS ====================
        // First create Adopter entities, then create applications

        var user1 = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+tomasz.wisniewski@outlook.com");
        var user2 = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+katarzyna.malinowska@outlook.com");
        var user3 = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+maria.zielinska@outlook.com");
        var user4 = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+jan.kowalski@outlook.com");

        if (user1 != null && user2 != null && user3 != null && user4 != null)
        {
            // Create Adopter entities
            var adopter1Result = Adopter.Create(
                user1.Id, "Tomasz", "Wiśniewski", user1.Email!, "+48 501 234 567",
                new DateTime(1982, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                "ul. Słoneczna 12", "Warszawa", "02-758", DateTime.UtcNow.AddDays(-60));

            var adopter2Result = Adopter.Create(
                user2.Id, "Katarzyna", "Malinowska", user2.Email!, "+48 502 345 678",
                new DateTime(1991, 9, 25, 0, 0, 0, DateTimeKind.Utc),
                "ul. Morska 45/8", "Gdańsk", "80-298", DateTime.UtcNow.AddDays(-45));

            var adopter3Result = Adopter.Create(
                user3.Id, "Maria", "Zielińska", user3.Email!, "+48 503 456 789",
                new DateTime(1958, 2, 14, 0, 0, 0, DateTimeKind.Utc),
                "ul. Długa 7/2", "Kraków", "31-147", DateTime.UtcNow.AddDays(-30));

            var adopter4Result = Adopter.Create(
                user4.Id, "Jan", "Kowalski", user4.Email!, "+48 111 111 111",
                new DateTime(1988, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                "ul. Lipowa 15/3", "Warszawa", "01-234", DateTime.UtcNow.AddDays(-7));

            if (!adopter1Result.IsSuccess || !adopter2Result.IsSuccess ||
                !adopter3Result.IsSuccess || !adopter4Result.IsSuccess)
            {
                logger.LogWarning("Nie udało się utworzyć wszystkich adopterów");
                return;
            }

            var adopter1 = adopter1Result.Value;
            var adopter2 = adopter2Result.Value;
            var adopter3 = adopter3Result.Value;
            var adopter4 = adopter4Result.Value;

            dbContext.Adopters.AddRange(adopter1, adopter2, adopter3, adopter4);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Utworzono 4 adopterów");

            var adoptedAnimals = await dbContext.Animals
                .Where(a => a.Status == AnimalStatus.Adopted)
                .ToListAsync();

            var orzeszekAnimal = adoptedAnimals.FirstOrDefault(a => a.Name == "Orzeszek");
            var biankaAnimal = adoptedAnimals.FirstOrDefault(a => a.Name == "Bianka");
            var mruczekAnimal = adoptedAnimals.FirstOrDefault(a => a.Name == "Mruczek");
            var rudiAnimal = adoptedAnimals.FirstOrDefault(a => a.Name == "Rudi");

            int appNumber = 1;

            // Adoption application for Orzeszek (completed 45 days ago)
            if (orzeszekAnimal != null)
            {
                var app1Result = AdoptionApplication.Create(
                    adopter1.Id,
                    orzeszekAnimal.Id,
                    "Zawsze marzyłem o psie typu spaniel. Mam duży dom z ogrodem.",
                    "Miałem psy przez całe życie, ostatni zmarł 2 lata temu.",
                    "Dom jednorodzinny z ogrodem 500m2, ogrodzony.",
                    "Brak innych zwierząt.",
                    applicationDate: DateTime.UtcNow.AddDays(-45)
                );
                if (app1Result.IsSuccess)
                {
                    var app1 = app1Result.Value;
                    app1.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    app1.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    app1.ChangeStatus(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, "Piotr Zieliński", "Weryfikacja pozytywna");
                    app1.ChangeStatus(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, "Piotr Zieliński");
                    app1.ChangeStatus(AdoptionApplicationTrigger.StawienieSieNaWizyte, "Piotr Zieliński");
                    app1.ChangeStatus(AdoptionApplicationTrigger.PozytywnaOcenaWizyty, "Piotr Zieliński", "Doskonałe warunki dla psa");
                    app1.GenerateContract($"ADOPT/2025/{appNumber++:D3}", "Piotr Zieliński");
                    app1.ChangeStatus(AdoptionApplicationTrigger.PodpisanieUmowy, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(app1);
                }
            }

            // Adoption application for Bianka (completed 30 days ago)
            if (biankaAnimal != null)
            {
                var app2Result = AdoptionApplication.Create(
                    adopter2.Id,
                    biankaAnimal.Id,
                    "Szukam spokojnej kotki do towarzystwa.",
                    "Mam dwa koty od 5 lat.",
                    "Mieszkanie 60m2 z balkonem zabezpieczonym siatką.",
                    "Dwa koty - są towarzyskie i lubią inne koty.",
                    applicationDate: DateTime.UtcNow.AddDays(-30)
                );
                if (app2Result.IsSuccess)
                {
                    var app2 = app2Result.Value;
                    app2.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    app2.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    app2.ChangeStatus(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, "Piotr Zieliński", "Weryfikacja pozytywna");
                    app2.ChangeStatus(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, "Piotr Zieliński");
                    app2.ChangeStatus(AdoptionApplicationTrigger.StawienieSieNaWizyte, "Piotr Zieliński");
                    app2.ChangeStatus(AdoptionApplicationTrigger.PozytywnaOcenaWizyty, "Piotr Zieliński", "Świetne warunki");
                    app2.GenerateContract($"ADOPT/2025/{appNumber++:D3}", "Piotr Zieliński");
                    app2.ChangeStatus(AdoptionApplicationTrigger.PodpisanieUmowy, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(app2);
                }
            }

            // Adoption application for Mruczek (completed 20 days ago)
            if (mruczekAnimal != null)
            {
                var app3Result = AdoptionApplication.Create(
                    adopter3.Id,
                    mruczekAnimal.Id,
                    "Jestem emerytką i szukam spokojnego towarzysza.",
                    "Całe życie miałam koty, ostatni zmarł rok temu.",
                    "Mieszkanie 45m2 na parterze.",
                    "Brak innych zwierząt.",
                    applicationDate: DateTime.UtcNow.AddDays(-20)
                );
                if (app3Result.IsSuccess)
                {
                    var app3 = app3Result.Value;
                    app3.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    app3.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    app3.ChangeStatus(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, "Piotr Zieliński", "Idealna kandydatka");
                    app3.ChangeStatus(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, "Piotr Zieliński");
                    app3.ChangeStatus(AdoptionApplicationTrigger.StawienieSieNaWizyte, "Piotr Zieliński");
                    app3.ChangeStatus(AdoptionApplicationTrigger.PozytywnaOcenaWizyty, "Piotr Zieliński", "Spokojna atmosfera");
                    app3.GenerateContract($"ADOPT/2025/{appNumber++:D3}", "Piotr Zieliński");
                    app3.ChangeStatus(AdoptionApplicationTrigger.PodpisanieUmowy, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(app3);
                }
            }

            // Adoption application for Rudi (completed 10 days ago)
            if (rudiAnimal != null)
            {
                var app4Result = AdoptionApplication.Create(
                    adopter1.Id,
                    rudiAnimal.Id,
                    "Szukamy psa dla naszej rodziny z dwójką dzieci. Mamy dom z ogrodem.",
                    "Mieliśmy psa przez 10 lat, który zmarł rok temu. Dzieci bardzo tęsknią.",
                    "Dom jednorodzinny z dużym ogrodem, ogrodzony płotem 1.8m.",
                    "Brak innych zwierząt. Dzieci w wieku 8 i 12 lat.",
                    applicationDate: DateTime.UtcNow.AddDays(-10)
                );
                if (app4Result.IsSuccess)
                {
                    var app4 = app4Result.Value;
                    app4.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    app4.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    app4.ChangeStatus(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, "Piotr Zieliński", "Rodzina Kamińskich - idealni kandydaci");
                    app4.ChangeStatus(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, "Piotr Zieliński");
                    app4.ChangeStatus(AdoptionApplicationTrigger.StawienieSieNaWizyte, "Piotr Zieliński");
                    app4.ChangeStatus(AdoptionApplicationTrigger.PozytywnaOcenaWizyty, "Piotr Zieliński", "Doskonałe warunki - duży ogród, kochająca rodzina");
                    app4.GenerateContract($"ADOPT/2025/{appNumber++:D3}", "Piotr Zieliński");
                    app4.ChangeStatus(AdoptionApplicationTrigger.PodpisanieUmowy, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(app4);
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} zgłoszeń adopcyjnych dla adoptowanych zwierząt", appNumber - 1);

            // ==================== ADDITIONAL APPLICATIONS IN VARIOUS STATES ====================
            var availableAnimals = await dbContext.Animals
                .Where(a => a.Status == AnimalStatus.Available)
                .ToListAsync();

            // PENDING application - for Luna (submitted 2 days ago)
            var lunaAnimal = availableAnimals.FirstOrDefault(a => a.Name == "Luna");
            if (lunaAnimal != null)
            {
                var pendingAppResult = AdoptionApplication.Create(
                    adopter4.Id,
                    lunaAnimal.Id,
                    "Marzę o labradorze od lat. Mam dużo czasu na spacery.",
                    "Nigdy nie miałem psa, ale dużo czytałem o rasie.",
                    "Mieszkanie 70m2 z dostępem do parku.",
                    "Brak innych zwierząt.",
                    applicationDate: DateTime.UtcNow.AddDays(-2)
                );
                if (pendingAppResult.IsSuccess)
                {
                    var pendingApp = pendingAppResult.Value;
                    pendingApp.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    dbContext.AdoptionApplications.Add(pendingApp);
                }
            }

            // IN REVIEW application - for Bella (submitted 5 days ago)
            var bellaAnimal = availableAnimals.FirstOrDefault(a => a.Name == "Bella");
            if (bellaAnimal != null)
            {
                var inProgressAppResult = AdoptionApplication.Create(
                    adopter2.Id,
                    bellaAnimal.Id,
                    "Husky to mój wymarzony pies. Biegam maratony i szukam towarzysza.",
                    "Miałem husky przez 8 lat, wiem jak wymagająca to rasa.",
                    "Dom z ogrodem 800m2, podwójne ogrodzenie.",
                    "Mam jednego kota, który jest przyzwyczajony do psów.",
                    applicationDate: DateTime.UtcNow.AddDays(-5)
                );
                if (inProgressAppResult.IsSuccess)
                {
                    var inProgressApp = inProgressAppResult.Value;
                    inProgressApp.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    inProgressApp.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(inProgressApp);
                }
            }

            // REJECTED application - for Max (submitted 14 days ago, rejected 10 days ago)
            var maxAnimal = availableAnimals.FirstOrDefault(a => a.Name == "Max");
            if (maxAnimal != null)
            {
                var rejectedAppResult = AdoptionApplication.Create(
                    adopter4.Id,
                    maxAnimal.Id,
                    "Chcę kupić psa dla dziecka na urodziny.",
                    "Nigdy nie miałem psa.",
                    "Mieszkanie 40m2, trzecie piętro bez windy.",
                    "Mam 3 koty i chomika.",
                    applicationDate: DateTime.UtcNow.AddDays(-14)
                );
                if (rejectedAppResult.IsSuccess)
                {
                    var rejectedApp = rejectedAppResult.Value;
                    rejectedApp.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    rejectedApp.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    rejectedApp.ChangeStatus(AdoptionApplicationTrigger.NegatywnaWeryfikacja, "Piotr Zieliński",
                        "Pies jako prezent-niespodzianka, brak doświadczenia z dużymi psami, nieodpowiednie warunki mieszkaniowe dla owczarka niemieckiego");
                    dbContext.AdoptionApplications.Add(rejectedApp);
                }
            }

            // AWAITING VISIT application - for Figa (submitted 7 days ago)
            var figaAnimal = availableAnimals.FirstOrDefault(a => a.Name == "Figa");
            if (figaAnimal != null)
            {
                var visitScheduledAppResult = AdoptionApplication.Create(
                    adopter3.Id,
                    figaAnimal.Id,
                    "Szukam spokojnej kotki do towarzystwa.",
                    "Mam koty od 30 lat, obecnie nie mam żadnego.",
                    "Mieszkanie 55m2, wszystkie okna zabezpieczone siatkami.",
                    "Brak innych zwierząt.",
                    applicationDate: DateTime.UtcNow.AddDays(-7)
                );
                if (visitScheduledAppResult.IsSuccess)
                {
                    var visitApp = visitScheduledAppResult.Value;
                    visitApp.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "System");
                    visitApp.ChangeStatus(AdoptionApplicationTrigger.PodjęciePrzezPracownika, "Piotr Zieliński");
                    visitApp.ChangeStatus(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, "Piotr Zieliński", "Doświadczona opiekunka kotów");
                    visitApp.ChangeStatus(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, "Piotr Zieliński");
                    dbContext.AdoptionApplications.Add(visitApp);
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano dodatkowe zgłoszenia adopcyjne w różnych stanach");
        }
    }

    private static async Task SeedVolunteerAttendanceAsync(
        ShelterDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Check if volunteer already exists
        var existingVolunteer = await dbContext.Volunteers
            .FirstOrDefaultAsync(v => v.Email == "schronisko.dla.zwierzat+anna.nowak@outlook.com");

        if (existingVolunteer != null)
        {
            logger.LogInformation("Wolontariusz już istnieje - pomijam seeding");

            // Check if volunteer already has attendance records
            var hasAttendances = await dbContext.Attendances
                .AnyAsync(a => a.VolunteerId == existingVolunteer.Id);

            if (!hasAttendances && existingVolunteer.Status == VolunteerStatus.Active)
            {
                // Add attendance records for existing volunteer
                await AddAttendanceRecordsAsync(dbContext, existingVolunteer, logger);
            }
            return;
        }

        // Get volunteer user
        var volunteerUser = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+anna.nowak@outlook.com");
        if (volunteerUser == null)
        {
            logger.LogWarning("Nie znaleziono użytkownika wolontariusza - pomijam seeding wolontariusza");
            return;
        }

        // Create volunteer
        var volunteerResult = Volunteer.Create(
            firstName: "Anna",
            lastName: "Nowak",
            email: "schronisko.dla.zwierzat+anna.nowak@outlook.com",
            phone: "+48 222 222 222",
            dateOfBirth: new DateTime(1995, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            address: "ul. Kwiatowa 8",
            city: "Lublin",
            postalCode: "20-100",
            emergencyContactName: "Marek Nowak",
            emergencyContactPhone: "+48 222 333 444",
            skills: new List<string> { "Spacery z psami", "Karmienie zwierząt", "Socjalizacja kotów", "Pielęgnacja sierści" },
            availability: new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Saturday },
            notes: "Doświadczony wolontariusz od 2022 roku, uwielbia pracę ze zwierzętami. Specjalizuje się w socjalizacji nieśmiałych kotów."
        );

        if (volunteerResult.IsFailure)
        {
            logger.LogError("Nie udało się utworzyć wolontariusza: {Error}", volunteerResult.Error.Message);
            return;
        }

        var volunteer = volunteerResult.Value;

        // Link to user and activate volunteer
        volunteer.AssignUser(volunteerUser.Id);

        // Go through the recruitment process
        volunteer.AcceptAndStartTraining("Administrator Systemu", DateTime.UtcNow.AddMonths(-2));
        volunteer.CompleteTraining("Administrator Systemu", $"VOL/{DateTime.UtcNow.Year}/001", DateTime.UtcNow.AddMonths(-1));

        dbContext.Volunteers.Add(volunteer);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Utworzono wolontariusza: {Email}, Status: {Status}", volunteer.Email, volunteer.Status);

        // Add attendance records
        await AddAttendanceRecordsAsync(dbContext, volunteer, logger);

        // ==================== DODATKOWI WOLONTARIUSZE ====================

        // Wolontariusz 2 - Michał Kowalczyk (Active)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+michal.kowalczyk@outlook.com",
            "Michał", "Kowalczyk", "+48 512 345 678",
            new DateTime(1998, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            "ul. Parkowa 15", "Warszawa", "02-100",
            "Janina Kowalczyk", "+48 512 111 222",
            new List<string> { "Spacery z psami", "Karmienie zwierząt" },
            new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday },
            "Wolontariusz od pół roku, specjalizuje się w opiece nad psami.",
            VolunteerStatus.Active, $"VOL/{DateTime.UtcNow.Year}/002");

        // Wolontariusz 3 - Ewa Wiśniewska (InTraining)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+ewa.wisniewska@outlook.com",
            "Ewa", "Wiśniewska", "+48 513 456 789",
            new DateTime(2000, 3, 12, 0, 0, 0, DateTimeKind.Utc),
            "ul. Słoneczna 22", "Kraków", "30-200",
            "Adam Wiśniewski", "+48 513 222 333",
            new List<string> { "Socjalizacja kotów", "Pielęgnacja sierści" },
            new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
            "Studentka weterynarii, w trakcie szkolenia.",
            VolunteerStatus.InTraining, null);

        // Wolontariusz 4 - Tomasz Lewandowski (Candidate)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+tomasz.lewandowski@outlook.com",
            "Tomasz", "Lewandowski", "+48 514 567 890",
            new DateTime(1992, 11, 5, 0, 0, 0, DateTimeKind.Utc),
            "ul. Górna 8", "Gdańsk", "80-100",
            "Barbara Lewandowska", "+48 514 333 444",
            new List<string> { "Transport zwierząt", "Spacery z psami" },
            new List<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Sunday },
            "Nowy kandydat, czeka na zatwierdzenie.",
            VolunteerStatus.Candidate, null);

        // Wolontariusz 5 - Karolina Dąbrowska (Active)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+karolina.dabrowska@outlook.com",
            "Karolina", "Dąbrowska", "+48 515 678 901",
            new DateTime(1997, 6, 18, 0, 0, 0, DateTimeKind.Utc),
            "ul. Leśna 33", "Poznań", "60-300",
            "Robert Dąbrowski", "+48 515 444 555",
            new List<string> { "Socjalizacja kotów", "Karmienie zwierząt", "Sprzątanie" },
            new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            "Wolontariuszka od roku, bardzo zaangażowana w socjalizację kotów.",
            VolunteerStatus.Active, $"VOL/{DateTime.UtcNow.Year}/003");

        // Wolontariusz 6 - Paweł Kaczmarek (Suspended)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+pawel.kaczmarek@outlook.com",
            "Paweł", "Kaczmarek", "+48 516 789 012",
            new DateTime(1994, 9, 25, 0, 0, 0, DateTimeKind.Utc),
            "ul. Krótka 5", "Wrocław", "50-400",
            "Teresa Kaczmarek", "+48 516 555 666",
            new List<string> { "Spacery z psami" },
            new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            "Zawieszony z powodu nieobecności - powrót planowany za miesiąc.",
            VolunteerStatus.Suspended, $"VOL/{DateTime.UtcNow.Year}/004");

        // Wolontariusz 7 - Magdalena Szymańska (Candidate)
        await CreateAdditionalVolunteerAsync(dbContext, userManager, logger,
            "schronisko.dla.zwierzat+magdalena.szymanska@outlook.com",
            "Magdalena", "Szymańska", "+48 517 890 123",
            new DateTime(2001, 1, 30, 0, 0, 0, DateTimeKind.Utc),
            "ul. Cicha 12", "Łódź", "90-500",
            "Krzysztof Szymański", "+48 517 666 777",
            new List<string> { "Pielęgnacja sierści", "Socjalizacja szczeniąt" },
            new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday },
            "Nowa kandydatka, studentka z pasją do zwierząt.",
            VolunteerStatus.Candidate, null);
    }

    private static async Task CreateAdditionalVolunteerAsync(
        ShelterDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string firstName,
        string lastName,
        string phone,
        DateTime dateOfBirth,
        string address,
        string city,
        string postalCode,
        string emergencyContactName,
        string emergencyContactPhone,
        List<string> skills,
        List<DayOfWeek> availability,
        string notes,
        VolunteerStatus targetStatus,
        string? agreementNumber)
    {
        // Check if volunteer already exists
        var existingVolunteer = await dbContext.Volunteers.FirstOrDefaultAsync(v => v.Email == email);
        if (existingVolunteer != null)
        {
            logger.LogInformation("Wolontariusz {Email} już istnieje - pomijam", email);
            return;
        }

        // Get user
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("Nie znaleziono użytkownika {Email} - pomijam", email);
            return;
        }

        // Create volunteer
        var volunteerResult = Volunteer.Create(
            firstName: firstName,
            lastName: lastName,
            email: email,
            phone: phone,
            dateOfBirth: dateOfBirth,
            address: address,
            city: city,
            postalCode: postalCode,
            emergencyContactName: emergencyContactName,
            emergencyContactPhone: emergencyContactPhone,
            skills: skills,
            availability: availability,
            notes: notes
        );

        if (volunteerResult.IsFailure)
        {
            logger.LogError("Nie udało się utworzyć wolontariusza {Email}: {Error}", email, volunteerResult.Error.Message);
            return;
        }

        var volunteer = volunteerResult.Value;
        volunteer.AssignUser(user.Id);

        // Progress through states based on target status
        if (targetStatus == VolunteerStatus.InTraining || targetStatus == VolunteerStatus.Active || targetStatus == VolunteerStatus.Suspended)
        {
            volunteer.AcceptAndStartTraining("Administrator Systemu", DateTime.UtcNow.AddMonths(-2));
        }

        if (targetStatus == VolunteerStatus.Active || targetStatus == VolunteerStatus.Suspended)
        {
            volunteer.CompleteTraining("Administrator Systemu", agreementNumber!, DateTime.UtcNow.AddMonths(-1));
        }

        if (targetStatus == VolunteerStatus.Suspended)
        {
            volunteer.Suspend("Administrator Systemu", "Czasowa przerwa");
        }

        dbContext.Volunteers.Add(volunteer);
        await dbContext.SaveChangesAsync();

        // Add some attendance for active volunteers
        if (targetStatus == VolunteerStatus.Active)
        {
            await AddAttendanceRecordsAsync(dbContext, volunteer, logger, 5);
        }

        logger.LogInformation("Utworzono wolontariusza: {Email}, Status: {Status}", email, volunteer.Status);
    }

    private static async Task AddAttendanceRecordsAsync(
        ShelterDbContext dbContext,
        Volunteer volunteer,
        ILogger logger,
        int recordCount = 10)
    {
        // Find staff user for attendance approval
        var staffUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "schronisko.dla.zwierzat+piotr.zielinski@outlook.com");
        var staffUserId = staffUser?.Id ?? Guid.Empty;

        var attendances = new List<Attendance>();
        var workDescriptions = new[]
        {
            "Spacery z psami Burkiem i Luną, karmienie kotów",
            "Socjalizacja nowych kotów, sprzątanie boksów",
            "Spacer grupowy z 4 psami, zabawa na wybiegu",
            "Karmienie wszystkich zwierząt, mycie misek",
            "Szczotkowanie kotów długowłosych, wyprowadzanie psów",
            "Pomoc przy przyjęciu nowego psa, spacery",
            "Zabawa z kotami, socjalizacja szczeniąt",
            "Spacery poranne, pomoc przy kąpieli psa",
            "Karmienie i pojenie zwierząt, sprzątanie",
            "Spacer z psami seniorem, pielęgnacja kotów"
        };

        // Create attendance records from the last 30 days
        for (int i = 0; i < recordCount; i++)
        {
            var daysAgo = (i + 1) * 3; // every 3 days, starting from 3 days ago
            var date = DateTime.UtcNow.AddDays(-daysAgo).Date;

            // Random start hours (9:00 - 10:00)
            var startHour = 9 + (i % 2);
            var checkInTime = date.AddHours(startHour);

            // Work 3-5 hours
            var workHours = 3 + (i % 3);
            var checkOutTime = checkInTime.AddHours(workHours);

            var attendanceResult = Attendance.CheckIn(
                volunteerId: volunteer.Id,
                scheduleSlotId: null,
                notes: $"Dzień wolontariatu #{recordCount - i}",
                checkInTime: checkInTime
            );

            if (attendanceResult.IsSuccess)
            {
                var attendance = attendanceResult.Value;
                attendance.CheckOut(workDescriptions[i % workDescriptions.Length], checkOutTime);

                // Approve older records (except 2 newest)
                if (i >= 2 && staffUserId != Guid.Empty)
                {
                    attendance.Approve(staffUserId);
                }

                attendances.Add(attendance);
            }
        }

        if (attendances.Any())
        {
            dbContext.Attendances.AddRange(attendances);

            // Update volunteer's total hours
            var totalHours = attendances.Sum(a => a.HoursWorked ?? 0);
            volunteer.AddWorkHours(totalHours, "System (seed data)");

            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Dodano {Count} wpisów obecności dla wolontariusza. Łącznie godzin: {Hours:F1}h",
                attendances.Count, totalHours);
        }
    }

    private static async Task SeedVolunteerScheduleAsync(
        ShelterDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Check if schedule already exists
        if (await dbContext.ScheduleSlots.AnyAsync())
        {
            logger.LogInformation("Grafik dyżurów już istnieje - pomijam seeding");
            return;
        }

        // Get all active volunteers
        var activeVolunteers = await dbContext.Volunteers
            .Where(v => v.Status == VolunteerStatus.Active)
            .ToListAsync();

        if (!activeVolunteers.Any())
        {
            logger.LogWarning("Brak aktywnych wolontariuszy - pomijam seeding grafiku");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync("schronisko.dla.zwierzat+krzystof.dabrowski@outlook.com");
        if (adminUser == null) return;

        var adminId = Guid.Parse(adminUser.Id.ToString());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get first volunteer for backward compatibility
        var volunteer = activeVolunteers.First();

        var dutyDescriptions = new[]
        {
            "Poranne spacery z psami",
            "Karmienie zwierząt",
            "Socjalizacja kotów",
            "Sprzątanie boksów",
            "Spacery popołudniowe",
            "Pielęgnacja zwierząt"
        };

        // --- PAST DUTIES (completed) ---
        for (int i = 14; i >= 1; i -= 3)
        {
            var date = today.AddDays(-i);
            if (date.DayOfWeek == DayOfWeek.Sunday) continue;

            var descIndex = (i / 3) % dutyDescriptions.Length;
            var slotResult = ScheduleSlot.Create(
                date,
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                maxVolunteers: 3,
                dutyDescriptions[descIndex],
                adminId);

            if (slotResult.IsFailure) continue;
            var slot = slotResult.Value;

            var assignment = slot.AssignVolunteer(volunteer.Id, adminId);
            if (assignment.IsSuccess)
                assignment.Value.Confirm();

            dbContext.ScheduleSlots.Add(slot);
        }

        // --- UPCOMING DUTIES (future) ---

        // Duty tomorrow - confirmed
        var tomorrow = today.AddDays(1);
        if (tomorrow.DayOfWeek == DayOfWeek.Sunday) tomorrow = tomorrow.AddDays(1);
        var tomorrowSlot = ScheduleSlot.Create(
            tomorrow, new TimeOnly(8, 0), new TimeOnly(12, 0),
            maxVolunteers: 3, "Poranne spacery z psami", adminId);
        if (tomorrowSlot.IsSuccess)
        {
            var a = tomorrowSlot.Value.AssignVolunteer(volunteer.Id, adminId);
            if (a.IsSuccess) a.Value.Confirm();
            dbContext.ScheduleSlots.Add(tomorrowSlot.Value);
        }

        // Duty in 3 days - confirmed
        var in3days = today.AddDays(3);
        if (in3days.DayOfWeek == DayOfWeek.Sunday) in3days = in3days.AddDays(1);
        var slot3 = ScheduleSlot.Create(
            in3days, new TimeOnly(13, 0), new TimeOnly(17, 0),
            maxVolunteers: 2, "Socjalizacja kotów", adminId);
        if (slot3.IsSuccess)
        {
            var a = slot3.Value.AssignVolunteer(volunteer.Id, adminId);
            if (a.IsSuccess) a.Value.Confirm();
            dbContext.ScheduleSlots.Add(slot3.Value);
        }

        // Duty in 5 days - awaiting confirmation
        var in5days = today.AddDays(5);
        if (in5days.DayOfWeek == DayOfWeek.Sunday) in5days = in5days.AddDays(1);
        var slot5 = ScheduleSlot.Create(
            in5days, new TimeOnly(8, 0), new TimeOnly(12, 0),
            maxVolunteers: 3, "Karmienie zwierząt", adminId);
        if (slot5.IsSuccess)
        {
            slot5.Value.AssignVolunteer(volunteer.Id, adminId);
            dbContext.ScheduleSlots.Add(slot5.Value);
        }

        // Duty in 7 days - awaiting confirmation
        var in7days = today.AddDays(7);
        if (in7days.DayOfWeek == DayOfWeek.Sunday) in7days = in7days.AddDays(1);
        var slot7 = ScheduleSlot.Create(
            in7days, new TimeOnly(9, 0), new TimeOnly(13, 0),
            maxVolunteers: 2, "Sprzątanie boksów", adminId);
        if (slot7.IsSuccess)
        {
            slot7.Value.AssignVolunteer(volunteer.Id, adminId);
            dbContext.ScheduleSlots.Add(slot7.Value);
        }

        // Duty in 10 days - confirmed
        var in10days = today.AddDays(10);
        if (in10days.DayOfWeek == DayOfWeek.Sunday) in10days = in10days.AddDays(1);
        var slot10 = ScheduleSlot.Create(
            in10days, new TimeOnly(8, 0), new TimeOnly(12, 0),
            maxVolunteers: 3, "Spacery popołudniowe", adminId);
        if (slot10.IsSuccess)
        {
            var a = slot10.Value.AssignVolunteer(volunteer.Id, adminId);
            if (a.IsSuccess) a.Value.Confirm();
            dbContext.ScheduleSlots.Add(slot10.Value);
        }

        // Duty in 14 days - awaiting
        var in14days = today.AddDays(14);
        if (in14days.DayOfWeek == DayOfWeek.Sunday) in14days = in14days.AddDays(1);
        var slot14 = ScheduleSlot.Create(
            in14days, new TimeOnly(13, 0), new TimeOnly(17, 0),
            maxVolunteers: 2, "Pielęgnacja zwierząt", adminId);
        if (slot14.IsSuccess)
        {
            slot14.Value.AssignVolunteer(volunteer.Id, adminId);
            dbContext.ScheduleSlots.Add(slot14.Value);
        }

        // Add slots with multiple volunteers if we have more than one active volunteer
        if (activeVolunteers.Count > 1)
        {
            var in4days = today.AddDays(4);
            if (in4days.DayOfWeek == DayOfWeek.Sunday) in4days = in4days.AddDays(1);
            var multiSlot1 = ScheduleSlot.Create(
                in4days, new TimeOnly(8, 0), new TimeOnly(14, 0),
                maxVolunteers: 4, "Sprzątanie całego schroniska", adminId);
            if (multiSlot1.IsSuccess)
            {
                foreach (var vol in activeVolunteers.Take(3))
                {
                    var a = multiSlot1.Value.AssignVolunteer(vol.Id, adminId);
                    if (a.IsSuccess) a.Value.Confirm();
                }
                dbContext.ScheduleSlots.Add(multiSlot1.Value);
            }

            var in8days = today.AddDays(8);
            if (in8days.DayOfWeek == DayOfWeek.Sunday) in8days = in8days.AddDays(1);
            var multiSlot2 = ScheduleSlot.Create(
                in8days, new TimeOnly(9, 0), new TimeOnly(15, 0),
                maxVolunteers: 5, "Dzień adopcyjny - pomoc przy wizytach", adminId);
            if (multiSlot2.IsSuccess)
            {
                foreach (var vol in activeVolunteers)
                {
                    var a = multiSlot2.Value.AssignVolunteer(vol.Id, adminId);
                    if (a.IsSuccess) a.Value.Confirm();
                }
                dbContext.ScheduleSlots.Add(multiSlot2.Value);
            }

            var in12days = today.AddDays(12);
            if (in12days.DayOfWeek == DayOfWeek.Sunday) in12days = in12days.AddDays(1);
            var multiSlot3 = ScheduleSlot.Create(
                in12days, new TimeOnly(10, 0), new TimeOnly(16, 0),
                maxVolunteers: 3, "Kąpiel psów", adminId);
            if (multiSlot3.IsSuccess)
            {
                foreach (var vol in activeVolunteers.Take(2))
                {
                    multiSlot3.Value.AssignVolunteer(vol.Id, adminId);
                }
                dbContext.ScheduleSlots.Add(multiSlot3.Value);
            }
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Dodano grafik dyżurów wolontariackich (przeszłe + nadchodzące) dla {Count} wolontariuszy", activeVolunteers.Count);
    }

    private static async Task SeedVisitSlotsAsync(ShelterDbContext dbContext, ILogger logger)
    {
        // Check if visit slots already exist
        var existingSlots = await dbContext.VisitSlots.AnyAsync();
        if (existingSlots)
        {
            logger.LogInformation("Sloty wizyt już istnieją - pomijam seeding");
            return;
        }

        var slots = new List<VisitSlot>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create slots for the next 4 weeks (28 days)
        for (int dayOffset = 1; dayOffset <= 28; dayOffset++)
        {
            var date = today.AddDays(dayOffset);

            // Skip Sundays
            if (date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            // Saturdays - morning only
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                // Morning slot 9:00 - 10:00
                var satMorningResult = VisitSlot.Create(
                    date,
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    maxCapacity: 3,
                    notes: "Sobotni slot poranny"
                );
                if (satMorningResult.IsSuccess)
                    slots.Add(satMorningResult.Value);

                // Slot 10:00 - 11:00
                var satMorning2Result = VisitSlot.Create(
                    date,
                    new TimeOnly(10, 0),
                    new TimeOnly(11, 0),
                    maxCapacity: 3,
                    notes: "Sobotni slot poranny"
                );
                if (satMorning2Result.IsSuccess)
                    slots.Add(satMorning2Result.Value);

                // Slot 11:00 - 12:00
                var satMorning3Result = VisitSlot.Create(
                    date,
                    new TimeOnly(11, 0),
                    new TimeOnly(12, 0),
                    maxCapacity: 3,
                    notes: "Sobotni slot poranny"
                );
                if (satMorning3Result.IsSuccess)
                    slots.Add(satMorning3Result.Value);

                continue;
            }

            // Weekdays - full schedule
            var timeSlots = new[]
            {
                (new TimeOnly(9, 0), new TimeOnly(10, 0)),
                (new TimeOnly(10, 0), new TimeOnly(11, 0)),
                (new TimeOnly(11, 0), new TimeOnly(12, 0)),
                (new TimeOnly(13, 0), new TimeOnly(14, 0)),
                (new TimeOnly(14, 0), new TimeOnly(15, 0)),
                (new TimeOnly(15, 0), new TimeOnly(16, 0)),
            };

            foreach (var (startTime, endTime) in timeSlots)
            {
                var slotResult = VisitSlot.Create(
                    date,
                    startTime,
                    endTime,
                    maxCapacity: 2,
                    notes: null
                );
                if (slotResult.IsSuccess)
                    slots.Add(slotResult.Value);
            }
        }

        if (slots.Any())
        {
            dbContext.VisitSlots.AddRange(slots);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} slotów wizyt adopcyjnych na następne 4 tygodnie", slots.Count);
        }
    }

    private static async Task SeedCmsContentAsync(ShelterDbContext dbContext, ILogger logger)
    {
        // Check if CMS content already exists
        if (await dbContext.BlogPosts.AnyAsync() || await dbContext.FaqItems.AnyAsync() || await dbContext.ContentPages.AnyAsync())
        {
            logger.LogInformation("Treści CMS już istnieją - pomijam seeding");
            return;
        }

        // ==================== BLOG POSTS ====================

        var blogPosts = new List<BlogPost>();

        // Post 1 - Adoption guide
        var post1Result = BlogPost.Create(
            title: "Jak przygotować się do adopcji psa ze schroniska?",
            content: @"Adopcja psa ze schroniska to piękna decyzja, która wymaga odpowiedniego przygotowania. Oto kilka kroków, które pomogą Ci się przygotować:

## 1. Oceń swoje możliwości

Przed adopcją zastanów się, czy masz wystarczająco dużo czasu, przestrzeni i środków finansowych na opiekę nad psem. Psy wymagają regularnych spacerów, wizyt u weterynarza i odpowiedniego żywienia.

## 2. Przygotuj dom

- Zabezpiecz niebezpieczne przedmioty i substancje
- Przygotuj legowisko w spokojnym miejscu
- Kup podstawowe akcesoria: miskę na wodę i jedzenie, smycz, obrożę, zabawki

## 3. Poznaj psa przed adopcją

Odwiedź schronisko kilka razy, aby poznać charakter psa. Porozmawiaj z opiekunami o jego zachowaniu, przeszłości i potrzebach.

## 4. Daj psu czas na adaptację

Pamiętaj, że pies potrzebuje czasu na przyzwyczajenie się do nowego domu. Bądź cierpliwy i konsekwentny w ustalaniu zasad.

Adopcja to początek pięknej przyjaźni. Życzymy powodzenia!",
            excerpt: "Praktyczny poradnik dla osób planujących adopcję psa ze schroniska. Dowiedz się, jak przygotować się i dom na nowego członka rodziny.",
            author: "Krzysztof Dąbrowski",
            category: BlogCategory.Adopcja,
            imageUrl: "https://images.unsplash.com/photo-1601758228041-f3b2795255f1?w=800&h=400&fit=crop"
        );
        if (post1Result.IsSuccess)
        {
            post1Result.Value.Publish();
            blogPosts.Add(post1Result.Value);
        }

        // Post 2 - Cat care
        var post2Result = BlogPost.Create(
            title: "Podstawy opieki nad kotem - poradnik dla początkujących",
            content: @"Koty to wspaniałe zwierzęta domowe, ale wymagają odpowiedniej opieki. Oto podstawowe informacje dla nowych opiekunów.

## Żywienie

Koty są mięsożercami obligatoryjnymi. Potrzebują diety bogatej w białko zwierzęce. Wybieraj wysokiej jakości karmę dla kotów i zawsze zapewniaj dostęp do świeżej wody.

## Kuweta

Kuweta powinna znajdować się w spokojnym, łatwo dostępnym miejscu. Czyść ją codziennie i wymieniaj żwirek co 1-2 tygodnie.

## Zdrowie

- Regularne wizyty u weterynarza (minimum raz w roku)
- Szczepienia i odrobaczanie zgodnie z zaleceniami
- Sterylizacja/kastracja - zapobiega wielu problemom zdrowotnym

## Zabawa i aktywność

Koty potrzebują stymulacji umysłowej i fizycznej. Zapewnij:
- Drapak (chroni meble!)
- Zabawki interaktywne
- Miejsca do wspinania się

Pamiętaj - szczęśliwy kot to zdrowy kot!",
            excerpt: "Wszystko, co musisz wiedzieć o opiece nad kotem. Od żywienia po zdrowie - kompletny poradnik dla początkujących.",
            author: "Piotr Zieliński",
            category: BlogCategory.Porady,
            imageUrl: "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=800&h=400&fit=crop"
        );
        if (post2Result.IsSuccess)
        {
            post2Result.Value.Publish();
            blogPosts.Add(post2Result.Value);
        }

        // Post 3 - Success story
        var post3Result = BlogPost.Create(
            title: "Historia Burka - z ulicy do kochającego domu",
            content: @"Burek trafił do naszego schroniska zimą 2023 roku. Znaleziony na ulicy, wychudzony i przestraszony, nie ufał ludziom. Dziś jest szczęśliwym psem w kochającej rodzinie.

## Pierwsze dni w schronisku

Burek był bardzo nieufny. Chował się w kącie boksu i nie chciał wychodzić na spacery. Nasi wolontariusze cierpliwie pracowali z nim każdego dnia.

## Przełom

Po trzech tygodniach Burek zaczął wychodzić do opiekunów. Okazało się, że to niezwykle łagodny i towarzyski pies, który po prostu potrzebował czasu.

## Nowy dom

W marcu 2024 roku Burek poznał rodzinę Kowalskich. To była miłość od pierwszego wejrzenia! Dziś biega po ogrodzie, bawi się z dziećmi i jest ukochanym członkiem rodziny.

*'Burek zmienił nasze życie. Nie wyobrażamy sobie domu bez niego'* - mówi pani Kowalska.

Takie historie motywują nas do dalszej pracy!",
            excerpt: "Wzruszająca historia Burka, który z przestraszonego psa ulicznego stał się kochanym członkiem rodziny. Dowód na to, że każdy pies zasługuje na drugą szansę.",
            author: "Anna Nowak",
            category: BlogCategory.Historie,
            imageUrl: "https://images.unsplash.com/photo-1587300003388-59208cc962cb?w=800&h=400&fit=crop"
        );
        if (post3Result.IsSuccess)
        {
            post3Result.Value.Publish();
            blogPosts.Add(post3Result.Value);
        }

        // Post 4 - Health tips
        var post4Result = BlogPost.Create(
            title: "Szczepienia zwierząt - dlaczego są tak ważne?",
            content: @"Szczepienia to jeden z najważniejszych elementów profilaktyki zdrowotnej zwierząt. Chronią przed groźnymi chorobami i ratują życie.

## Szczepienia psów

Podstawowe szczepienia psów obejmują:
- **Nosówka** - groźna choroba wirusowa
- **Parwowiroza** - szczególnie niebezpieczna dla szczeniąt
- **Leptospiroza** - może przenosić się na ludzi
- **Wścieklizna** - obowiązkowe szczepienie w Polsce

## Szczepienia kotów

Koty powinny być szczepione przeciwko:
- **Panleukopenia** (koci tyfus)
- **Kaliciwiroza**
- **Herpesiwiroza** (katar koci)
- **Wścieklizna** - szczególnie dla kotów wychodzących

## Harmonogram szczepień

- Szczenięta/kocięta: od 6-8 tygodnia życia
- Szczepienia przypominające: co 12 miesięcy
- Wścieklizna: co 1-3 lata (zgodnie z zaleceniami)

Pamiętaj - szczepienia chronią nie tylko Twoje zwierzę, ale też inne zwierzęta i ludzi!",
            excerpt: "Dowiedz się, dlaczego regularne szczepienia są kluczowe dla zdrowia Twojego pupila i jakie szczepionki są zalecane dla psów i kotów.",
            author: "lek. wet. Jan Kowalski",
            category: BlogCategory.Zdrowie,
            imageUrl: "https://images.unsplash.com/photo-1628009368231-7bb7cfcb0def?w=800&h=400&fit=crop"
        );
        if (post4Result.IsSuccess)
        {
            post4Result.Value.Publish();
            blogPosts.Add(post4Result.Value);
        }

        // Post 5 - News/Events
        var post5Result = BlogPost.Create(
            title: "Dzień Otwarty Schroniska - zapraszamy w sobotę!",
            content: @"Serdecznie zapraszamy na Dzień Otwarty Schroniska! To doskonała okazja, aby poznać nasze zwierzęta i dowiedzieć się więcej o adopcji.

## Kiedy i gdzie?

**Data:** Najbliższa sobota, godz. 10:00-16:00
**Miejsce:** Schronisko dla Zwierząt, ul. Przytulna 1

## Program

- **10:00** - Otwarcie, powitanie gości
- **10:30** - Spacer z psami dla chętnych
- **12:00** - Pokaz pielęgnacji zwierząt
- **13:00** - Przerwa na poczęstunek
- **14:00** - Spotkanie z wolontariuszami
- **15:00** - Konkurs wiedzy o zwierzętach dla dzieci
- **16:00** - Zakończenie

## Co możesz zrobić?

- Poznać zwierzęta czekające na dom
- Porozmawiać z opiekunami
- Zostać wolontariuszem
- Wesprzeć schronisko darowizną
- Adoptować nowego przyjaciela!

Do zobaczenia!",
            excerpt: "Zapraszamy na Dzień Otwarty Schroniska! Poznaj nasze zwierzęta, porozmawiaj z opiekunami i może znajdziesz swojego nowego przyjaciela.",
            author: "Krzysztof Dąbrowski",
            category: BlogCategory.Wydarzenia,
            imageUrl: "https://images.unsplash.com/photo-1548199973-03cce0bbc87b?w=800&h=400&fit=crop"
        );
        if (post5Result.IsSuccess)
        {
            post5Result.Value.Publish();
            blogPosts.Add(post5Result.Value);
        }

        if (blogPosts.Any())
        {
            dbContext.BlogPosts.AddRange(blogPosts);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} wpisów blogowych", blogPosts.Count);
        }

        // ==================== FAQ ITEMS ====================

        var faqItems = new List<FaqItem>();

        // Adoption FAQs
        var faq1 = FaqItem.Create(
            "Jak wygląda proces adopcji?",
            "Proces adopcji składa się z kilku kroków: 1) Wypełnienie formularza adopcyjnego online lub w schronisku, 2) Rozmowa z pracownikiem schroniska, 3) Wizyta w schronisku i poznanie zwierzęcia, 4) Weryfikacja warunków mieszkaniowych (opcjonalnie), 5) Podpisanie umowy adopcyjnej i odebranie zwierzęcia.",
            FaqCategory.Adopcja,
            displayOrder: 1
        );
        if (faq1.IsSuccess) faqItems.Add(faq1.Value);

        var faq2 = FaqItem.Create(
            "Czy adopcja jest płatna?",
            "Adopcja w naszym schronisku jest bezpłatna. Prosimy jednak o dobrowolną darowiznę, która pomoże nam w opiece nad innymi zwierzętami. Wszystkie zwierzęta są zaszczepione, odrobaczone i zaczipowane przed adopcją.",
            FaqCategory.Adopcja,
            displayOrder: 2
        );
        if (faq2.IsSuccess) faqItems.Add(faq2.Value);

        var faq3 = FaqItem.Create(
            "Czy mogę zwrócić adoptowane zwierzę?",
            "Tak, jeśli z ważnych powodów nie możesz dalej opiekować się zwierzęciem, przyjmiemy je z powrotem. Prosimy jednak o wcześniejszy kontakt. Pamiętaj, że adopcja to poważna decyzja i zobowiązanie na wiele lat.",
            FaqCategory.Adopcja,
            displayOrder: 3
        );
        if (faq3.IsSuccess) faqItems.Add(faq3.Value);

        // Pet care FAQs
        var faq4 = FaqItem.Create(
            "Jak często powinienem szczepić zwierzę?",
            "Podstawowe szczepienia powinny być podawane co roku. Szczenięta i kocięta wymagają serii szczepień od 6-8 tygodnia życia. Szczepienie przeciw wściekliźnie jest obowiązkowe. Szczegółowy harmonogram ustali weterynarz.",
            FaqCategory.OpikaZwierzat,
            displayOrder: 1
        );
        if (faq4.IsSuccess) faqItems.Add(faq4.Value);

        var faq5 = FaqItem.Create(
            "Czym karmić adoptowanego psa/kota?",
            "Zalecamy wysokiej jakości karmę dostosowaną do wieku, wielkości i potrzeb zwierzęcia. Przy odbiorze otrzymasz informację o aktualnej diecie. Zmianę karmy wprowadzaj stopniowo przez 7-10 dni, mieszając starą z nową.",
            FaqCategory.OpikaZwierzat,
            displayOrder: 2
        );
        if (faq5.IsSuccess) faqItems.Add(faq5.Value);

        // Volunteering FAQs
        var faq6 = FaqItem.Create(
            "Jak zostać wolontariuszem?",
            "Aby zostać wolontariuszem, wypełnij formularz zgłoszeniowy na naszej stronie. Po akceptacji zgłoszenia zaprosimy Cię na krótkie szkolenie wprowadzające. Wolontariusze muszą mieć ukończone 18 lat (lub 16 lat za zgodą rodzica).",
            FaqCategory.Wolontariat,
            displayOrder: 1
        );
        if (faq6.IsSuccess) faqItems.Add(faq6.Value);

        var faq7 = FaqItem.Create(
            "Ile czasu muszę poświęcić jako wolontariusz?",
            "Minimalny wymiar to 4 godziny tygodniowo. Możesz wybrać dni i godziny, które Ci odpowiadają. Rozumiemy, że życie bywa nieprzewidywalne - wystarczy nas uprzedzić, jeśli nie możesz przyjść.",
            FaqCategory.Wolontariat,
            displayOrder: 2
        );
        if (faq7.IsSuccess) faqItems.Add(faq7.Value);

        // Donations FAQs
        var faq8 = FaqItem.Create(
            "Jak mogę wesprzeć schronisko?",
            "Możesz nas wesprzeć na kilka sposobów: 1) Darowizna pieniężna (przelew lub gotówka), 2) Darowizna rzeczowa (karma, koce, zabawki), 3) Wolontariat, 4) Przekazanie 1.5% podatku, 5) Udostępnianie informacji o zwierzętach do adopcji.",
            FaqCategory.Darowizny,
            displayOrder: 1
        );
        if (faq8.IsSuccess) faqItems.Add(faq8.Value);

        var faq9 = FaqItem.Create(
            "Jakie rzeczy są najbardziej potrzebne?",
            "Najbardziej potrzebujemy: karmy dla psów i kotów, kocyków i ręczników, środków czystości, worków na śmieci, zabawek dla zwierząt, smyczy i obroży, transporterów. Aktualna lista potrzeb jest dostępna na naszej stronie.",
            FaqCategory.Darowizny,
            displayOrder: 2
        );
        if (faq9.IsSuccess) faqItems.Add(faq9.Value);

        // Contact FAQs
        var faq10 = FaqItem.Create(
            "Jakie są godziny otwarcia schroniska?",
            "Schronisko jest otwarte dla odwiedzających: Poniedziałek-Piątek: 9:00-16:00, Sobota: 9:00-14:00, Niedziela: nieczynne. Wizyty adopcyjne najlepiej umawiać wcześniej przez nasz system rezerwacji online.",
            FaqCategory.Kontakt,
            displayOrder: 1
        );
        if (faq10.IsSuccess) faqItems.Add(faq10.Value);

        var faq11 = FaqItem.Create(
            "Jak skontaktować się ze schroniskiem?",
            "Możesz się z nami skontaktować: Telefon: +48 123 456 789 (pn-pt 9:00-16:00), Email: kontakt@schronisko.pl, Formularz kontaktowy na stronie. W nagłych przypadkach (znalezione/ranne zwierzę) dzwoń pod numer alarmowy: +48 123 456 780.",
            FaqCategory.Kontakt,
            displayOrder: 2
        );
        if (faq11.IsSuccess) faqItems.Add(faq11.Value);

        // Adoption procedure FAQs
        var faq12 = FaqItem.Create(
            "Jakie dokumenty są potrzebne do adopcji?",
            "Do adopcji potrzebujesz: dowodu osobistego lub innego dokumentu tożsamości, potwierdzenia adresu zamieszkania (np. rachunek za media). W przypadku wynajmowania mieszkania - zgody właściciela na posiadanie zwierzęcia.",
            FaqCategory.ProceduraAdopcji,
            displayOrder: 1
        );
        if (faq12.IsSuccess) faqItems.Add(faq12.Value);

        var faq13 = FaqItem.Create(
            "Czy mogę zarezerwować zwierzę przed wizytą?",
            "Nie rezerwujemy zwierząt przed osobistą wizytą. Musisz poznać zwierzę i upewnić się, że jest odpowiednie dla Ciebie. Po pozytywnej wizycie i złożeniu wniosku adopcyjnego, zwierzę zostaje zarezerwowane na Twoje nazwisko.",
            FaqCategory.ProceduraAdopcji,
            displayOrder: 2
        );
        if (faq13.IsSuccess) faqItems.Add(faq13.Value);

        if (faqItems.Any())
        {
            dbContext.FaqItems.AddRange(faqItems);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} elementów FAQ", faqItems.Count);
        }

        // ==================== CONTENT PAGES ====================

        var contentPages = new List<ContentPage>();

        // O nas
        var aboutPage = ContentPage.Create(
            title: "O nas",
            slug: "o-nas",
            content: @"# O Schronisku dla Zwierząt w Białej Podlaskiej

## Nasza misja

Schronisko dla Zwierząt w Białej Podlaskiej to miejsce, gdzie bezdomne psy i koty znajdują tymczasowy dom w oczekiwaniu na nowych, kochających właścicieli. Naszą misją jest zapewnienie zwierzętom godnych warunków życia, opieki weterynaryjnej oraz znalezienie im nowych, odpowiedzialnych opiekunów.

## Historia

Schronisko zostało założone w 2005 roku z inicjatywy lokalnych miłośników zwierząt. Od tego czasu pomogliśmy tysiącom zwierząt znaleźć nowy dom. Obecnie opiekujemy się średnio 150 zwierzętami - psami i kotami różnych ras i w różnym wieku.

## Nasz zespół

W schronisku pracuje zespół oddanych opiekunów zwierząt, którzy dbają o codzienne potrzeby naszych podopiecznych. Wspierają nas również wolontariusze, bez których nasza praca byłaby niemożliwa.

## Czym się zajmujemy

- **Opieka nad bezdomnymi zwierzętami** - zapewniamy schronienie, pożywienie i opiekę weterynaryjną
- **Adopcje** - szukamy odpowiedzialnych opiekunów dla naszych podopiecznych
- **Sterylizacja i kastracja** - prowadzimy program kontroli populacji
- **Edukacja** - uczymy odpowiedzialnej opieki nad zwierzętami
- **Interwencje** - reagujemy na zgłoszenia o zwierzętach w potrzebie

## Współpraca

Współpracujemy z lokalnymi władzami, organizacjami prozwierzęcymi oraz klinikami weterynaryjnymi, aby zapewnić jak najlepszą opiekę naszym podopiecznym.

Zapraszamy do odwiedzenia naszego schroniska i poznania naszych zwierząt!",
            metaDescription: "Schronisko dla Zwierząt w Białej Podlaskiej - dowiedz się więcej o naszej misji i działalności",
            metaKeywords: "schronisko, zwierzęta, adopcja, Biała Podlaska, psy, koty"
        );
        if (aboutPage.IsSuccess)
        {
            aboutPage.Value.Publish();
            contentPages.Add(aboutPage.Value);
        }

        // Regulamin adopcji
        var adoptionRulesPage = ContentPage.Create(
            title: "Regulamin adopcji",
            slug: "regulamin-adopcji",
            content: @"# Regulamin adopcji zwierząt ze schroniska

## § 1. Postanowienia ogólne

1. Niniejszy regulamin określa zasady adopcji zwierząt ze Schroniska dla Zwierząt w Białej Podlaskiej.
2. Adopcja zwierząt jest bezpłatna.
3. Wszystkie zwierzęta przed adopcją są zaszczepione, odrobaczone i zaczipowane.

## § 2. Warunki adopcji

1. Adoptować zwierzę może osoba pełnoletnia, posiadająca pełną zdolność do czynności prawnych.
2. Adoptujący musi przedstawić dokument tożsamości.
3. W przypadku mieszkania wynajmowanego - wymagana jest zgoda właściciela na posiadanie zwierzęcia.
4. Adoptujący musi zapewnić zwierzęciu odpowiednie warunki bytowe.

## § 3. Procedura adopcji

1. Wypełnienie formularza adopcyjnego.
2. Rozmowa z pracownikiem schroniska.
3. Wizyta w schronisku i poznanie zwierzęcia.
4. Weryfikacja warunków mieszkaniowych (opcjonalnie).
5. Podpisanie umowy adopcyjnej.
6. Odebranie zwierzęcia.

## § 4. Obowiązki adoptującego

1. Zapewnienie zwierzęciu odpowiednich warunków bytowych.
2. Zapewnienie regularnej opieki weterynaryjnej.
3. Humanitarne traktowanie zwierzęcia.
4. Nierozmnażanie zwierzęcia bez konsultacji ze schroniskiem.
5. Informowanie schroniska o zmianie adresu zamieszkania.

## § 5. Wizyty kontrolne

1. Schronisko zastrzega sobie prawo do przeprowadzenia wizyt kontrolnych.
2. Wizyty mają na celu sprawdzenie warunków, w jakich przebywa zwierzę.

## § 6. Zwrot zwierzęcia

1. W przypadku niemożności dalszej opieki nad zwierzęciem, adoptujący zobowiązany jest zwrócić je do schroniska.
2. Zabrania się przekazywania zwierzęcia osobom trzecim bez zgody schroniska.

## § 7. Postanowienia końcowe

1. Schronisko zastrzega sobie prawo do odmowy adopcji bez podania przyczyny.
2. W sprawach nieuregulowanych niniejszym regulaminem stosuje się przepisy prawa polskiego.

---

*Regulamin wchodzi w życie z dniem 1 stycznia 2024 roku.*",
            metaDescription: "Regulamin adopcji zwierząt ze schroniska - poznaj zasady i procedury",
            metaKeywords: "regulamin, adopcja, zasady, procedura adopcji"
        );
        if (adoptionRulesPage.IsSuccess)
        {
            adoptionRulesPage.Value.Publish();
            contentPages.Add(adoptionRulesPage.Value);
        }

        // Polityka prywatności
        var privacyPage = ContentPage.Create(
            title: "Polityka prywatności",
            slug: "polityka-prywatnosci",
            content: @"# Polityka prywatności

## Administrator danych

Administratorem danych osobowych jest Schronisko dla Zwierząt w Białej Podlaskiej, ul. Przytulna 1, 21-500 Biała Podlaska.

## Cele przetwarzania

Przetwarzamy dane osobowe w następujących celach:
- Realizacja procesu adopcji zwierząt
- Kontakt z osobami zainteresowanymi adopcją
- Prowadzenie rejestru adopcji
- Obsługa wolontariatu
- Marketing i komunikacja (za zgodą)

## Podstawa prawna

Dane przetwarzamy na podstawie:
- Art. 6 ust. 1 lit. a RODO (zgoda)
- Art. 6 ust. 1 lit. b RODO (wykonanie umowy)
- Art. 6 ust. 1 lit. c RODO (obowiązek prawny)
- Art. 6 ust. 1 lit. f RODO (prawnie uzasadniony interes)

## Okres przechowywania

Dane przechowujemy przez okres niezbędny do realizacji celów, nie dłużej niż wymagają tego przepisy prawa.

## Prawa osób

Osobom, których dane przetwarzamy, przysługuje prawo do:
- Dostępu do swoich danych
- Sprostowania danych
- Usunięcia danych
- Ograniczenia przetwarzania
- Przenoszenia danych
- Wniesienia sprzeciwu

## Kontakt

W sprawach dotyczących danych osobowych prosimy o kontakt: kontakt@schronisko.pl",
            metaDescription: "Polityka prywatności schroniska dla zwierząt",
            metaKeywords: "polityka prywatności, RODO, dane osobowe"
        );
        if (privacyPage.IsSuccess)
        {
            privacyPage.Value.Publish();
            contentPages.Add(privacyPage.Value);
        }

        // Kontakt
        var contactPage = ContentPage.Create(
            title: "Kontakt",
            slug: "kontakt",
            content: @"# Kontakt ze schroniskiem

## Dane kontaktowe

**Schronisko dla Zwierząt w Białej Podlaskiej**

📍 **Adres:** ul. Przytulna 1, 21-500 Biała Podlaska

📞 **Telefon:** +48 123 456 789

📧 **Email:** kontakt@schronisko.pl

## Godziny otwarcia

| Dzień | Godziny |
|-------|---------|
| Poniedziałek | 9:00 - 16:00 |
| Wtorek | 9:00 - 16:00 |
| Środa | 9:00 - 16:00 |
| Czwartek | 9:00 - 16:00 |
| Piątek | 9:00 - 16:00 |
| Sobota | 9:00 - 14:00 |
| Niedziela | Nieczynne |

## Numer alarmowy

W nagłych przypadkach (znalezione zwierzę, zwierzę w niebezpieczeństwie):

📞 **+48 123 456 780** (całodobowo)

## Dojazd

Schronisko znajduje się na obrzeżach miasta, w pobliżu lasu miejskiego. Dojazd możliwy autobusem linii 5 (przystanek ""Schronisko"") lub samochodem - dostępny jest bezpłatny parking.

## Formularz kontaktowy

Możesz również skontaktować się z nami poprzez formularz kontaktowy dostępny na naszej stronie. Odpowiadamy w ciągu 2 dni roboczych.

---

*Zapraszamy do odwiedzin!*",
            metaDescription: "Kontakt ze schroniskiem dla zwierząt - adres, telefon, godziny otwarcia",
            metaKeywords: "kontakt, adres, telefon, godziny otwarcia, schronisko"
        );
        if (contactPage.IsSuccess)
        {
            contactPage.Value.Publish();
            contentPages.Add(contactPage.Value);
        }

        if (contentPages.Any())
        {
            dbContext.ContentPages.AddRange(contentPages);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} stron CMS", contentPages.Count);
        }
    }

    private static async Task SeedAdminNotificationsAsync(ShelterDbContext dbContext, ILogger logger)
    {
        // Check if notifications already exist
        if (await dbContext.AdminNotifications.AnyAsync())
        {
            logger.LogInformation("Powiadomienia admin już istnieją - pomijam seeding");
            return;
        }

        var notifications = new List<AdminNotification>();

        // Get some animals for reference
        var animals = await dbContext.Animals.Take(5).ToListAsync();
        var adoptionApplications = await dbContext.AdoptionApplications.Take(3).ToListAsync();

        // Notification 1 - New adoption application (unread)
        var notification1 = AdminNotification.Create(
            type: NotificationType.NewAdoptionApplication,
            title: "Nowy wniosek adopcyjny",
            message: "Jan Kowalski złożył wniosek adopcyjny na psa Tofika. Wniosek oczekuje na rozpatrzenie.",
            priority: NotificationPriority.Normal,
            link: "/admin/adoptions",
            relatedEntityType: "AdoptionApplication"
        );
        notifications.Add(notification1);

        // Notification 2 - Escalation alert (high priority, unread)
        var notification2 = AdminNotification.Create(
            type: NotificationType.ApplicationEscalation,
            title: "Eskalacja - brak odpowiedzi 48h",
            message: "Wniosek adopcyjny #2024-015 oczekuje na odpowiedź od ponad 48 godzin. Wymagana pilna reakcja.",
            priority: NotificationPriority.High,
            link: "/admin/adoptions",
            relatedEntityType: "AdoptionApplication"
        );
        notifications.Add(notification2);

        // Notification 3 - New volunteer application (unread)
        var notification3 = AdminNotification.Create(
            type: NotificationType.NewVolunteerApplication,
            title: "Nowe zgłoszenie wolontariusza",
            message: "Marta Wiśniewska zgłosiła chęć zostania wolontariuszem. Profil oczekuje na weryfikację.",
            priority: NotificationPriority.Normal,
            link: "/admin/volunteers",
            relatedEntityType: "Volunteer"
        );
        notifications.Add(notification3);

        // Notification 4 - Visit reminder (normal priority)
        var notification4 = AdminNotification.Create(
            type: NotificationType.VisitReminder,
            title: "Przypomnienie o wizycie",
            message: "Jutro o godzinie 10:00 zaplanowana jest wizyta adopcyjna rodziny Nowaków w sprawie psa Brunona.",
            priority: NotificationPriority.Normal,
            link: "/admin/appointments"
        );
        notifications.Add(notification4);

        // Notification 5 - Animal health alert (urgent, unread)
        if (animals.Any())
        {
            var animalId = animals.First().Id;
            var notification5 = AdminNotification.Create(
                type: NotificationType.AnimalHealthAlert,
                title: "Alert zdrowotny - Tosiek",
                message: "Tosiek wymaga kontroli pooperacyjnej. Termin wizyty: za 7 dni. Proszę o przygotowanie dokumentacji medycznej.",
                priority: NotificationPriority.High,
                link: $"/admin/animals/{animalId}",
                relatedEntityId: animalId,
                relatedEntityType: "Animal"
            );
            notifications.Add(notification5);
        }

        // Notification 6 - System info (read)
        var notification6 = AdminNotification.Create(
            type: NotificationType.SystemAlert,
            title: "Aktualizacja systemu",
            message: "System został zaktualizowany do wersji 2.0. Nowe funkcje: zarządzanie użytkownikami, konfiguracja AI.",
            priority: NotificationPriority.Low,
            expiresAt: DateTime.UtcNow.AddDays(30)
        );
        notifications.Add(notification6);

        // Notification 7 - General info (read, older)
        var notification7 = AdminNotification.Create(
            type: NotificationType.GeneralInfo,
            title: "Raport miesięczny dostępny",
            message: "Raport za poprzedni miesiąc jest już dostępny w sekcji Raporty. Liczba adopcji: 12, Nowe zwierzęta: 8.",
            priority: NotificationPriority.Low,
            link: "/admin/reports"
        );
        notifications.Add(notification7);

        if (notifications.Any())
        {
            dbContext.AdminNotifications.AddRange(notifications);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Dodano {Count} powiadomień administratora", notifications.Count);
        }
    }
}
