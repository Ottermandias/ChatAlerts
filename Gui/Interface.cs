using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ChatAlerts.Gui.Raii;
using ChatAlerts.SeFunctions;
using Dalamud.Game.Text;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace ChatAlerts.Gui
{
    public class Interface : IDisposable
    {
        public           bool                        Visible;
        private          float                       _alignAlertFieldNames;
        private readonly Dictionary<ushort, Vector4> _foregroundColors;
        private readonly Dictionary<ushort, Vector4> _glowColors;

        private readonly Sounds[] _validSounds =
            ((Sounds[]) Enum.GetValues(typeof(Sounds))).Where(s => s != Sounds.None && s != Sounds.Unknown).ToArray();

        private readonly string _chatAlertsHeader;
        private          float  _scale;

        private ConfigAction _action = ConfigAction.None;
        private Alert?       _changedAlert;
        private Vector2      _colorPreviewSize = Vector2.Zero;
        private bool         _changes;

        private void ResetChange()
        {
            _changes      = false;
            _action       = ConfigAction.None;
            _changedAlert = null;
        }

        public Interface(ChatAlerts plugin)
        {
            ChatAlerts plugin1 = plugin;

            _chatAlertsHeader = ChatAlerts.Version.Length > 0
                ? $"{plugin1.Name} v{ChatAlerts.Version}###{plugin1.Name}Main"
                : $"{plugin1.Name}###{plugin1.Name}Main";

            Dalamud.PluginInterface.UiBuilder.Draw         += Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi += ToggleVisibility;

            var colorSheet = Dalamud.GameData.Excel.GetSheet<UIColor>()!;
            _foregroundColors = new Dictionary<ushort, Vector4>((int) colorSheet.RowCount);
            _glowColors       = new Dictionary<ushort, Vector4>((int) colorSheet.RowCount);
            foreach (var color in colorSheet)
            {
                var fa = color.UIForeground & 255;
                if (fa > 0)
                {
                    var fb = (color.UIForeground >> 8) & 255;
                    var fg = (color.UIForeground >> 16) & 255;
                    var fr = (color.UIForeground >> 24) & 255;
                    _foregroundColors[(ushort) color.RowId] = new Vector4(fr / 255f, fg / 255f, fb / 255f, fa / 255f);
                }

                var ga = color.UIGlow & 255;
                if (ga > 0)
                {
                    var gb = (color.UIGlow >> 8) & 255;
                    var gg = (color.UIGlow >> 16) & 255;
                    var gr = (color.UIGlow >> 24) & 255;
                    _glowColors[(ushort) color.RowId] = new Vector4(gr / 255f, gg / 255f, gb / 255f, ga / 255f);
                }
            }
        }

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= ToggleVisibility;
            Dalamud.PluginInterface.UiBuilder.Draw         -= Draw;
        }

        public void ToggleVisibility()
            => Visible = !Visible;

        private void AlignLabel(string text)
        {
            ImGuiHelpers.ScaledDummy(0, 24);
            ImGui.SameLine();
            var textSize = ImGui.CalcTextSize(text);

            if (_alignAlertFieldNames - textSize.X < ImGui.GetStyle().ItemSpacing.X * 2)
                _alignAlertFieldNames = textSize.X + ImGui.GetStyle().ItemSpacing.X * 2;

            ImGui.SetCursorPosX(_alignAlertFieldNames - textSize.X);

            ImGui.Text(text);
            ImGui.SameLine();
        }

        private void DrawMoveButton(Alert alert, int idx)
        {
            using var font = ImGuiRaii.PushFont(UiBuilder.IconFont);
            if (!ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconChar()}##moveUp{idx}",
                new Vector2(ImGui.GetItemRectSize().Y)))
                return;

            _changedAlert = alert;
            _action       = ConfigAction.MoveUp;
        }

        private void DrawDeleteButton(Alert alert, int idx)
        {
            using var raii = ImGuiRaii.PushColor(ImGuiCol.Button, 0x660000B5)
                .Push(ImGuiCol.ButtonActive,  0xFF000078)
                .Push(ImGuiCol.ButtonHovered, 0xFF0000BD);
            using var font = ImGuiRaii.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}##delete{idx}", new Vector2(ImGui.GetItemRectSize().Y))
             && ImGui.GetIO().KeyShift)
            {
                _changedAlert = alert;
                _action       = ConfigAction.Delete;
            }

            font.Pop();

            ImGuiCustom.HoverTooltip("Hold SHIFT to delete.");
        }

        private void DrawAlert(Alert alert, int idx)
        {
            if (!ImGui.TreeNodeEx($"{alert.Name}###alert{idx}"))
                return;

            using var raii = ImGuiRaii.DeferredEnd(ImGui.TreePop);
            AlignLabel("Name:");
            ImGui.SetNextItemWidth(-145 * _scale);

            _changes |= ImGui.InputTextWithHint($"##newName{idx}", "Name", ref alert.Name, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.SameLine();
            DrawMoveButton(alert, idx);
            ImGui.SameLine();
            DrawDeleteButton(alert, idx);

            AlignLabel("Content:");
            ImGui.SameLine();
            DrawContentInput(alert, idx);

            DrawCheckboxes(alert, idx);

            DrawChannels(alert, idx);

            DrawHighlights(alert, idx);

            DrawAudio(alert, idx);
        }

        private void DrawContentInput(Alert alert, int idx)
        {
            var       badRegex = !alert.CanMatch();
            using var color    = ImGuiRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, badRegex);
            using var style    = ImGuiRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2, badRegex);

            if (!ImGui.InputTextWithHint($"##alertContent{idx}", "Content", ref alert.Content, 500))
                return;

            alert.UpdateRegex();
            _changes = true;
        }

        private void DrawCheckboxes(Alert alert, int idx)
        {
            AlignLabel("Enabled:");
            _changes |= ImGui.Checkbox($"##alertEnabled{idx}", ref alert.Enabled);

            ImGui.SameLine();
            ImGui.Text("    Use RegEx:");
            ImGui.SameLine();
            if (ImGui.Checkbox($"##alertRegex{idx}", ref alert.IsRegex))
            {
                alert.UpdateRegex();
                _changes = true;
            }

            ImGui.SameLine();
            ImGui.Text("    Ignore Case:");
            ImGui.SameLine();
            if (ImGui.Checkbox($"##alertIgnoreCase{idx}", ref alert.IgnoreCase))
            {
                alert.UpdateRegex();
                _changes = true;
            }

            AlignLabel("Sender:");
            _changes |= ImGui.Checkbox($"##alertSender{idx}", ref alert.SenderAlert);

            ImGui.SameLine();
            ImGui.Text("    Trigger on filtered messages:");
            ImGui.SameLine();
            _changes |= ImGui.Checkbox($"##alertIncludeHidden{idx}", ref alert.IncludeHidden);
        }

        private void DrawChannels(Alert alert, int idx)
        {
            AlignLabel("Channels:");
            var channelListStr = alert.Channels.Contains(XivChatType.None)
                ? "All"
                : string.Join(", ",
                    alert.Channels.Where(c => c != XivChatType.CrossParty).Select(c => c.GetDetails()?.FancyName ?? c.ToString()));
            if (!ImGui.BeginCombo($"##alertChannels{idx}", channelListStr, ImGuiComboFlags.HeightLarge))
                return;

            {
                ImGui.Columns(3);
                using var raii = ImGuiRaii.DeferredEnd(ImGui.EndCombo)
                    .Push(ImGui.Columns);

                foreach (var chatType in (XivChatType[]) Enum.GetValues(typeof(XivChatType)))
                {
                    if (chatType == XivChatType.CrossParty || chatType == XivChatType.Debug || chatType == XivChatType.Urgent)
                        continue;

                    var name = chatType == XivChatType.None ? "All" : chatType.GetDetails()?.FancyName ?? chatType.ToString();
                    var e    = alert.Channels.Contains(chatType);

                    if (ImGui.Checkbox($"{name}##alertChannelOption{(ushort) chatType}", ref e))
                    {
                        alert.Channels.RemoveAll(c => c == chatType || c == XivChatType.CrossParty && chatType == XivChatType.Party);
                        if (e)
                        {
                            alert.Channels.Add(chatType);
                            if (chatType == XivChatType.Party)
                                alert.Channels.Add(XivChatType.CrossParty);
                        }

                        alert.Channels.Sort();
                        _changes = true;
                    }

                    ImGui.NextColumn();
                    if (chatType == XivChatType.None && e)
                        break;
                }
            }
        }

        private void DrawHighlightCombo(Alert alert, int idx, bool which)
        {
            var label = which ? $"##alertHighlightCombo{idx}" : $"##alertGlowCombo{idx}";
            if (!ImGui.BeginCombo(label, "", ImGuiComboFlags.NoPreview))
                return;

            using var raii = ImGuiRaii.DeferredEnd(ImGui.EndCombo);

            var       counter   = 0;
            using var colorRaii = new ImGuiRaii.Color();
            var       colors    = which ? _foregroundColors : _glowColors;
            foreach (var (id, color) in colors)
            {
                colorRaii.Push(ImGuiCol.ChildBg, color);
                ImGui.BeginChild($"{label}_{id}", _colorPreviewSize, true);
                ImGui.EndChild();
                colorRaii.Pop();
                if (ImGui.IsItemClicked())
                {
                    if (which)
                        alert.HighlightForeground = id;
                    else
                        alert.HighlightGlow = id;
                    _changes = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGuiCustom.HoverTooltip($"{id}");
                if (++counter % 10 != 0)
                    ImGui.SameLine();
            }
        }

        private void DrawGlow(Alert alert, int idx)
        {
            ImGui.Text("Glow Colour:");
            ImGui.SameLine();

            var glowColor = _glowColors.TryGetValue(alert.HighlightGlow, out var c) ? c : Vector4.UnitZ;

            using var colorRaii = ImGuiRaii.PushColor(ImGuiCol.ChildBg, glowColor);
            ImGui.BeginChild("##alertGlowPreview", _colorPreviewSize, true);
            ImGui.EndChild();
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                alert.HighlightGlow = 0;
                _changes            = true;
            }

            colorRaii.Pop();
            ImGui.SameLine();

            DrawHighlightCombo(alert, idx, false);
        }

        private void DrawHighlights(Alert alert, int idx)
        {
            AlignLabel("Highlight:");
            _changes |= ImGui.Checkbox($"##alertHighlight{idx}", ref alert.Highlight);
            if (!alert.Highlight)
                return;

            ImGui.SameLine();
            ImGui.Text("Text Colour:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(600 * _scale);

            var foregroundColor = _foregroundColors.TryGetValue(alert.HighlightForeground, out var c) ? c : Vector4.UnitZ;

            using var colorRaii = ImGuiRaii.PushColor(ImGuiCol.ChildBg, foregroundColor);
            ImGui.BeginChild("##alertHighlightPreview", _colorPreviewSize, true);
            ImGui.EndChild();
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                alert.HighlightForeground = 0;
                _changes                  = true;
            }

            colorRaii.Pop();
            ImGui.SameLine();

            DrawHighlightCombo(alert, idx, true);

            ImGui.SameLine();
            DrawGlow(alert, idx);
        }

        private void DrawNewAlertButton()
        {
            ImGuiHelpers.ScaledDummy(10, 10);
            ImGui.SetCursorPosX(50 * _scale);
            if (ImGui.Button("Add Alert", ImGuiHelpers.ScaledVector2(-50, 24)))
            {
                var a = new Alert();
                a.Channels.Add(XivChatType.Say);
                a.Channels.Add(XivChatType.Shout);
                a.Channels.Add(XivChatType.Yell);
                a.Channels.Add(XivChatType.Party);
                a.Channels.Add(XivChatType.Alliance);
                a.Channels.Add(XivChatType.FreeCompany);
                a.Update();
                ChatAlerts.Config.Alerts.Add(a);
                _changes = true;
            }
        }

        private void HandleActions()
        {
            if (_changedAlert == null || _action == ConfigAction.None)
                return;

            _changes = true;
            var alerts = ChatAlerts.Config.Alerts;
            switch (_action)
            {
                case ConfigAction.Delete:
                {
                    ChatAlerts.Config.Alerts.Remove(_changedAlert);
                    _changedAlert.Dispose();
                    break;
                }
                case ConfigAction.MoveUp:
                {
                    var idx = alerts.IndexOf(_changedAlert);
                    if (idx <= 0)
                        break;

                    alerts.RemoveAt(idx);
                    alerts.Insert(idx - 1, _changedAlert);
                    break;
                }
                case ConfigAction.MoveDown:
                {
                    var idx = alerts.IndexOf(_changedAlert);
                    if (idx < 0 || idx >= alerts.Count - 1)
                        break;

                    alerts.RemoveAt(idx);
                    alerts.Insert(idx + 1, _changedAlert);
                    break;
                }
            }
        }

        private void HandleChanges()
        {
            HandleActions();
            if (_changes)
                ChatAlerts.Config.Save();
            ResetChange();
        }

        private void DrawCustomSound(Alert alert, int idx)
        {
            ImGui.SetNextItemWidth(150 * _scale);
            if (ImGui.SliderFloat($"##alertVolume{idx}", ref alert.Volume, 0, 1, $"Volume: {alert.Volume * 100:F1}%%"))
            {
                _changes = true;
                alert.UpdateAudio();
            }

            ImGui.SameLine();

            var       soundReady = alert.PlaySound && !alert.SoundReady();
            using var color      = ImGuiRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, soundReady);
            using var style      = ImGuiRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2, soundReady);

            ImGui.SetNextItemWidth(-(ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.X * 2));
            if (ImGui.InputTextWithHint($"##alertSoundFileInput{idx}", "Sound File Path", ref alert.SoundPath, 256))
            {
                _changes = true;
                alert.UpdateAudio();
            }

            ImGui.SameLine();
            using var font = ImGuiRaii.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Cross}##alertNoCustomSound{idx}", new Vector2(-1, ImGui.GetItemRectSize().Y)))
            {
                alert.CustomSound = false;
                _changedAlert     = alert;
                alert.UpdateAudio();
            }

            font.Pop();

            ImGuiCustom.HoverTooltip("Use game sounds");
        }

        private void DrawGameSound(Alert alert, int idx)
        {
            ImGui.SetNextItemWidth(150 * _scale);
            if (ImGui.BeginCombo($"##alertSoundCombo{idx}", alert.SoundEffect.ToName()))
            {
                using var raii = ImGuiRaii.DeferredEnd(ImGui.EndCombo);
                foreach (var se in _validSounds)
                {
                    if (!ImGui.Selectable($"{se.ToName()}##AlertSoundsCombo{idx}"))
                        continue;

                    _changes          = alert.SoundEffect != se;
                    alert.SoundEffect = se;
                    ChatAlerts.PlaySound.Play(se);
                    alert.UpdateAudio();
                }
            }

            ImGui.SameLine();
            if (!ImGui.Button("Custom"))
                return;

            _changes          = !alert.CustomSound;
            alert.CustomSound = true;
            alert.UpdateAudio();
        }

        private void DrawAudio(Alert alert, int idx)
        {
            AlignLabel("Audio Alert:");
            if (ImGui.Checkbox($"##alertPlaySound{idx}", ref alert.PlaySound))
            {
                alert.UpdateAudio();
                _changes = true;
            }

            if (!alert.PlaySound)
                return;

            ImGui.SameLine();

            using var font = ImGuiRaii.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Play.ToIconChar()}##alertSoundTest{idx}", new Vector2(ImGui.GetItemRectSize().Y)))
                alert.StartSound();
            font.Pop();
            ImGui.SameLine();
            if (alert.CustomSound)
                DrawCustomSound(alert, idx);
            else
                DrawGameSound(alert, idx);
        }

        public void Draw()
        {
            if (!Visible)
                return;

            _scale            = ImGuiHelpers.GlobalScale;
            _colorPreviewSize = ImGuiHelpers.ScaledVector2(24, 24);
            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(500, 300), new Vector2(float.MaxValue));

            using var colors = ImGuiRaii.PushColor(ImGuiCol.TitleBg, 0xAA993333)
                .Push(ImGuiCol.TitleBgActive, 0xFF993333)
                .Push(ImGuiCol.WindowBg,      0xEE000000);
            using var raii = ImGuiRaii.DeferredEnd(ImGui.End);
            ImGui.Begin(_chatAlertsHeader, ref Visible);
            colors.Pop(3);

            ImGui.Text("Create and configure chat alerts.\nAlerts are processed from top to bottom.");

            ImGui.Separator();

            var i = 0;
            foreach (var alert in ChatAlerts.Config.Alerts)
                DrawAlert(alert, i++);

            DrawNewAlertButton();

            HandleChanges();
        }
    }
}
