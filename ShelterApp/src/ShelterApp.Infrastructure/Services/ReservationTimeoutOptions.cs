namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Configuration options for the automatic cancellation of expired reservations service (WB-14)
/// </summary>
public class ReservationTimeoutOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ReservationTimeout";

    /// <summary>
    /// Number of days after which a reservation is considered expired
    /// </summary>
    public int TimeoutDays { get; set; } = 7;

    /// <summary>
    /// Check interval in hours
    /// </summary>
    public int CheckIntervalHours { get; set; } = 1;

    /// <summary>
    /// Whether the service is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of applications processed per iteration
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
