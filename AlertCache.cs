using System;
using System.Text.RegularExpressions;
using Dalamud.Logging;
using NAudio.Wave;

namespace ChatAlerts
{
    public class AlertCache : IDisposable
    {
        public  Regex?           CompiledRegex;
        private AudioFileReader? _audioFile;
        private WaveOutEvent?    _audioEvent;

        public bool SoundReady
            => _audioFile != null && _audioEvent != null;

        public bool PlaySound()
        {
            if (_audioFile == null || _audioEvent == null)
                return false;

            _audioEvent!.Stop();
            _audioFile!.Position = 0;
            _audioEvent!.Play();
            return true;
        }

        public void Dispose()
        {
            DisposeAudio();
            CompiledRegex = null;
        }

        public void UpdateRegex(Alert parent)
        {
            CompiledRegex = null;
            if (!parent.IsRegex)
                return;

            try
            {
                var regexOptions = RegexOptions.CultureInvariant;
                if (parent.IgnoreCase)
                    regexOptions |= RegexOptions.IgnoreCase;
                CompiledRegex = new Regex(parent.Content, regexOptions);
                if (CompiledRegex.Match(string.Empty).Success)
                    CompiledRegex = null;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error attempting to compile Regex.");
                CompiledRegex = null;
            }
        }

        private void DisposeAudio()
        {
            _audioFile?.Dispose();
            _audioFile = null;
            _audioEvent?.Dispose();
            _audioEvent = null;
        }

        public void UpdateAudio(Alert parent)
        {
            if (!(parent.PlaySound && parent.CustomSound))
            {
                DisposeAudio();
                return;
            }

            try
            {
                if (_audioFile?.FileName != parent.SoundPath)
                    DisposeAudio();

                _audioFile  = new AudioFileReader(parent.SoundPath) { Volume = parent.Volume };
                _audioEvent = new WaveOutEvent();
                _audioEvent.Init(_audioFile);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error attempting to setup sound.");
                DisposeAudio();
            }
        }

        public void Update(Alert parent)
        {
            UpdateRegex(parent);
            UpdateAudio(parent);
        }
    }
}
