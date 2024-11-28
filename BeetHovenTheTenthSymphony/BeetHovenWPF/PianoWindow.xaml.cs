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


namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private readonly PianoInputHandler _inputHandler;

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

            //try
            //{
            //    _inputHandler = new PianoInputHandler();
            //    _inputHandler.NotePressed += OnMidiNotePressed;
            //    Debug.WriteLine("MIDI handler successfully initialized.");
            //}
            //catch (Exception ex)
            //{
            //    //Debug.WriteLine($"MIDI initialization error: {ex.Message}");
            //    //MessageBox.Show($"Unable to initialize MIDI device: {ex.Message}", "MIDI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    _inputHandler = null; // Continue without MIDI
            //}
        }

        private void OnMidiNotePressed(string note)
        {
            //update textbox met laatste note input
            Dispatcher.Invoke(() => LastPressedNoteTextBox.Text = note);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _inputHandler?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disposing MIDI handler: {ex.Message}", "Dispose Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    DispatcherTimer timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
                    };
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading MIDI file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); //hertekend de piano bij venstergrootte-aanpassing
        }

        private void GeneratePiano()
        {
            //verwijder vorige keys
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
            //animatie speelt alleen af wanneer pianowindow open is (betere performance en geen console spam)
            bool isWindowOpen = Application.Current.Windows.OfType<PianoWindow>().Any(window => window.IsVisible);
            if(isWindowOpen)
            {
                try
                {
                    double elapsedTime = (DateTime.Now - _startTime).TotalSeconds;
                    var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedTime);

                    foreach (var note in notesToPlay)
                    {
                        StartAnimationForNote(note.NoteName.ToString(), note.Length, note.Octave);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout in timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /*
        ANIMATIE VOOR NOTEN HIERONDER
        */

        private void StartAnimationForNote(string note, double duration, int octave)
        {
            double animationDuration = 10;
            double actualNoteDuration = (duration / uitlezenLogic.GetTicksPerBeat()) * (60 / uitlezenLogic.BerekenBpm());

            //geef rechthoek die bij noot hoort bijbehorende tag
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>()
                .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}{octave}");

            //als noot niet wordt gevonden
            if (targetKey == null)
            {
                Debug.WriteLine($"Noot {note}{octave} niet gevonden");
                return;
            }

            //breedte doeltoets
            double keyWidth = targetKey.Width;
            double keyLeft = Canvas.GetLeft(targetKey);

            //hoogte vallende noot
            double noteHeight = (actualNoteDuration / animationDuration) * PianoCanvas.ActualHeight;
            Debug.WriteLine($"noothiehgt {noteHeight} duration {actualNoteDuration} animationduration {animationDuration}");

            //maak de vallende noot
            Rectangle fallingNote = new Rectangle
            {
                Width = keyWidth,
                Height = noteHeight * 100,
                Fill = Brushes.Blue
            };

            //plaats de vallende noot boven de doeltoets
            Canvas.SetLeft(fallingNote, keyLeft);
            Canvas.SetTop(fallingNote, 0);
            PianoCanvas.Children.Add(fallingNote);

            //animeer vallende noot
            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = 0,
                To = PianoCanvas.ActualHeight,
                Duration = new Duration(TimeSpan.FromSeconds(animationDuration)),  //de animatieduur is altijd 5 seconden
                FillBehavior = FillBehavior.Stop
            };

            //verwijder de vallende noot van het canvas zodra de animatie is voltooid
            fallAnimation.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);

            // Start de animatie
            fallingNote.BeginAnimation(Canvas.TopProperty, fallAnimation);
        }
    }
}
