using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Linq;

namespace BeethovenBusiness
{
    public class PianoInputHandler : IDisposable
    {
        private InputDevice _midiDevice;

        public event Action<string> NotePressed;

        public PianoInputHandler()
        {
            InitializeMidiInput(); //search for midi input on startup
        }

        public void InitializeMidiInput()
        {
            //als er al een midi device is
            if (_midiDevice != null)
            {
                Console.WriteLine("A MIDI input device is already connected and listening.");
                return;
            }

            Dispose();
            _midiDevice = InputDevice.GetAll().FirstOrDefault();

            //als er geen midi device te vinden is
            if (_midiDevice == null)
            {
                Console.WriteLine("No MIDI input device detected.");
                return;
            }

            //connect met midi
            try
            {
                _midiDevice.EventReceived += OnMidiEventReceived;
                _midiDevice.ErrorOccurred += OnMidiDeviceError;
                _midiDevice.StartEventsListening();
                Console.WriteLine("MIDI input device connected successfully!.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error initializing MIDI device: {e.Message}");
                Dispose();
                throw new InvalidOperationException("Failed to initialize MIDI device.", e);
            }
        }

        public void Dispose()
        {
            if (_midiDevice != null)
            {
                _midiDevice.StopEventsListening();
                _midiDevice.EventReceived -= OnMidiEventReceived;
                _midiDevice.ErrorOccurred -= OnMidiDeviceError;
                _midiDevice = null;
                Console.WriteLine("MIDI device disposed.");
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

        private void OnMidiDeviceError(object sender, ErrorOccurredEventArgs e)
        {
            Console.WriteLine($"MIDI device error: {e.Exception.Message}");
            //InitializeMidiInput();
        }

        private string ConvertNoteToNameAndOctave(int noteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string noteName = noteNames[noteNumber % 12];
            int octave = (noteNumber / 12) - 1;
            return $"{noteName}{octave}";
        }
    }
}
