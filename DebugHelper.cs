using System;
using ChatAlerts.SeFunctions;
using Dalamud.Hooking;
using Dalamud.Logging;

namespace ChatAlerts
{
    public class DebugHelper : IDisposable
    {
        public readonly Hook<PlaySoundDelegate>? PlaySoundHook;

        public DebugHelper()
            => PlaySoundHook = ChatAlerts.PlaySound.CreateHook(PlaySoundDetour);

        private ulong PlaySoundDetour(Sounds id, ulong a2, ulong a3)
        {
            var ret = PlaySoundHook!.Original(id, a2, a3);
            PluginLog.Debug($"Play Sound: {id} [{a2}, {a3}] => {ret}");
            return ret;
        }

        public void Dispose()
        {
            PlaySoundHook?.Dispose();
        }
    }
}
