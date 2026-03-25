using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Appointments;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Cms;
using ShelterApp.Domain.Emails;
using ShelterApp.Domain.Notifications;
using ShelterApp.Domain.Users;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Infrastructure.Persistence;

public class ShelterDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork
{
    private readonly IPublisher? _publisher;
    private IDbContextTransaction? _currentTransaction;

    public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options)
    {
    }

    public ShelterDbContext(DbContextOptions<ShelterDbContext> options, IPublisher publisher) : base(options)
    {
        _publisher = publisher;
    }

    // Animals
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<AnimalStatusChange> AnimalStatusChanges => Set<AnimalStatusChange>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<AnimalNote> AnimalNotes => Set<AnimalNote>();

    // Adoptions
    public DbSet<Adopter> Adopters => Set<Adopter>();
    public DbSet<AdopterStatusChange> AdopterStatusChanges => Set<AdopterStatusChange>();
    public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
    public DbSet<AdoptionApplicationStatusChange> AdoptionApplicationStatusChanges => Set<AdoptionApplicationStatusChange>();
    public DbSet<AdoptionContract> AdoptionContracts => Set<AdoptionContract>();

    // Volunteers
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<VolunteerStatusChange> VolunteerStatusChanges => Set<VolunteerStatusChange>();
    public DbSet<VolunteerCertificate> VolunteerCertificates => Set<VolunteerCertificate>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<VolunteerAssignment> VolunteerAssignments => Set<VolunteerAssignment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Appointments
    public DbSet<VisitSlot> VisitSlots => Set<VisitSlot>();
    public DbSet<VisitBooking> VisitBookings => Set<VisitBooking>();

    // Users
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Emails
    public DbSet<EmailQueue> EmailQueue => Set<EmailQueue>();

    // CMS (WF-27)
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();

    // Notifications (WF-30, WF-31)
    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();

    public bool HasActiveTransaction => _currentTransaction is not null;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelterDbContext).Assembly);

        // Configure Identity tables with custom schema/names
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // UpdateTimestamps(); // Temporarily disabled for testing
        FixStatusChangeEntitiesState();
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Fixes the state of StatusChange entities - new entities should be Added, not Modified.
    /// This is a workaround for an EF Core tracking issue with navigation collections,
    /// where newly added items are sometimes marked as Modified instead of Added.
    /// </summary>
    private void FixStatusChangeEntitiesState()
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(-1);

        // Fix AnimalStatusChange
        foreach (var entry in ChangeTracker.Entries<AnimalStatusChange>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.ChangedAt > threshold)
            {
                entry.State = EntityState.Added;
            }
        }

        // Fix AdopterStatusChange
        foreach (var entry in ChangeTracker.Entries<AdopterStatusChange>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.ChangedAt > threshold)
            {
                entry.State = EntityState.Added;
            }
        }

        // Fix AdoptionApplicationStatusChange
        foreach (var entry in ChangeTracker.Entries<AdoptionApplicationStatusChange>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.ChangedAt > threshold)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException("A transaction is already in progress");

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Entity<Guid> entity)
            {
                entity.SetUpdatedAt();
            }
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_publisher is null) return;

        var aggregateRoots = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
