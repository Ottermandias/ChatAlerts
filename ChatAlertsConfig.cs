using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace ChatAlerts
{
    public class ChatAlertsConfig : IPluginConfiguration, IDisposable
    {
        public int         Version { get; set; } = 2;
        public List<Alert> Alerts = new();

        public void Save()
            => Dalamud.PluginInterface.SavePluginConfig(this);

        public static ChatAlertsConfig Load()
        {
            if (Dalamud.PluginInterface.GetPluginConfig() is ChatAlertsConfig config)
                return config;

            config = new ChatAlertsConfig();
            config.Save();
            return config;
        }

        public void Dispose()
        {
            foreach (var configAlert in Alerts)
                configAlert.Dispose();
        }
    }
}
