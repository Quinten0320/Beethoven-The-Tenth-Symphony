using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Diagnostics;
using BeethovenDataAccesLayer;

namespace BeethovenBusiness
{
    public class FeedbackLogic
    {
        private readonly PianoInputHandler _inputHandler;
        private List<Melanchall.DryWetMidi.Interaction.Note> _notes;
        private HashSet<Melanchall.DryWetMidi.Interaction.Note> _processedNotes = new();
        private double _elapsedTime;
        private string _midiFilePath;
        private double _animationDuration = 4;
        private double _actualDuration;
        private Stopwatch _timer;
         
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
            List<Melanchall.DryWetMidi.Interaction.Note> notes, 
            double elapsedTime, 
            string midiFilePath)
        {
            _notes = notes;
            _elapsedTime = elapsedTime;
            _midiFilePath = midiFilePath;

            _timer = Stopwatch.StartNew();

            _inputHandler = PianoInputHandlerService.Instance;
            _inputHandler.NotePressed += HandleNotePressed;
        }

        private void HandleNotePressed(string note)
        {
            double currentTime = _elapsedTime;

            foreach (Melanchall.DryWetMidi.Interaction.Note noteToCheck in _notes)
            {

                if (_processedNotes.Contains(noteToCheck)) continue;

                string noteName = noteToCheck.NoteName.ToString() + noteToCheck.Octave.ToString();
                if (noteName == note)
                {
                    CheckNoteTiming(noteToCheck, currentTime);
                    _processedNotes.Add(noteToCheck);
                }
            }
        }


        private void CheckNoteTiming(Melanchall.DryWetMidi.Interaction.Note noteToCheck, double pressTime)
        {
            // Bereken het moment waarop de noot de doelzone bereikt
            double noteStartTime = noteToCheck.TimeAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds / 1_000_000.0;
            double noteEndTime = noteStartTime + noteToCheck.LengthAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds / 1_000_000.0;

            // Bereken de afwijking
            double difference = pressTime - noteStartTime;

            // Controleer of de timing binnen een acceptabele marge valt
            //const double tolerance = 0.15; // 150 ms tolerantie
            const double tolerance = 0.5; // 150 ms tolerantie
            if (difference >= -tolerance && difference <= tolerance)
            {
                Debug.WriteLine("Timing correct! Afwijking: " + difference + " seconden.");
            }
            else if (pressTime < noteStartTime)
            {
                Debug.WriteLine("Te vroeg! Afwijking: " + difference + " seconden.");
            }
            else
            {
                Debug.WriteLine("Te laat! Afwijking: " + difference + " seconden.");
            }
        }

    }
}
