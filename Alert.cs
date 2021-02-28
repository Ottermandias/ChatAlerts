using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Plugin;
using NAudio.Wave;
using Newtonsoft.Json;

namespace ChatAlerts {
    public class Alert : IDisposable {
        public List<XivChatType> Channels = new();
        public string Name = "New Alert";
        public string Content = string.Empty;
        public bool Enabled = true;
        public bool IsRegex;
        public bool IgnoreCase = true;
        public bool CustomSound;
        public bool SenderAlert = false;
        public bool IncludeHidden = false;

        public bool Highlight = true;
        public uint HighlightForeground = 500;
        public uint HighlightGlow;

        public bool PlaySound;
        public SoundEffect SoundEffect = ChatAlerts.SoundEffect.SoundEffect2;
        public string SoundPath = string.Empty;
        public float Volume = 0.5f;

        [NonSerialized] public Regex CompiledRegex;
        [NonSerialized] private AudioFileReader audioFile;
        [NonSerialized] private WaveOutEvent audioEvent;

        [JsonIgnore] public bool SoundReady => audioFile != null && audioEvent != null;

        public void Update() {
            this.CompiledRegex = null;
            if (this.IsRegex) {
                try {
                    var regexOptions = RegexOptions.CultureInvariant;
                    if (this.IgnoreCase) regexOptions |= RegexOptions.IgnoreCase;
                    this.CompiledRegex = new Regex(this.Content, regexOptions);
                } catch {
                    this.CompiledRegex = null;
                }
            }

            if (PlaySound && CustomSound) {
                try {
                    if (audioFile?.FileName != SoundPath) {
                        audioEvent?.Dispose();
                        audioEvent = null;
                        audioFile?.Dispose();
                        audioFile = null;
                    }

                    audioFile = new AudioFileReader(SoundPath);
                    audioFile.Volume = Volume;
                    audioEvent = new WaveOutEvent();
                    audioEvent.Init(audioFile);
                } catch (Exception ex) {
                    PluginLog.Error(ex, "Error attempting to setup sound.");
                    audioEvent?.Dispose();
                    audioEvent = null;
                    audioFile?.Dispose();
                    audioFile = null;
                }
            } else {
                audioEvent?.Dispose();
                audioEvent = null;
                audioFile?.Dispose();
                audioFile = null;
            }
        }

        public bool StartSound(Plugin plugin) {
            if (!PlaySound) return false;

            if (CustomSound) {
                if (!PlaySound || audioEvent == null) return false;
                audioEvent?.Stop();
                audioFile.Position = 0;
                audioEvent?.Play();
                return true;
            } else {
                plugin.PlayGameSound(SoundEffect);

                return true;
            }
        }

        public void Dispose() {
            audioEvent?.Dispose();
            audioEvent = null;
            audioFile?.Dispose();
            audioFile = null;
        }
    }
}
