namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// Chatbot profiling dialog state
/// </summary>
public enum ChatState
{
    /// <summary>
    /// Initial state - greeting
    /// </summary>
    Initial,

    /// <summary>
    /// Asking about preferred species
    /// </summary>
    ProfilingSpecies,

    /// <summary>
    /// Asking about pet care experience
    /// </summary>
    ProfilingExperience,

    /// <summary>
    /// Asking about living conditions
    /// </summary>
    ProfilingLiving,

    /// <summary>
    /// Asking about lifestyle
    /// </summary>
    ProfilingLifestyle,

    /// <summary>
    /// Asking about children in household
    /// </summary>
    ProfilingChildren,

    /// <summary>
    /// Asking about other pets
    /// </summary>
    ProfilingPets,

    /// <summary>
    /// Asking about preferred size
    /// </summary>
    ProfilingSize,

    /// <summary>
    /// Profile complete - ready for recommendations
    /// </summary>
    ProfilingComplete,

    /// <summary>
    /// Normal conversation
    /// </summary>
    Conversing
}
