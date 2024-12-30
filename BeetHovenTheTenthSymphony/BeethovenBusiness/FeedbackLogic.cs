using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Diagnostics;
using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.MusicTheory;

namespace BeethovenBusiness
{
    public class FeedbackLogic
    {
        private readonly PianoInputHandler _inputHandler;
        private List<Melanchall.DryWetMidi.Interaction.Note> ?_notes;
        private HashSet<Melanchall.DryWetMidi.Interaction.Note> _processedNotes = new();
        public double _elapsedTime;
        private string _midiFilePath;
        private double _animationDuration = 4;
        private double _actualDuration;
        private Stopwatch _timer;
        private UitlezenMidiLogica _uitlezenMidiLogica;

        public double AnimationDuration
        {
            get { return _animationDuration; }
            set { _animationDuration = value; }
        }

        public double AcutalDuration
        {
            get { return _actualDuration; }
            set { _actualDuration = value; }
        }


        public FeedbackLogic(
            UitlezenMidiLogica uitlezenMidiLogica
            )
        {
            _notes = new List<Melanchall.DryWetMidi.Interaction.Note>();
            _uitlezenMidiLogica = uitlezenMidiLogica;
            _inputHandler = PianoInputHandlerService.Instance;
            _inputHandler.NotePressed += HandleNotePressed;
        }

        //TODO private maken
        public void HandleNotePressed(string note)
        {
            double currentTime = _elapsedTime;

            foreach (Melanchall.DryWetMidi.Interaction.Note noteToCheck in _notes)
            {
                if (_processedNotes.Contains(noteToCheck)) continue;

                string noteName = noteToCheck.NoteName.ToString() + noteToCheck.Octave.ToString();
                if (noteName == note)
                {
                    _processedNotes.Add(noteToCheck);
                    CheckNoteTiming(noteToCheck, currentTime);
                }
            }
        }

        public void updateNotestoplay(List<Melanchall.DryWetMidi.Interaction.Note> notes, double elapsedtime)
        {
            _elapsedTime = elapsedtime;
            foreach (Melanchall.DryWetMidi.Interaction.Note note in notes)
            {
                _notes.Add(note);
            }
        }

        private void CheckNoteTiming(Melanchall.DryWetMidi.Interaction.Note noteToCheck, double pressTime)
        {
            long noteTimeInTicks = noteToCheck.Time;

            MetricTimeSpan metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, _uitlezenMidiLogica.tempoMap);
            double noteTimeInSeconds = metricTime.TotalSeconds;

            // Bereken de afwijking
            double difference = pressTime - noteTimeInSeconds - 4;
            

            //Debug.WriteLine($"{noteTimeInSeconds}, {pressTime}, {noteToCheck.LengthAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds}");

            // Controleer of de timing binnen een acceptabele marge valt
            //const double tolerance = 0.15; // 150 ms tolerantie
            const double tolerance = 0.5; // 150 ms tolerantie
            if (difference >= -tolerance && difference <= tolerance)
            {
                Debug.WriteLine("Timing correct! Afwijking: " + difference + " seconden." + " seconden:" + noteTimeInSeconds + " presstime:" + pressTime);
            }
            else if (pressTime < noteTimeInSeconds)
            {
                Debug.WriteLine("Te vroeg! Afwijking: " + difference + " seconden." + " seconden:" + noteTimeInSeconds + " presstime:" + pressTime);
            }
            else
            {
                Debug.WriteLine("Te laat! Afwijking: " + difference + " seconden." + " seconden:" + noteTimeInSeconds + " presstime:" + pressTime);
            }
        }

    }
}