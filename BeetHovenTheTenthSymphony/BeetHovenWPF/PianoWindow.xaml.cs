using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BeethovenBusiness;

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private readonly UitlezenMidiLogica uitlezenLogic;
        private readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;

        public PianoWindow(string midiPath)
        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
        }

        private void PianoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePiano(); // Creëer de piano

            if (_midiPath != null)
            {
                try
                {
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    double bpm = uitlezenLogic.BerekenBpm();
                    // Notitie over BPM wordt nu niet meer weergegeven in de NotesTextBox

                    _startTime = DateTime.Now;

                    DispatcherTimer timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
                    };
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij initialisatie: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); // Herteken de piano bij venstergrootte-aanpassing
        }

        private void GeneratePiano()
        {
            PianoCanvas.Children
                .OfType<Rectangle>()
                .Where(r => r.Tag?.ToString() == "White" || r.Tag?.ToString() == "Black")
                .ToList()
                .ForEach(r => PianoCanvas.Children.Remove(r));

            double canvasWidth = PianoCanvas.ActualWidth;
            double canvasHeight = PianoCanvas.ActualHeight;

            double whiteKeyWidth = canvasWidth / (_octaves * _whiteKeyCount);
            double whiteKeyHeight = canvasHeight / 3.2;
            double blackKeyWidth = whiteKeyWidth * 0.6;
            double blackKeyHeight = whiteKeyHeight * 0.6;

            string[] whiteKeysWithBlack = { "C", "D", "F", "G", "A" };
            double currentX = 0;

            for (int octave = 0; octave < _octaves; octave++)
            {
                for (int i = 0; i < _whiteKeyCount; i++)
                {
                    string whiteNote = GetWhiteKeyName(i) + (octave + 1);

                    Rectangle whiteKey = new Rectangle
                    {
                        Width = whiteKeyWidth,
                        Height = whiteKeyHeight,
                        Fill = Brushes.White,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Tag = whiteNote
                    };

                    Canvas.SetLeft(whiteKey, currentX);
                    Canvas.SetBottom(whiteKey, 0);
                    Panel.SetZIndex(whiteKey, 0);
                    whiteKey.MouseDown += Key_MouseDown;
                    PianoCanvas.Children.Add(whiteKey);

                    if (i < _whiteKeyCount - 1 && whiteKeysWithBlack.Contains(GetWhiteKeyName(i)))
                    {
                        string blackNote = GetWhiteKeyName(i) + "#" + (octave + 1);

                        Rectangle blackKey = new Rectangle
                        {
                            Width = blackKeyWidth,
                            Height = blackKeyHeight,
                            Fill = Brushes.Black,
                            Tag = blackNote
                        };

                        Canvas.SetLeft(blackKey, currentX + whiteKeyWidth * 0.75 - (blackKeyWidth / 2) + whiteKeyWidth * 0.25);
                        Canvas.SetBottom(blackKey, whiteKeyHeight - blackKeyHeight);
                        Panel.SetZIndex(blackKey, 1);
                        blackKey.MouseDown += Key_MouseDown;
                        PianoCanvas.Children.Add(blackKey);
                    }

                    currentX += whiteKeyWidth;
                }
            }
        }

        private string GetWhiteKeyName(int index)
        {
            string[] whiteKeyNames = { "C", "D", "E", "F", "G", "A", "B" };
            return whiteKeyNames[index];
        }

        private void Key_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle key)
            {
                string note = key.Tag?.ToString();
                LastPressedNoteTextBox.Text = note;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                double elapsedTime = (DateTime.Now - _startTime).TotalSeconds;
                var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedTime);

                if (notesToPlay.Count > 0)
                {
                    // Weergave van noten in de TextBox is verwijderd
                }

                foreach (var note in notesToPlay)
                {
                    StartAnimationForNote(note.NoteName.ToString(), note.Length, elapsedTime);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout in timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartAnimationForNote(string note, double duration, double elapsedTime)
        {
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>()
                .FirstOrDefault(r => r.Tag?.ToString() == note);

            if (targetKey == null) return;

            double keyWidth = targetKey.Width;
            double keyLeft = Canvas.GetLeft(targetKey);

            Rectangle fallingNote = new Rectangle
            {
                Width = keyWidth,
                Height = duration * 100, // Scale duration to height (adjust as needed)
                Fill = Brushes.Blue
            };

            Canvas.SetLeft(fallingNote, keyLeft);
            Canvas.SetTop(fallingNote, 0); // Start from the top
            PianoCanvas.Children.Add(fallingNote);

            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = PianoCanvas.ActualHeight,
                Duration = TimeSpan.FromSeconds(duration),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);

            fallingNote.BeginAnimation(Canvas.TopProperty, animation);
        }
    }
}
