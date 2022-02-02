using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;

namespace ChatAlerts
{
    public class ChatWatcher : IDisposable
    {
        private readonly SortedSet<XivChatType> _watchedChannels = new();
        private          bool                   _watchAllChannels;

        private static List<Alert> Alerts
            => ChatAlerts.Config.Alerts;

        public ChatWatcher()
        {
            UpdateAllAlerts();
            Dalamud.Chat.CheckMessageHandled += OnCheckMessageHandled;
            Dalamud.Chat.ChatMessage         += OnChatMessage;
        }

        internal void UpdateAllAlerts()
        {
            _watchedChannels.Clear();
            _watchAllChannels = false;

            foreach (var alert in Alerts)
                UpdateAlert(alert);

            PluginLog.Debug($"Watching Channels: {(_watchAllChannels ? "All" : string.Join(", ", _watchedChannels))}");
        }

        internal void UpdateAlert(Alert alert)
        {
            alert.Update();
            _watchAllChannels |= alert.Channels.Contains(XivChatType.None);
            if (!_watchAllChannels)
                _watchedChannels.UnionWith(alert.Channels);
        }

        public void Dispose()
        {
            Dalamud.Chat.CheckMessageHandled -= OnCheckMessageHandled;
            Dalamud.Chat.ChatMessage         -= OnChatMessage;
        }

        private static void CopySublist(IReadOnlyList<Payload> payloads, List<Payload> newPayloads, int from, int to)
        {
            while (from < to)
                newPayloads.Add(payloads[from++]);
        }

        private static bool HandleAlert(Alert alert, List<Payload> payloads, out List<Payload> newPayloads)
        {
            newPayloads = payloads;
            var            lastCopiedPayload = 0;
            List<Payload>? ret               = null;
            var            match             = false;
            for (var payload = 0; payload < payloads.Count; ++payload)
            {
                if (payloads[payload] is not TextPayload tp)
                    continue;

                var oldIdx = 0;
                var idx    = alert.Match(tp.Text ?? string.Empty, oldIdx);
                if (idx.From < 0)
                    continue;

                match = true;
                if (!alert.Highlight)
                    return true;

                ret ??= new List<Payload>(payloads.Count + 6);

                CopySublist(payloads, ret!, lastCopiedPayload, payload);
                lastCopiedPayload = payload + 1;

                do
                {
                    var preString   = tp.Text.Substring(oldIdx,   idx.From - oldIdx);
                    var matchString = tp.Text.Substring(idx.From, idx.Length);
                    oldIdx = idx.From + idx.Length;

                    if (preString.Length > 0)
                        ret.Add(new TextPayload(preString));
                    if (alert.HighlightForeground != 0)
                        ret.Add(new UIForegroundPayload(alert.HighlightForeground));
                    if (alert.HighlightGlow != 0)
                        ret.Add(new UIGlowPayload(alert.HighlightGlow));
                    ret.Add(new TextPayload(matchString));
                    if (alert.HighlightForeground != 0)
                        ret.Add(UIForegroundPayload.UIForegroundOff);
                    if (alert.HighlightGlow != 0)
                        ret.Add(UIGlowPayload.UIGlowOff);

                    idx = alert.Match(tp.Text, oldIdx);
                } while (idx.From >= 0);

                if (oldIdx < tp.Text.Length)
                    ret.Add(new TextPayload(tp.Text.Substring(oldIdx)));
            }

            if (ret != null)
            {
                CopySublist(payloads, ret, lastCopiedPayload, payloads.Count);
                newPayloads = ret;
            }

            return match;
        }

        private void HandleMessage(XivChatType type, ref SeString sender, ref SeString message, bool preFilter)
        {
            if (!(_watchAllChannels || _watchedChannels.Contains(type)))
                return;

            var soundPlayed = false;
            foreach (var alert in Alerts.Where(a => a.Enabled
             && a.CanMatch()
             && a.IncludeHidden == preFilter
             && (a.Channels.Contains(XivChatType.None) || a.Channels.Contains(type))))
            {
                var payloads   = alert.SenderAlert ? sender.Payloads : message.Payloads;
                var alertMatch = HandleAlert(alert, payloads, out payloads);
                if (alert.SenderAlert)
                    sender = new SeString(payloads);
                else
                    message = new SeString(payloads);
                if (alertMatch && !soundPlayed)
                    soundPlayed = alert.StartSound();
            }
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            HandleMessage(type, ref sender, ref message, false);
        }

        private void OnCheckMessageHandled(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            HandleMessage(type, ref sender, ref message, true);
        }
    }
}
