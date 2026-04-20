using Itenium.Forge.Core;
using Itenium.Forge.Settings;

namespace FeatureFlags.Api;

public class FeatureFlagsSettings : IForgeSettings
{
    public ForgeSettings Forge { get; } = new();
}
