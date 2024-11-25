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

        public static DevicesWatcher Instance { get; }
        public event EventHandler<DeviceAddedRemovedEventArgs> DeviceAdded;
        public event EventHandler<DeviceAddedRemovedEventArgs> DeviceRemoved;

        public PianoInputHandler()
        {
            //Subscribe to device added and removed events
            InitializeMidiInput();
        }

        private void InitializeMidiInput()
        {
            Dispose();
            _midiDevice = InputDevice.GetAll().FirstOrDefault();

            if (_midiDevice == null)
            {
                Console.WriteLine("No MIDI device");
                return;
            }

            try
            {
                _midiDevice.EventReceived += OnMidiEventReceived;
                _midiDevice.ErrorOccurred += OnMidiDeviceError;
                _midiDevice.StartEventsListening();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error initializing MIDI device");
            }
        }

        private void OnDeviceAdded(object sender, DeviceAddedRemovedEventArgs e)
        {
            if (_midiDevice == null)
            {
                InitializeMidiInput();
            }
        }

        private void OnDeviceRemoved(object sender, DeviceAddedRemovedEventArgs e)
        {
            if (_midiDevice != null && _midiDevice.Equals(e.Device))
            {
                Dispose();
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

            DeviceAdded -= OnDeviceAdded;
            DeviceRemoved -= OnDeviceRemoved;
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
            InitializeMidiInput();
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
