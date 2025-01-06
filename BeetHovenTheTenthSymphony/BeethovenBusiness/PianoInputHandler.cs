using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Linq;
using System.Diagnostics;

namespace BeethovenBusiness
{
    public class PianoInputHandler
    {
        private InputDevice _midiDevice;

        public event Action<string> NotePressed;
        public event Action<string> NoteReleased;


        public PianoInputHandler()
        {
            //InitializeMidiInput();
        }

        public bool IsMidiDeviceConnected => _midiDevice != null;

        public void InitializeMidiInput()
        {
            Dispose();

            _midiDevice = InputDevice.GetAll().FirstOrDefault();

            try
            {
                if (_midiDevice == null)
                {
                    throw new InvalidOperationException("No MIDI input devices found.");
                }

                _midiDevice.EventReceived += OnMidiEventReceived;
                _midiDevice.StartEventsListening();
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("No MIDI input devices found.");
            }
        }

        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {

            if (e.Event is NoteOnEvent noteOnEvent)
            {
                string note = ConvertNoteToNameAndOctave(noteOnEvent.NoteNumber);

                if (noteOnEvent.Velocity > 1)
                {
                    Debug.WriteLine($"Event received: {e.Event}");
                    NotePressed?.Invoke(note);
                } else
                {
                    Debug.WriteLine($"Note off: {noteOnEvent.NoteNumber}");
                    NoteReleased?.Invoke(note);
                }
            }
        }

        private string ConvertNoteToNameAndOctave(int noteNumber)
        {
            string[] noteNames = { "C", "CSharp", "D", "DSharp", "E", "F", "FSharp", "G", "GSharp", "A", "ASharp", "B" };

            string noteName = noteNames[noteNumber % 12];
            int octave = (noteNumber / 12) - 1;

            return $"{noteName}{octave}";
        }

        public void Dispose()
        {
            if (_midiDevice != null)
            {
                _midiDevice.StopEventsListening();
                _midiDevice.EventReceived -= OnMidiEventReceived;
                _midiDevice.Dispose();
                _midiDevice = null;
            }
        }
    }

}