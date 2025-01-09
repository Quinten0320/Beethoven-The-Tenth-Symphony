using BeethovenBusiness.Interfaces;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace BeethovenBusiness.MidiFileLogica
{
    public class PlaybackService : IPlayback
    {
        private Playback _playback;
        private OutputDevice _outputDevice;

        public PlaybackService(MidiFile midiFile)
        {
            _outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
            _playback = midiFile.GetPlayback(_outputDevice);
        }

        public void Start()
        {
            _playback.Start();
        }

        public void Stop()
        {
            _playback.Stop();
        }

        public void Dispose()
        {
            _playback?.Dispose();
            _outputDevice?.Dispose();
        }

        public void MoveToTime(MetricTimeSpan time)
        {
            _playback.MoveToTime(time);
        }

        public bool IsRunning => _playback.IsRunning;

        public event EventHandler Finished
        {
            add { _playback.Finished += value; }
            remove { _playback.Finished -= value; }
        }
    }
}