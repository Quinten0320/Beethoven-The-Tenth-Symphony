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

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private List<Rectangle> Blackkeys = new List<Rectangle>();
        private readonly PianoInputHandler _inputHandler;

        private readonly UitlezenMidiLogica uitlezenLogic;
        private FeedbackLogic _feedbackLogic;
        private readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;
        private DispatcherTimer _timer;

        public event Action<string> VirtualNotePressed;

        public PianoWindow(string midiPath)
        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement

            _inputHandler = PianoInputHandlerService.Instance;

            _inputHandler.NotePressed -= OnMidiNotePressed; //veiligheid, niet perse nodig
            _inputHandler.NotePressed += OnMidiNotePressed;

            UpdateMidiStatus();
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
                    double bpm = uitlezenLogic.BerekenBpm();

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
                _feedbackLogic.HandleNotePressed(note);
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


                    //Aanmaken feedbacklogic en data meegeven.
                    _feedbackLogic = new FeedbackLogic(notesToPlay, elapsedTime, _midiPath);

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

        private void StartAnimationForNote(string note, double duration, int octave)
        {
            Rectangle fallingNote;
            double animationDuration = 5 * (120 / uitlezenLogic.BerekenBpm());
            //Debug.WriteLine($"Duur: {animationDuration}");

            // Bereken de werkelijke duur van de noot in seconden
            double actualduration = (duration / uitlezenLogic.GetTicksPerBeat()) * (60 / uitlezenLogic.BerekenBpm());

            //Geef animatie gegeves mee aan feedbacklogic
            _feedbackLogic.AnimationDuration = animationDuration;
            _feedbackLogic.AcutalDuration = actualduration;

            // Zoek de rechthoek die overeenkomt met de noot
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>()
                .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}{octave}");

            if (targetKey == null)
            {
                Debug.WriteLine($"Noot {note}{octave} niet gevonden");
                return;
            } // Als de noot niet wordt gevonden, stop de methode

            // Bereken de breedte en positie van de doeltoets
            double keyWidth = targetKey.Width;
            double keyLeft = Canvas.GetLeft(targetKey);

            // Bereken de hoogte van de vallende noot op basis van de duur en de hoogte van het canvas
            double noteHeight = (actualduration / animationDuration) * PianoCanvas.ActualHeight;

            // Creëer de rechthoek voor de vallende noot
            if (Blackkeys.Contains(targetKey))
            {
                fallingNote = new Rectangle
                {
                    Width = keyWidth,  // De breedte van de vallende noot is een derde van de breedte van de doeltoets
                    Height = noteHeight,
                    Fill = Brushes.Black,
                    Stroke = Brushes.Red,
                };
            }
            else
            {
                fallingNote = new Rectangle
                {
                    Width = keyWidth,  // De breedte van de vallende noot is een derde van de breedte van de doeltoets
                    Height = noteHeight,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Red,

                };
            }

            // Plaats de vallende noot boven de doeltoets
            Canvas.SetLeft(fallingNote, keyLeft);
            Canvas.SetTop(fallingNote, 0);
            PianoCanvas.Children.Add(fallingNote);

            // Creëer de animatie om de vallende noot van boven naar beneden te bewegen
            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = 0,
                To = PianoCanvas.ActualHeight,
                Duration = new Duration(TimeSpan.FromSeconds(animationDuration)),  // De animatieduur is altijd 5 seconden
                FillBehavior = FillBehavior.Stop
            };

            // Verwijder de vallende noot van het canvas zodra de animatie is voltooid
            fallAnimation.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);

            // Start de animatie
            fallingNote.BeginAnimation(Canvas.TopProperty, fallAnimation);
        }
    }
}