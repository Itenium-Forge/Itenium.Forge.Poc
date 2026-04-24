namespace Shell.Api.Clients;

/// <summary>Represents a feature flag with its current state.</summary>
/// <param name="Name">Unique identifier of the flag.</param>
/// <param name="Enabled">Whether the flag is currently enabled.</param>
public record Flag(string Name, bool Enabled);
