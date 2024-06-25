using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace ChatAlerts;

public class Dalamud
{
    public static void Initialize(DalamudPluginInterface pluginInterface)
        => pluginInterface.Create<Dalamud>();

        // @formatter:off
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager        Commands        { get; private set; } = null!;
        [PluginService] public static ISigScanner            SigScanner      { get; private set; } = null!;
        [PluginService] public static IDataManager           GameData        { get; private set; } = null!;
        [PluginService] public static IChatGui               Chat            { get; private set; } = null!;
        [PluginService] public static IPluginLog             Log             { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider   Interop         { get; private set; } = null!;
    // @formatter:on
}
