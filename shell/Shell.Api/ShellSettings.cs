using Itenium.Forge.Core;
using Itenium.Forge.Settings;

namespace Shell.Api;

public class ShellSettings : IForgeSettings
{
    /// <inheritdoc />
    public ForgeSettings Forge { get; } = new();
}
