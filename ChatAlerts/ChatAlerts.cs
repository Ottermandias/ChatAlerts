using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using ChatAlerts.Gui;
using ChatAlerts.SeFunctions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using NAudio.Midi;

namespace ChatAlerts
{
    public class ChatAlerts : IDalamudPlugin
    {
        public string Name
            => "Chat Alerts";

        private const string CommandName = "/chatalerts";

        public static    ChatAlertsConfig Config    { get; private set; } = null!;
        public static    PlaySound        PlaySound { get; private set; } = null!;
        public readonly  DebugHelper?     Debug = null;
        private readonly Interface        _interface;
        public readonly ChatWatcher      Watcher;

        public static string Version { get; private set; } = string.Empty;

        public ChatAlerts(DalamudPluginInterface pluginInterface)
        {
            Dalamud.Initialize(pluginInterface);
            Config    = ChatAlertsConfig.Load();
            Version   = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            PlaySound = new PlaySound(Dalamud.SigScanner);
            Watcher  = new ChatWatcher();

            _interface = new Interface(this);
#if false && DEBUG
            Debug = new DebugHelper();
#endif

            Dalamud.Commands.AddHandler(CommandName, new CommandInfo(OnConfigCommandHandler)
            {
                HelpMessage = $"Open config window for {Name}",
                ShowInHelp  = true,
            });
        }

        public void OnConfigCommandHandler(object command, object args)
            => _interface.Visible = !_interface.Visible;


        public void Dispose()
        {
            _interface.Dispose();
            Watcher.Dispose();
            Config.Dispose();
            Dalamud.Commands.RemoveHandler(CommandName);
            Debug?.Dispose();
        }
    }
}
