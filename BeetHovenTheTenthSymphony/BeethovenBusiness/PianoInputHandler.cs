using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;


namespace BeethovenBusiness
{
    public class PianoInputHandler : IDisposable
    {
        private InputDevice _midiDevice;

        public event Action<string> NotePressed;

        public PianoInputHandler()
        {
            InitializeMidiInput();
        }

        public void InitializeMidiInput()
        {

            Dispose();

            _midiDevice = InputDevice.GetAll().FirstOrDefault();

            if (_midiDevice == null)
            {
                throw new InvalidOperationException("No MIDI input device detected.");
            }

            try
            {
                Debug.WriteLine("MIDI input device connected successfully.");
                _midiDevice.EventReceived += OnMidiEventReceived;
                _midiDevice.ErrorOccurred += OnMidiDeviceError;
                _midiDevice.StartEventsListening();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error initializing MIDI device: {e.Message}");
                Dispose(); 
                throw new InvalidOperationException("Failed to initialize MIDI device.", e);
            }
        }

        public void Dispose()
        {
            if (_midiDevice != null)
            {
                try
                {
                    _midiDevice.StopEventsListening();

                    _midiDevice.EventReceived -= OnMidiEventReceived;
                    _midiDevice.ErrorOccurred -= OnMidiDeviceError;

                    _midiDevice = null;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error during MIDI device disposal: {ex.Message}");
                }
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
            throw new InvalidOperationException($"MIDI device error: {e.Exception.Message}");
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
