using Itenium.Forge.Core;
using Itenium.Forge.Settings;

namespace Shell.Api;

/// <summary>Application settings for the Shell microservice.</summary>
public class ShellSettings : IForgeSettings
{
    /// <inheritdoc />
    public ForgeSettings Forge { get; } = new();
}
