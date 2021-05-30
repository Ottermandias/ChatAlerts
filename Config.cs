using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Lumina.Excel.GeneratedSheets;

namespace ChatAlerts {
    public class Config : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private Plugin plugin;
        
        public List<Alert> Alerts = new();

        public int Version { get; set; }

        public void Init(Plugin plugin, DalamudPluginInterface pluginInterface) {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
        }

        public void Save() {
            pluginInterface.SavePluginConfig(this);
        }

        private float alignAlertFieldNames;
        
        private void AlignLabel(string text) {
            ImGui.Dummy(new Vector2(1, 24 * ImGui.GetIO().FontGlobalScale));
            ImGui.SameLine();
            var textSize = ImGui.CalcTextSize(text);

            if (alignAlertFieldNames - textSize.X < ImGui.GetStyle().ItemSpacing.X * 2) {
                alignAlertFieldNames = textSize.X + ImGui.GetStyle().ItemSpacing.X * 2;
            }
            
            ImGui.SetCursorPosX(alignAlertFieldNames - textSize.X);
            
            ImGui.Text(text);
            ImGui.SameLine();
        }

        public bool DrawConfigUI() {
            var hasChange = false;
            var drawConfig = true;
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoCollapse;
            
            ImGui.SetNextWindowSizeConstraints(new Vector2(500, 300) * ImGui.GetIO().FontGlobalScale, new Vector2(float.MaxValue));
            
            ImGui.PushStyleColor(ImGuiCol.TitleBg, 0xAA993333);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, 0xFF993333);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xEE000000);
            ImGui.Begin($"{plugin.Name} Config", ref drawConfig, windowFlags);
            ImGui.PopStyleColor(3);
            
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF5E5BAA);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF5E5BDD);
            var oPos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize("Support on Ko-fi").X);
            if (ImGui.SmallButton("Support on Ko-fi")) {
                Process.Start("https://ko-fi.com/Caraxi");
            }
            ImGui.SetCursorPos(oPos);
            ImGui.PopStyleColor(3);
            
            ImGui.Text("Create and configure chat alerts.\nAlerts are processed from top to bottom.");

            ImGui.Separator();
            
            var i = 0;
            Alert actionAlert = null;
            ConfigAction action = ConfigAction.None;
            foreach (var alert in Alerts) {
                ImGui.PushID($"SimpleTweaks.ChatAlerts.Alert-{i++}");
                if (ImGui.TreeNodeEx($"{alert.Name}###chatAlerts-alert-treeHeader-{i}", ImGuiTreeNodeFlags.Framed)) {
                    ImGui.PushItemWidth(-1);
                    AlignLabel("Name:");
                    ImGui.SetNextItemWidth(-145 * ImGui.GetIO().FontGlobalScale);
                    hasChange |= ImGui.InputTextWithHint($"##chatAlerts-alert-nameEdit-{i}", "Name", ref alert.Name, 128);
                    ImGui.SameLine();
                    
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowUp}##chatAlerts-alert-moveUp-{i}", new Vector2(ImGui.GetItemRectSize().Y, ImGui.GetItemRectSize().Y))) {
                        actionAlert = alert;
                        action = ConfigAction.MoveUp;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowDown}##chatAlerts-alert-moveDown-{i}", new Vector2(ImGui.GetItemRectSize().Y, ImGui.GetItemRectSize().Y))) {
                        actionAlert = alert;
                        action = ConfigAction.MoveDown;
                    }
                    ImGui.SameLine();
                    ImGui.PopFont();

                    ImGui.PushStyleColor(ImGuiCol.Button, 0x660000B5);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF000078);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF0000BD);
                    if (ImGui.Button($"Delete##chatAlerts-alert-delete-{i}", new Vector2(-1, ImGui.GetItemRectSize().Y)) && ImGui.GetIO().KeyShift) {
                        actionAlert = alert;
                        action = ConfigAction.Delete;
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Hold SHIFT to delete.");
                    }
                    ImGui.PopStyleColor(3);
                    AlignLabel("Content:");

                    if (alert.IsRegex && alert.CompiledRegex == null) {
                        ImGui.PushStyleColor(ImGuiCol.Border, 0xFF0000FF);
                        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2);
                    }
                    
                    hasChange |= ImGui.InputTextWithHint($"##chatAlerts-alert-contentEdit-{i}", "Content", ref alert.Content, 500);

                    if (alert.IsRegex && alert.CompiledRegex == null) {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleVar();
                    }
                    
                    AlignLabel("Enabled:");
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-enabledCheckbox-{i}", ref alert.Enabled);
                    
                    ImGui.SameLine();
                    ImGui.Text("    Use RegEx:");
                    ImGui.SameLine();
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-regexCheckbox-{i}", ref alert.IsRegex);
                    
                    ImGui.SameLine();
                    ImGui.Text("    Ignore Case:");
                    ImGui.SameLine();
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-ignoreCaseCheckbox-{i}", ref alert.IgnoreCase);
                    
                    AlignLabel("Sender:");
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-senderAlert-{i}", ref alert.SenderAlert);
                    
                    ImGui.SameLine();
                    ImGui.Text("    Trigger on filtered messages:");
                    ImGui.SameLine();
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-includeHidden{i}", ref alert.IncludeHidden);
                    
                    AlignLabel("Channels:");
                    var channelListStr = alert.Channels.Contains(XivChatType.None) ? "All" : string.Join(", ", alert.Channels.Where(c => c != XivChatType.CrossParty).Select(c => c.GetDetails()?.FancyName ?? c.ToString()));
                    if (ImGui.BeginCombo("##chatAlerts-alert-channelSelect-{i}", channelListStr, ImGuiComboFlags.HeightLarge)) {
                        
                        ImGui.Columns(3);

                        foreach (var chatType in (XivChatType[])Enum.GetValues(typeof(XivChatType))) {
                            if (chatType == XivChatType.CrossParty || chatType == XivChatType.Debug || chatType == XivChatType.Urgent) continue;
                            var name = chatType == XivChatType.None ? "All" : chatType.GetDetails()?.FancyName ?? chatType.ToString();
                            var e = alert.Channels.Contains(chatType);
                            
                            if (ImGui.Checkbox($"{name}##chatAlerts-alert-channelOption-{(ushort)chatType}", ref e)) {
                                alert.Channels.RemoveAll(c => c == chatType || c == XivChatType.CrossParty && chatType == XivChatType.Party);
                                if (e) {
                                    alert.Channels.Add(chatType);
                                    if (chatType == XivChatType.Party) alert.Channels.Add(XivChatType.CrossParty);
                                }
                                alert.Channels.Sort();
                                hasChange = true;
                            }

                            ImGui.NextColumn();
                            if (chatType == XivChatType.None && e) break;
                        }
                        ImGui.Columns();
                        
                        
                        ImGui.EndCombo();
                    }
                    
                    AlignLabel("Highlight:");

                    if (alert.SenderAlert) {
                        ImGui.TextDisabled("Not supported with Sender Alerts.");
                    } else {
                        hasChange |= ImGui.Checkbox($"##chatAlerts-alert-highlightCheckbox-{i}", ref alert.Highlight);

                        if (alert.Highlight) {
                            ImGui.SameLine();
                            ImGui.Text("Text Colour:");
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(600);

                            var fc = pluginInterface.Data.Excel.GetSheet<UIColor>().GetRow(alert.HighlightForeground);
                            var fa = fc.UIForeground & 255;
                            var fb = (fc.UIForeground >> 8) & 255;
                            var fg = (fc.UIForeground >> 16) & 255;
                            var fr = (fc.UIForeground >> 24) & 255;
                            
                            var fColor = new Vector4(fr / 255f, fg / 255f, fb / 255f, fa / 255f);
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, fColor);
                            ImGui.BeginChild($"###chatAlerts-alert-highlightColor-preview", new Vector2(24 * ImGui.GetIO().FontGlobalScale), true);
                            ImGui.EndChild();
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                                alert.HighlightForeground = 0;
                                hasChange = true;
                            }
                            ImGui.PopStyleColor();
                            ImGui.SameLine();

                            if (ImGui.BeginCombo($"##chatAlerts-alert-highlightColor-{i}", "", ImGuiComboFlags.NoPreview)) {

                                var counter = 0;
                                foreach (var c in pluginInterface.Data.Excel.GetSheet<UIColor>()) {
                                    var a = c.UIForeground & 255;
                                    var b = (c.UIForeground >> 8) & 255;
                                    var g = (c.UIForeground >> 16) & 255;
                                    var r = (c.UIForeground >> 24) & 255;

                                    if (a == 0) continue;


                                    var color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);


                                    ImGui.PushStyleColor(ImGuiCol.ChildBg, color);
                                    ImGui.BeginChild($"###chatAlerts-alert-highlightColor-option-{c.RowId}", new Vector2(24 * ImGui.GetIO().FontGlobalScale), true);
                                    ImGui.EndChild();
                                    if (ImGui.IsItemClicked()) {
                                        alert.HighlightForeground = c.RowId;
                                        hasChange = true;
                                        ImGui.CloseCurrentPopup();
                                    }

                                    if (ImGui.IsItemHovered()) {
                                        ImGui.SetTooltip($"{c.RowId}");
                                    }

                                    ImGui.PopStyleColor();
                                    if (++counter % 10 != 0) ImGui.SameLine();
                                }
                                ImGui.EndCombo();
                            }

                            ImGui.SameLine();
                            ImGui.Text("Glow Colour:");
                            ImGui.SameLine();
                            var gc = pluginInterface.Data.Excel.GetSheet<UIColor>().GetRow(alert.HighlightGlow);
                            var ga = gc.UIGlow & 255;
                            var gb = (gc.UIGlow >> 8) & 255;
                            var gg = (gc.UIGlow >> 16) & 255;
                            var gr = (gc.UIGlow >> 24) & 255;
                            
                            var gColor = new Vector4(gr / 255f, gg / 255f, gb / 255f, ga / 255f);
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, gColor);
                            ImGui.BeginChild($"###chatAlerts-alert-highlightGlowColor-preview", new Vector2(24 * ImGui.GetIO().FontGlobalScale), true);
                            ImGui.EndChild();
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                                alert.HighlightGlow = 0;
                                hasChange = true;
                            }
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                            
                            if (ImGui.BeginCombo($"##chatAlerts-alert-highlightGlow-{i}", "", ImGuiComboFlags.NoPreview)) {
                                
                                var counter = 0;
                                foreach (var c in pluginInterface.Data.Excel.GetSheet<UIColor>()) {
                                    var a = c.UIGlow & 255;
                                    var b = (c.UIGlow >> 8) & 255;
                                    var g = (c.UIGlow >> 16) & 255;
                                    var r = (c.UIGlow >> 24) & 255;
                                    
                                    if (a == 0) continue;
                                    

                                    var color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                                    
                                    
                                    ImGui.PushStyleColor(ImGuiCol.ChildBg, color);
                                    ImGui.BeginChild($"###chatAlerts-alert-highlightGlowColor-option-{c.RowId}", new Vector2(24 * ImGui.GetIO().FontGlobalScale), true);
                                    ImGui.EndChild();
                                    if (ImGui.IsItemClicked()) {
                                        alert.HighlightGlow = c.RowId;
                                        hasChange = true;
                                        ImGui.CloseCurrentPopup();
                                    }
                                    if (ImGui.IsItemHovered()) {
                                        ImGui.SetTooltip($"{c.RowId}");
                                    }
                                    ImGui.PopStyleColor();
                                    if (++counter % 10 != 0) ImGui.SameLine();
                                }
                                
                                ImGui.EndCombo();
                            }
                        }
                    }
                    
                    AlignLabel("Audio Alert:");
                    hasChange |= ImGui.Checkbox($"##chatAlerts-alert-playSoundCheckbox-{i}", ref alert.PlaySound);

                    if (alert.PlaySound) {
                        ImGui.SameLine();
                        
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{(char) FontAwesomeIcon.Play}###chatAlerts-alert-soundFileTest", new Vector2(ImGui.GetItemRectSize().Y, ImGui.GetItemRectSize().Y))) {
                            alert.StartSound(plugin);
                        }
                        ImGui.PopFont();
                        ImGui.SameLine();
                        if (alert.CustomSound) {
                            
                            ImGui.SetNextItemWidth(150 * ImGui.GetIO().FontGlobalScale);
                            hasChange |= ImGui.SliderFloat("###chatAlerts-alert-soundFileVolume", ref alert.Volume, 0, 1, $"Volume: {alert.Volume*100:F1}%%");

                            ImGui.SameLine();
                    
                            if (alert.PlaySound && alert.SoundReady == false) {
                                ImGui.PushStyleColor(ImGuiCol.Border, 0xFF0000FF);
                                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2);
                            }
                            ImGui.SetNextItemWidth(-(ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.X*2));
                            hasChange |= ImGui.InputTextWithHint($"###chatAlerts-alert-soundFileInput-{i}", "Sound File Path", ref alert.SoundPath, 800);
                            ImGui.SameLine();
                            if (ImGui.Button("X", new Vector2(-1, ImGui.GetItemRectSize().Y))) {
                                alert.CustomSound = false;
                                hasChange = true;
                            }
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Use game sounds");
                        
                            if (alert.PlaySound && alert.SoundReady == false) {
                                ImGui.PopStyleColor();
                                ImGui.PopStyleVar();
                            }
                        } else {
                            ImGui.SetNextItemWidth(150);
                            if (ImGui.BeginCombo($"###chatAlerts_alert{i}_gameSoundCombo", $"{alert.SoundEffect.GetAttribute<DescriptionAttribute>()?.Description ?? alert.SoundEffect.ToString()}")) {

                                foreach (var se in (SoundEffect[])Enum.GetValues(typeof(SoundEffect))) {
                                    if (ImGui.Selectable($"{(se.GetAttribute<DescriptionAttribute>()?.Description ?? se.ToString())}##chatAlerts_alert{i}_gameSoundOption")) {
                                        alert.SoundEffect = se;
                                        plugin.PlayGameSound(se);
                                        hasChange = true;
                                    }
                                }
                                
                                ImGui.EndCombo();
                            }
                            
                            ImGui.SameLine();
                            if (ImGui.Button("Custom")) {
                                alert.CustomSound = true;
                                hasChange = true;
                            }
                            
                        }
                    }
                    
                    ImGui.PopItemWidth();
                    ImGui.TreePop();

                }
                ImGui.PopID();
            }
            ImGui.Dummy(new Vector2(10 * ImGui.GetIO().FontGlobalScale));
            ImGui.SetCursorPosX(50 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.Button("Add Alert", new Vector2(-50 * ImGui.GetIO().FontGlobalScale, 24 * ImGui.GetIO().FontGlobalScale))) {
                var a = new Alert();
                a.Channels.Add(XivChatType.Say);
                a.Channels.Add(XivChatType.Shout);
                a.Channels.Add(XivChatType.Yell);
                a.Channels.Add(XivChatType.Party);
                a.Channels.Add(XivChatType.Alliance);
                a.Channels.Add(XivChatType.FreeCompany);

                Alerts.Add(a);
                hasChange = true;
            }

            if (actionAlert != null && action != ConfigAction.None) {
                switch (action) {
                    case ConfigAction.Delete: {
                        Alerts.Remove(actionAlert);
                        actionAlert.Dispose();
                        break;
                    }
                    case ConfigAction.MoveUp: {
                        var idx = Alerts.IndexOf(actionAlert);
                        if (idx <= 0) break;
                        Alerts.Remove(actionAlert);
                        Alerts.Insert(idx - 1, actionAlert);
                        break;
                    }
                    case ConfigAction.MoveDown: {
                        var idx = Alerts.IndexOf(actionAlert);
                        if (idx < 0 || idx >= Alerts.Count - 1) break;
                        Alerts.Remove(actionAlert);
                        Alerts.Insert(idx + 1, actionAlert);
                        break;
                    }
                }
                
                hasChange = true;
            }
            
            ImGui.End();

            if (hasChange) {
                Save();
                plugin.UpdateAlerts();
            }
            
            return drawConfig;
        }

        public void Dispose() {
            foreach (var configAlert in Alerts) {
                configAlert.Dispose();
            }
        }
    }
}