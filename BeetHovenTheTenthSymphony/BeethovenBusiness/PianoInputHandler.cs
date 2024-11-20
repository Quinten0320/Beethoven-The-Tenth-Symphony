using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Linq;

namespace BeethovenBusiness
{
    public class PianoInputHandler
    {
        private InputDevice _midiDevice;

        //event that will be triggered when a note is pressed
        public event Action<string> NotePressed;

        public PianoInputHandler()
        {
            InitializeMidiInput();
        }

        private void InitializeMidiInput()
        {
            //select the first available MIDI input device
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
            catch(InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
            }
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
            _midiDevice?.Dispose();
        }
    }
}
