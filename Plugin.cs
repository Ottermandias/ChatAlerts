using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;

namespace ChatAlerts {
    public class Plugin : IDalamudPlugin {
        public string Name => "Chat Alerts";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public Config PluginConfig { get; private set; }

#if DEBUG
        private bool drawConfigWindow = true;
#else
        private bool drawConfigWindow = false;
#endif
        
        private readonly List<XivChatType> watchedChannels = new();
        private bool watchAllChannels;

        private delegate ulong PlayGameSoundDelegate(SoundEffect id, ulong a2, ulong a3);

        private PlayGameSoundDelegate playGameSound;
        
#if DEBUG
        private Dalamud.Hooking.Hook<PlayGameSoundDelegate> soundPlayHook;
#endif
        

        public void Dispose() {
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            PluginInterface.UiBuilder.OnOpenConfigUi -= this.OnConfigCommandHandler;
            PluginInterface.Framework.Gui.Chat.OnChatMessage -= OnChatMessage;
            PluginInterface.Framework.Gui.Chat.OnCheckMessageHandled -= OnCheckMessageHandled;

            PluginConfig.Dispose();

#if DEBUG
            soundPlayHook?.Disable();
            soundPlayHook?.Dispose();   
#endif
            RemoveCommands();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {

            playGameSound = Marshal.GetDelegateForFunctionPointer<PlayGameSoundDelegate>(pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 4D 39 BE"));
            
#if DEBUG
            soundPlayHook = new Dalamud.Hooking.Hook<PlayGameSoundDelegate>(pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 4D 39 BE"), new PlayGameSoundDelegate((a, b, c) => {
                PluginLog.Log($"Play Sound: {a} [{b}, {c}]");
                return soundPlayHook.Original(a, b, c);
            }));
            soundPlayHook.Enable();
#endif
            


            this.PluginInterface = pluginInterface;
            this.PluginConfig = (Config)pluginInterface.GetPluginConfig() ?? new Config();
            this.PluginConfig.Init(this, pluginInterface);

            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;
            PluginInterface.UiBuilder.OnOpenConfigUi += this.OnConfigCommandHandler;
            PluginInterface.Framework.Gui.Chat.OnChatMessage += OnChatMessage;
            PluginInterface.Framework.Gui.Chat.OnCheckMessageHandled += OnCheckMessageHandled;

            UpdateAlerts();
            SetupCommands();
        }

        public void SetupCommands() {
            PluginInterface.CommandManager.AddHandler("/chatalerts", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler) {
                HelpMessage = $"Open config window for {this.Name}",
                ShowInHelp = true
            });
        }

        public void OnConfigCommandHandler(object command, object args) {
            drawConfigWindow = !drawConfigWindow;
        }

        public void RemoveCommands() {
            PluginInterface.CommandManager.RemoveHandler("/pChatAlertsconfig");
        }

        private void BuildUI() {
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
        }

        public void PlayGameSound(SoundEffect id) {
            playGameSound(id, 0, 0);
        }
        
        internal void UpdateAlerts() {
            watchedChannels.Clear();
            watchAllChannels = false;
            foreach (var a in PluginConfig.Alerts) {
                a.Update();
                if (a.Channels.Contains(XivChatType.None)) watchAllChannels = true;
                watchedChannels.AddRange(a.Channels.Where(chatType => !watchedChannels.Contains(chatType)));
            }
            
            PluginLog.Log($"Watching Channels: { (watchAllChannels ? "All" : string.Join(",", watchedChannels)) }");
        }
        
        private void HandleMessage(XivChatType type, ref SeString sender, ref SeString message, bool preFilter) {
            if (!(watchAllChannels || watchedChannels.Contains(type))) return;
            var soundPlayed = false;
            foreach (var alert in PluginConfig.Alerts.Where(a => a.Enabled && a.IncludeHidden == preFilter && (a.Channels.Contains(XivChatType.None) || a.Channels.Contains(type)))) {
                var alertMatch = false;
                if (alert.IsRegex && alert.CompiledRegex == null) continue;
                if (string.IsNullOrEmpty(alert.Content)) continue;
                var newPayloads = new List<Payload>();
                foreach (var payload in alert.SenderAlert ? sender.Payloads : message.Payloads) {
                    if (!(payload is TextPayload tp)) {
                        newPayloads.Add(payload);
                        continue;
                    }

                    var idx = 0;
                    if (alert.IsRegex) {
                        var match = alert.CompiledRegex.Match(tp.Text);
                        if (!match.Success) {
                            newPayloads.Add(payload);
                            continue;
                        }

                        while (match.Success) {

                            var skippedStr = tp.Text.Substring(idx, match.Index - idx);
                            if (!string.IsNullOrEmpty(skippedStr)) {
                                newPayloads.Add(new TextPayload(skippedStr));
                            }

                            idx = match.Index + match.Length;
                            if (alert.Highlight) {
                                newPayloads.Add(new UIForegroundPayload(PluginInterface.Data, (ushort)alert.HighlightForeground));
                                newPayloads.Add(new UIGlowPayload(PluginInterface.Data, (ushort) alert.HighlightGlow));
                            }
                            newPayloads.Add(new TextPayload(match.Value));
                            if (alert.Highlight) {
                                newPayloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                newPayloads.Add(new UIGlowPayload(PluginInterface.Data, 0));
                            }
                            match = match.NextMatch();
                            alertMatch = true;
                        }

                        var remainingStr = tp.Text.Substring(idx);
                        if (!string.IsNullOrEmpty(remainingStr)) newPayloads.Add(new TextPayload(remainingStr));
                        
                        
                    } else {
                        var str = tp.Text;
                        var nextIndex = str.IndexOf(alert.Content, idx, alert.IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                        while (nextIndex >= 0) {
                            var skippedStr = tp.Text.Substring(idx, nextIndex - idx);
                            if (!string.IsNullOrEmpty(skippedStr)) {
                                newPayloads.Add(new TextPayload(skippedStr));
                            }
                            if (alert.Highlight) {
                                newPayloads.Add(new UIForegroundPayload(PluginInterface.Data, (ushort)alert.HighlightForeground));
                                newPayloads.Add(new UIGlowPayload(PluginInterface.Data, (ushort) alert.HighlightGlow));
                            }
                            newPayloads.Add(new TextPayload(tp.Text.Substring(nextIndex, alert.Content.Length)));
                            if (alert.Highlight) {
                                newPayloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                newPayloads.Add(new UIGlowPayload(PluginInterface.Data, 0));
                            }
                            idx = nextIndex + alert.Content.Length;
                            alertMatch = true;
                            nextIndex = str.IndexOf(alert.Content, idx, alert.IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                        }
                        
                        var remainingStr = tp.Text.Substring(idx);
                        if (!string.IsNullOrEmpty(remainingStr)) newPayloads.Add(new TextPayload(remainingStr));
                    }
                }

                if (!alertMatch) continue;
                if (!alert.SenderAlert) {
                    message = new SeString(newPayloads);
                }
                
                if (!soundPlayed) soundPlayed = alert.StartSound(this);
            }            
        }
        
        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            HandleMessage(type, ref sender, ref message, false);
        }
        
        private void OnCheckMessageHandled(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled) {
            HandleMessage(type, ref sender, ref message, true);
        }
    }
}
