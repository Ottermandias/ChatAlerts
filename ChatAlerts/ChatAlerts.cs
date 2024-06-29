using System.Reflection;
using ChatAlerts.Gui;
using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace ChatAlerts;

public class ChatAlerts : IDalamudPlugin
{
    public string Name
        => "Chat Alerts";

    private const string CommandName = "/chatalerts";

    public static    ChatAlertsConfig Config { get; private set; } = null!;
    private readonly Interface        _interface;
    public readonly  ChatWatcher      Watcher;

    public static string Version { get; private set; } = string.Empty;

    public ChatAlerts(IDalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);
        Config  = ChatAlertsConfig.Load();
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
        Watcher = new ChatWatcher();

        _interface = new Interface(this);

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
    }
}
