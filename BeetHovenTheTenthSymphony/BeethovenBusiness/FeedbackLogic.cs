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
        private List<Melanchall.DryWetMidi.Interaction.Note>? _notes;
        private HashSet<Melanchall.DryWetMidi.Interaction.Note> _processedNotes = new();
        public double _elapsedTime = 0;
        private string _midiFilePath;
        private double _animationDuration = 4;
        private double _actualDuration;
        private Stopwatch _timer;
        private UitlezenMidiLogica _uitlezenMidiLogica;

        private int _correctNotes = 0;
        private int _earlyNotes = 0;
        private int _lateNotes = 0;
        private int _totalNotes = 0;
        private double _score = 0.0;
        private double _extraTime = 0;


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

        public object UitlezenMidiLogica { get; set; }

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
            List<Melanchall.DryWetMidi.Interaction.Note> notesToRemove = new List<Melanchall.DryWetMidi.Interaction.Note>();

            foreach (Melanchall.DryWetMidi.Interaction.Note noteToCheck in _notes)
            {
                string noteName = noteToCheck.NoteName.ToString() + noteToCheck.Octave.ToString();
                if (noteName == note)
                {
                    CheckNoteTiming(noteToCheck, currentTime);
                    notesToRemove.Add(noteToCheck);
                    break;
                }
            }

            foreach (Melanchall.DryWetMidi.Interaction.Note noteToRemove in notesToRemove)
            {
                _notes.Remove(noteToRemove);
            }
        }

        public void updateNotestoplay(List<Melanchall.DryWetMidi.Interaction.Note> notes, double elapsedtime, double extratime)
        {
            _elapsedTime = elapsedtime;
            _extraTime = extratime;
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

            pressTime -= 4.5;

            double difference = pressTime - noteTimeInSeconds;

            const double tolerance = 0.5; // 500 ms tolerantie
            _totalNotes++;

            if (Math.Abs(difference) <= tolerance)
            {
                double scoreIncrement = 100 * (1 - Math.Abs(difference) / tolerance); // Hoe dichterbij, hoe meer punten
                _correctNotes++;
                _score += scoreIncrement;
                Debug.WriteLine($"Timing correct! Afwijking: {difference:F3} seconden. Score +{scoreIncrement:F2}");
            }
            else if (pressTime < noteTimeInSeconds)
            {
                _earlyNotes++;
                Debug.WriteLine($"Te vroeg! Afwijking: {difference:F3} seconden.");
            }
            else
            {
                _lateNotes++;
                Debug.WriteLine($"Te laat! Afwijking: {difference:F3} seconden.");
            }
            NotifyScoreUpdated(); // Update de score na elke noot
        }

        public double GetScore()
        {
            return _score < 0 ? 0 : _score; // Zorg dat de score niet negatief wordt
        }

        public string GetPerformanceSummary()
        {
            return $"Correct: {_correctNotes}, Early: {_earlyNotes}, Late: {_lateNotes}, Total: {_totalNotes}, Score: {GetScore():F2}";
        }

        public event Action<double> ScoreUpdated; // Event om score-updates te versturen

        private void NotifyScoreUpdated()
        {

            ScoreUpdated?.Invoke(_score);
        }

        public void StartScoreSimulation()
        {
            // Timer die elke seconde de score verhoogt
            var timer = new System.Timers.Timer(1000); // 1000 ms = 1 seconde
            timer.Elapsed += (sender, e) =>
            {
                NotifyScoreUpdated(); // Update de UI
            };
            timer.Start();
        }

        public void SaveScoreToDatabase(string songTitle, double songDuration, string filePath)
        {
            try
            {
                int finalScore = (int)GetScore(); // Haal de finale score op
                Data.SaveScore(songTitle, songDuration, filePath, finalScore);
                Debug.WriteLine("Score succesvol opgeslagen in de database.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fout bij het opslaan van de score: {ex.Message}");
            }
        }

        public void OnSongFinished(string songTitle, double songDuration, string filePath)
        {
            Debug.WriteLine($"Lied afgelopen: {songTitle}, Duur: {songDuration}, Bestand: {filePath}");
            SaveScoreToDatabase(songTitle, songDuration, filePath);
        }



    }
}