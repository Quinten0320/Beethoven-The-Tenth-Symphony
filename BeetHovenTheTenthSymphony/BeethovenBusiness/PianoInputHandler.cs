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

        public PianoInputHandler()
        {
            InitializeMidiInput();
        }

        public void InitializeMidiInput()
        {
            Dispose(); // Dispose of the existing device if any

            _midiDevice = InputDevice.GetAll().FirstOrDefault();

            if (_midiDevice == null)
            {
                throw new InvalidOperationException("No MIDI input devices found.");
            }

            _midiDevice.EventReceived += OnMidiEventReceived;
            _midiDevice.StartEventsListening();
        }


        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is NoteOnEvent noteOnEvent)
            {
                string note = ConvertNoteToNameAndOctave(noteOnEvent.NoteNumber);

                NotePressed?.Invoke(note);
            }
        }

        private string ConvertNoteToNameAndOctave(int noteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

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
                Debug.WriteLine("MIDI device disposed.");
            }
        }
    }
}