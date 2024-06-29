using System;
using System.Collections.Generic;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace ChatAlerts
{
    public class Alert : IDisposable
    {
        public List<XivChatType> Channels  = new();
        public string            Name      = "New Alert";
        public string            Content   = string.Empty;
        public string            SoundPath = string.Empty;

        public float  Volume              = 0.5f;
        public ushort HighlightForeground = 500;
        public ushort HighlightGlow;
        public Sounds SoundEffect = Sounds.Sound02;

        public bool Enabled = true;
        public bool IsRegex;
        public bool IgnoreCase = true;
        public bool CustomSound;
        public bool SenderAlert   = false;
        public bool IncludeHidden = false;
        public bool Highlight     = true;
        public bool PlaySound;


        [NonSerialized]
        private readonly AlertCache _cache = new();

        public bool StartSound()
        {
            if (!PlaySound)
                return false;

            if (CustomSound)
                return _cache.PlaySound();

            UIModule.PlaySound((uint)SoundEffect);
            return true;
        }

        public bool SoundReady()
            => !CustomSound || _cache.SoundReady;

        public bool CanMatch()
            => Content.Length > 0 && (!IsRegex || _cache.CompiledRegex != null);

        public (int From, int Length) Match(string text, int startIdx)
        {
            if (IsRegex)
            {
                var match = _cache.CompiledRegex!.Match(text, startIdx);
                return match.Success ? (match.Index, match.Length) : (-1, 0);
            }

            var comparison = IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            var idx        = text.IndexOf(Content, startIdx, comparison);
            return idx >= 0 ? (idx, Content.Length) : (-1, 0);
        }

        public void Update()
            => _cache.Update(this);

        public void UpdateRegex()
            => _cache.UpdateRegex(this);

        public void UpdateAudio()
            => _cache.UpdateAudio(this);

        public void Dispose()
            => _cache.Dispose();
    }
}
