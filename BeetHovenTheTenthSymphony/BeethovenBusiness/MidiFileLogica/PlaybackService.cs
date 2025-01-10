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

        //initialiseert de playback met een midifile
        public PlaybackService(MidiFile midiFile)
        {
            _outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
            _playback = midiFile.GetPlayback(_outputDevice);
        }

        //start de playback
        public void Start()
        {
            _playback.Start();
        }

        //stopt de playback
        public void Stop()
        {
            _playback.Stop();
        }

        //verwijdert de playback
        public void Dispose()
        {
            _playback?.Dispose();
            _outputDevice?.Dispose();
        }

        //verplaatst de playback naar een bepaalde tijd
        public void MoveToTime(MetricTimeSpan time)
        {
            _playback.MoveToTime(time);
        }

        public bool IsRunning => _playback.IsRunning;

        //event dat aangeeft dat de playback klaar is
        public event EventHandler Finished
        {
            add { _playback.Finished += value; }
            remove { _playback.Finished -= value; }
        }
    }
}