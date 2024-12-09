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
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.VisualBasic;

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {

        /*private List<double> fallPercentages = new List<double>();
        double totalDistance = PianoCanvas.ActualHeight + maxNoteHeight * 0.5;
        double distanceToBottom = PianoCanvas.ActualHeight - targetKey.Height;
        double fallPercentage = distanceToBottom / totalDistance;
        fallPercentages.Add(fallPercentage);*/



        private List<Rectangle> Blackkeys = new List<Rectangle>();
        private readonly PianoInputHandler _inputHandler;
        private double Fallpercentage;
        private readonly UitlezenMidiLogica uitlezenLogic;
        private readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;
        private DispatcherTimer _timer;
        private bool allesopgevraagd = true;
        long getmaxlength;
        long getgemiddeldelengte;

        public PianoWindow(string midiPath)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized; // Set window to fullscreen
            this.WindowStyle = WindowStyle.None;     // Remove the title bar and borders
            this.ResizeMode = ResizeMode.NoResize;  // Prevent resizing to enforce fullscreen

            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement

            _inputHandler = PianoInputHandlerService.Instance;

            _inputHandler.NotePressed -= OnMidiNotePressed; // Veiligheid, niet per se nodig
            _inputHandler.NotePressed += OnMidiNotePressed;

            UpdateMidiStatus();
        }

        private double BerekenFallPercentage(Rectangle targetKey, double maxNoteHeight)
        {
            double totalDistance = PianoCanvas.ActualHeight + maxNoteHeight * 0.5;
            double distanceToBottom = PianoCanvas.ActualHeight - targetKey.Height;
            double fallPercentage = distanceToBottom / totalDistance;
            return fallPercentage;
        }

        private void UpdateMidiStatus()
        {
            try
            {
                if (_inputHandler != null && _inputHandler.IsMidiDeviceConnected)
                {
                    MidiStatusTextBox.Text = "Connected";
                }
                else
                {
                    MidiStatusTextBox.Text = "Not Connected";
                }
            }
            catch (Exception ex)
            {
                MidiStatusTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private void OnMidiNotePressed(string note)
        {
            //update textbox met laatste not input
            Dispatcher.Invoke(() => LastPressedNoteTextBox.Text = note);
        }

        private void PianoWindow_Closing(object sender, CancelEventArgs e)
        {
            // Verwijder de oude midicontroller wanneer de pianowindow sluit (dit is nodig)
            _inputHandler.Dispose();

            // Stop de timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }
        }

        private void PianoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePiano();

            if (_midiPath != null)
            {
                try
                {
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    _startTime = DateTime.Now;
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
                    };
                    _timer.Tick += Timer_Tick;
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij initialisatie: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); //Herteken de piano bij venstergrootte-aanpassing
        }

        private void GeneratePiano()
        {
            // Remove existing piano keys
            var pianoNotes = PianoCanvas.Children.OfType<UIElement>()
                .Where(child => child is Rectangle rect && (rect.Tag is string tag && tag.StartsWith("PianoNote")))
                .ToList();

            foreach (var note in pianoNotes)
            {
                PianoCanvas.Children.Remove(note);
            }

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
                        Tag = $"PianoNote:{whiteNote}"
                    };

                    Canvas.SetLeft(whiteKey, currentX);
                    Canvas.SetBottom(whiteKey, 0);
                    Panel.SetZIndex(whiteKey, 0);
                    whiteKey.MouseDown += Key_MouseDown;
                    PianoCanvas.Children.Add(whiteKey);

                    if (i < _whiteKeyCount - 1 && whiteKeysWithBlack.Contains(GetWhiteKeyName(i)))
                    {
                        string blackNote = GetWhiteKeyName(i) + "Sharp" + (octave + 1);

                        Rectangle blackKey = new Rectangle
                        {
                            Width = blackKeyWidth,
                            Height = blackKeyHeight,
                            Fill = Brushes.Black,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1,
                            Tag = $"PianoNote:{blackNote}"
                        };

                        Canvas.SetLeft(blackKey, currentX + whiteKeyWidth * 0.75 - (blackKeyWidth / 2) + whiteKeyWidth * 0.25); Canvas.SetBottom(blackKey, whiteKeyHeight - blackKeyHeight);
                        Panel.SetZIndex(blackKey, 1);
                        blackKey.MouseDown += Key_MouseDown;
                        Blackkeys.Add(blackKey);
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
            bool isWindowOpen = Application.Current.Windows.OfType<PianoWindow>().Any(window => window.IsVisible);
            if (isWindowOpen)
            {
                try
                {
                    double elapsedTime = (DateTime.Now - _startTime).TotalSeconds;
                    var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedTime);
                    if (allesopgevraagd)
                    {
                        StartAnimationForNote("C", 0, 0);
                    }

                    foreach (var note in notesToPlay)
                    {
                        StartAnimationForNote(note.NoteName.ToString(), note.Length, note.Octave);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout in timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else
            {
                _timer.Stop();
            }
        }
        //private void StopAnimationForNote(string note, double length, int octave)
        //{
        //    var targetKey = PianoCanvas.Children
        //        .OfType<Rectangle>()
        //        .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}{octave}");
        //    Rectangle fallingNote;
        //    double bpm = uitlezenLogic.BerekenBpm();
        //    double AnimationDuration = 5 * (120 / bpm);
        //    double actualLength = (length / uitlezenLogic.GetTicksPerBeat()) * (60 / bpm); //lengte noot
        //    double noteHeight = (actualLength / AnimationDuration) * PianoCanvas.ActualHeight;

        //}

        private void StartAnimationForNote(string note, double length, int octave)
        {
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>()
                .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}{octave}");

            if (targetKey == null)
            {
                Debug.WriteLine($"Noot {note}{octave} niet gevonden");
                return;
            }
            if (allesopgevraagd)
            {
                getmaxlength = uitlezenLogic.getMaxLength();
                getgemiddeldelengte = uitlezenLogic.BerekenGemiddeldeLengte();
            }

            double bpm = uitlezenLogic.BerekenBpm();
            double baseAnimationDuration = 5 * (120 / bpm);
            double actualLength = (length / uitlezenLogic.GetTicksPerBeat()) * (60 / bpm);
            double maxLength = (getmaxlength / uitlezenLogic.GetTicksPerBeat()) * (60 / bpm);
            double noteHeight = (actualLength / baseAnimationDuration) * PianoCanvas.ActualHeight;
            double maxNoteHeight = (maxLength / baseAnimationDuration) * PianoCanvas.ActualHeight;

            
                Rectangle fallingNote = new Rectangle
                {
                    Width = targetKey.Width,
                    Height = noteHeight,
                    Fill = Blackkeys.Contains(targetKey) ? Brushes.Black : Brushes.Blue,
                    Stroke = Brushes.Red,
                };
            if (!allesopgevraagd)
            {
                Canvas.SetLeft(fallingNote, Canvas.GetLeft(targetKey));
                Canvas.SetBottom(fallingNote, PianoCanvas.ActualHeight);
                PianoCanvas.Children.Add(fallingNote);
            }

            double gemiddeldeLengte = (((getgemiddeldelengte / uitlezenLogic.GetTicksPerBeat()) * (60 / bpm)) / baseAnimationDuration) * PianoCanvas.ActualHeight;
            double adjustedAnimationDuration = baseAnimationDuration + (maxNoteHeight / gemiddeldeLengte);
            adjustedAnimationDuration = Math.Max(1, Math.Min(adjustedAnimationDuration, 10));
            
            if (allesopgevraagd)
            {
                allesopgevraagd = false;
                Fallpercentage = BerekenFallPercentage(targetKey, maxNoteHeight);
                uitlezenLogic.fallPercentage = Fallpercentage;
                uitlezenLogic.animationDurationUitlezenMidiLogica = adjustedAnimationDuration;
                return;
            }

            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = PianoCanvas.ActualHeight + maxNoteHeight * 0.5,
                To = -maxNoteHeight,
                Duration = new Duration(TimeSpan.FromSeconds(adjustedAnimationDuration)),
                FillBehavior = FillBehavior.Stop
            };

            fallAnimation.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);
            fallingNote.BeginAnimation(Canvas.BottomProperty, fallAnimation);
        }
    }
}