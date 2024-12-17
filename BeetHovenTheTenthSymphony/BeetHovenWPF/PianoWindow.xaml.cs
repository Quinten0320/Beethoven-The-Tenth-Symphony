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
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;

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
        public readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;
        private DispatcherTimer _timer;
        private bool allesopgevraagd = true;
        private readonly MidiFile _currentMidi;
        private OutputDevice _outputDevice;
        private Playback _playback;
        long getmaxlength;
        long getgemiddeldelengte;
        double elapsedTime;
        bool muziekafspelen = true;

        //bool patatje = true;
        //bool patatje2 = true;
        //double patatje4 = 0;

        private List<Storyboard> activeAnimations = new List<Storyboard>();
        private DateTime _pauseStartTime;
        private TimeSpan _totalPauseDuration = TimeSpan.Zero;
        private bool _isPaused = false;

        public PianoWindow(string midiPath, MidiFile midiFile)
        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement

            _inputHandler = PianoInputHandlerService.Instance;

            _currentMidi = midiFile;
            _outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
            _playback = midiFile.GetPlayback(_outputDevice);
            

            _inputHandler.NotePressed -= OnMidiNotePressed; //veiligheid, niet perse nodig
            _inputHandler.NotePressed += OnMidiNotePressed;
            this.KeyDown += PianoWindowPauze;
            _playback.Finished += OnPlaybackFinished;

            this.WindowState = WindowState.Maximized; // Set window to fullscreen
            this.WindowStyle = WindowStyle.None;     // Remove the title bar and borders
            this.ResizeMode = ResizeMode.NoResize;  // Prevent resizing to enforce fullscreen

            UpdateMidiStatus();
        }

        private double BerekenFallPercentage(Rectangle targetKey, double maxNoteHeight)
        {
            double totalDistance = PianoCanvas.ActualHeight + maxNoteHeight * 0.5;
            double distanceToBottom = PianoCanvas.ActualHeight - targetKey.Height - targetKey.Height * 0.5;
            double fallPercentage = distanceToBottom / totalDistance;
            Debug.WriteLine($"Fallpercentage: {fallPercentage}");
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

        public void PianoWindow_Closing(object sender, CancelEventArgs e)
        {
            // Verwijder de oude midicontroller wanneer de pianowindow sluit (dit is nodig)
            _inputHandler.Dispose();
            StopAndDisposePlayback();

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
            _playback.Finished += OnPlaybackFinished;

            if (_midiPath != null)
            {
                try
                {
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    _startTime = DateTime.Now;
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120) 
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
            if (_isPaused)
            {
                // Timer is actief, maar logica wordt niet uitgevoerd als de applicatie is gepauzeerd.
                return;
            }

            bool isWindowOpen = Application.Current.Windows.OfType<PianoWindow>().Any(window => window.IsVisible);
            if (isWindowOpen)
            {
                try
                {

                    elapsedTime = (DateTime.Now - _startTime).TotalSeconds;
                    if (elapsedTime > 4 && muziekafspelen)
                    {
                        muziekafspelen = false;
                        _playback.Start();
                    }
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
            else
            {
                _timer.Stop();
            }
        }

        private void StartAnimationForNote(string note, long length, double octave)
        {
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>() 
                .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}{octave}");
            if (targetKey == null)
            {
                Debug.WriteLine($"Note {note}{octave} not found");
                return;
            }

            // Calculate the fall height and animation duration
            double animationDuration = 10;
            
            MetricTimeSpan noteInSeconds = TimeConverter.ConvertTo<MetricTimeSpan>(length, uitlezenLogic.tempoMap);
            double noteHeight = (noteInSeconds.TotalSeconds / animationDuration) * 2000 * 2;
            Debug.WriteLine($"NoteHeight: {noteHeight}");

            // Create the falling rectangle
            Rectangle fallingNote = new Rectangle
            {
                Width = targetKey.Width,
                Height = noteHeight,
                Fill = Blackkeys.Contains(targetKey) ? Brushes.Black : Brushes.Blue,
                Stroke = Brushes.Red
            };

            // Set the position of the falling note
            double left = Canvas.GetLeft(targetKey);
            double bottom = 2000;
            Canvas.SetLeft(fallingNote, left);
            Canvas.SetBottom(fallingNote, bottom);
            PianoCanvas.Children.Add(fallingNote);

            // Apply the OpacityMask to make the bottom of the note invisible and the top visible
            LinearGradientBrush opacityMask = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            opacityMask.GradientStops.Add(new GradientStop(Colors.Black, 0.0));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Black, 0.5));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Transparent, 0.5));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
            fallingNote.OpacityMask = opacityMask;

            // Add the animation
            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = 2000,
                To = -2000,
                Duration = new Duration(TimeSpan.FromSeconds(animationDuration)),
                FillBehavior = FillBehavior.Stop
            };
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fallAnimation, fallingNote);
            Storyboard.SetTargetProperty(fallAnimation, new PropertyPath("(Canvas.Bottom)"));
            storyboard.Children.Add(fallAnimation);
            storyboard.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);

            activeAnimations.Add(storyboard);
            storyboard.Begin();
        }

        private void PianoWindowPauze(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                TogglePause();
            }
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                //stop de timer en registreer de start van de pauze
                _timer?.Stop();
                _pauseStartTime = DateTime.Now;
               
                _playback.Stop();

                //pauzeer animaties
                foreach (var storyboard in activeAnimations)
                {
                    storyboard.Pause();
                }

                ShowPauseMenu(true);
            }
            else
            {
                //bereken de totale pauzeduur en pas deze toe op de starttijd
                _totalPauseDuration += DateTime.Now - _pauseStartTime;
                _startTime = _startTime.Add(_totalPauseDuration);

                //TimeSpan tijd = 1;
                //_playback.MoveToTime();
                _playback.Start();

                //reset de pauzeduur
                _totalPauseDuration = TimeSpan.Zero;

                //start de timer opnieuw en hervat animaties
                _timer?.Start();
                foreach (var storyboard in activeAnimations)
                {
                    storyboard.Resume();
                }

                ShowPauseMenu(false);
            }
        }

        public void ShowPauseMenu(bool show)
        {
            if (show)
            {
                PauseFrame.Visibility = Visibility.Visible; //toon de Frame
                PauseFrame.Navigate(new PauzeMenu(_currentMidi));       //navigeer naar PauzeMenu
            }
            else
            {
                PauseFrame.Content = null;                 //leeg de Frame
                PauseFrame.Visibility = Visibility.Collapsed; //verberg de Frame
            }
        }

        private async void OnPlaybackFinished(object sender, EventArgs e)
        {
            _timer?.Stop();
            _playback.Stop();

            await Task.Delay(3000); // Wait for 3 seconds

            Dispatcher.Invoke(() =>
            {
                // Close the current PianoWindow
                this.Close();

                // Show the End Menu in a new window
                var window = new Window
                {
                    Title = "End Menu",
                    Content = new EndMenu(_currentMidi),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    Width = ActualWidth,
                    Height = ActualHeight,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0, 0, 0)),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                window.ShowDialog();
            });
        }



        public void StopAndDisposePlayback()
        {
            if (_playback != null)
            {
                _playback.Stop(); // Stop playback
                _playback.Dispose(); // Dispose playback instance
                _playback = null; // Clear the reference
            }

            if (_outputDevice != null)
            {
                _outputDevice.Dispose(); // Dispose output device
                _outputDevice = null; // Clear the reference
            }
        }
    }
}