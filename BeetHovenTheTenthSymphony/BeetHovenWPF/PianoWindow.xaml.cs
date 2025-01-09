using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using System.Data.SQLite;
using BeethovenBusiness.Checkpoints;
using BeethovenBusiness.PianoLogica;
using BeethovenBusiness.MidiFileLogica;
using BeethovenDataAccesLayer.DataBaseAcces;

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private List<Rectangle> Blackkeys = new List<Rectangle>();
        private readonly PianoInputHandler _inputHandler;
        private readonly UitlezenMidiLogica uitlezenLogic;
        public readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;
        private DispatcherTimer _timer;
        private readonly Data _data;
        public string _selectedMidiName;
        private Slider slider;
        private TextBlock sliderValue;
        private readonly MidiFile _currentMidi;
        private OutputDevice _outputDevice;
        private Playback _playback;
        double elapsedTime;
        bool muziekafspelen = true;
        private List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private const int MaxSegments = 5;
        private List<Rectangle> checkpointMarkers = new List<Rectangle>();
        private Dictionary<Checkpoint, DispatcherTimer> _checkpointTimers = new Dictionary<Checkpoint, DispatcherTimer>();
        private static string connectionString = @"Data Source=..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";
        private List<Storyboard> activeAnimations = new List<Storyboard>();
        private DateTime _pauseStartTime;
        private TimeSpan _totalPauseDuration = TimeSpan.Zero;
        private bool _isPaused = false;
        private FeedbackLogic _feedbacklogic;
        private Score _score;
        private double finalScore;
        private bool _startedPlayback = false;
        private double _selectedSongDuration;
        int songID;
        List<Checkpoint> CheckpointsForSong;
        private PlaybackService _playbackService;
        private bool _isCheckpointActive;


        public PianoWindow(string midiPath, MidiFile midiFile, string MidiName)
        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;
            _data = new Data();

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement
            _isCheckpointActive = false;

            _inputHandler = PianoInputHandlerService.Instance;
            _selectedMidiName = MidiName;
            _feedbacklogic = new FeedbackLogic(uitlezenLogic);

            _currentMidi = midiFile;
            _playbackService = new PlaybackService(midiFile);
            _playbackService.Finished += OnPlaybackFinished;

            _feedbacklogic.NewFeedback += UpdateKeyFeedback;

            _inputHandler.NotePressed -= OnMidiNotePressed; //veiligheid, niet perse nodig
            _inputHandler.NotePressed += OnMidiNotePressed;

            _inputHandler.NoteReleased -= OnMidiNoteReleased;
            _inputHandler.NoteReleased += OnMidiNoteReleased;

            this.KeyDown += PianoWindowPauze;

            this.WindowState = WindowState.Maximized; // Set window to fullscreen
            this.WindowStyle = WindowStyle.None;     // Remove the title bar and borders
            this.ResizeMode = ResizeMode.NoResize;  // Prevent resizing to enforce fullscreen

            UpdateMidiStatus();

            // Simuleer de score voor testdoeleinden
            _feedbacklogic.StartScoreSimulation();

            // Maak de ScorePage en laad deze in het Frame
            _score = new Score();
            ScoreFrame.Content = _score;
            _feedbacklogic.ScoreUpdated += OnScoreUpdated;
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

        private void UpdateKeyFeedback(string feedback)
        {
            Dispatcher.Invoke(() => FeedbackTextBox.Text = feedback);
        }

        private void OnMidiNotePressed(string note)
        {
            Dispatcher.Invoke(() =>
            {
                LastPressedNoteTextBox.Text = note;

                var targetKey = PianoCanvas.Children
                    .OfType<Rectangle>()
                    .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}");

                if (targetKey != null)
                {
                    targetKey.Fill = Brushes.Yellow;
                }
            });
        }
        
        private void OnMidiNoteReleased(string note)
        {
            Dispatcher.Invoke(() =>
            {
                var targetKey = PianoCanvas.Children
                    .OfType<Rectangle>()
                    .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}");

                if (targetKey != null)
                {
                    targetKey.Fill = Blackkeys.Contains(targetKey) ? Brushes.Black : Brushes.White;
                }
            });
        }
        
        private void HighlightKey(string note, bool isPressed)
        {
            var targetKey = PianoCanvas.Children
                .OfType<Rectangle>()
                .FirstOrDefault(r => r.Tag?.ToString() == $"PianoNote:{note}");

            if (targetKey != null)
            {
                targetKey.Fill = isPressed ? Brushes.Yellow :
                    (Blackkeys.Contains(targetKey) ? Brushes.Black : Brushes.White);
            }
        }
        
        public void PianoWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_playbackService.IsRunning)
            {
                _playbackService.Stop();
            }

            // Andere cleanup-logica
            _playbackService.Dispose();

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            if (_feedbacklogic != null)
            {
                _feedbacklogic.ScoreUpdated -= OnScoreUpdated;
            }
        }
        
        private void PianoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePiano();
            GenerateSlider();
            slider.SizeChanged += Slider_SizeChanged;

            if (_midiPath != null)
            {
                try
                {
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    _startTime = DateTime.Now;
                    _selectedSongDuration = SelectedSongDuration();
                    songID = GetSongID(_selectedMidiName);
                    CheckpointsForSong = LoadCheckpoints(songID);
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120), 
                        Tag = "0"
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
            GeneratePiano();
            GenerateSlider(); //Herteken de piano bij venstergrootte-aanpassing
        }
        
        private void Slider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (slider.ActualWidth > 0)
            {
                slider.SizeChanged -= Slider_SizeChanged; // Verwijder de eventhandler
                DrawAllMarkers();
            }
        }
        
        private void UpdateSlider(double value)
        {
            int minutes = (int)value / 60;
            int seconds = (int)value % 60;
            slider.Value = value;
            sliderValue.Text = $"Duur: {minutes}:{seconds:00}";
        }
        
        private double SelectedSongDuration()
        {
            List<string> Nameslist = _data.LoadMidiNames();
            List<double> DurationsList = _data.LoadSongDuration();

            //int voor het goede nummer in de lijst
            int i = Nameslist.FindIndex(d => d.ToString().Contains(_selectedMidiName));
            if (i == -1)
                throw new Exception("Geen duur gevonden voor geselecteerde MIDI.");
            double SelectedDuration = DurationsList[i];
            return SelectedDuration;
        }
        
        double RoundBasedOnFirstDecimal(double value)
        {
            // Haal de eerste decimaal op
            double fractionalPart = value - Math.Floor(value); // Geeft alleen de decimale waarde
            int firstDecimal = (int)(fractionalPart * 10); // Vermenigvuldig met 10 en cast naar int

            if (firstDecimal >= 5)
            {
                // Rond af naar boven
                return Math.Ceiling(value);
            }
            else
            {
                // Rond af naar beneden
                return Math.Floor(value);
            }
        }
        
        private void GenerateSlider()
        {


            string totalSeconds = string.Empty;

            double DurationSonginSec = _selectedSongDuration % 60;
            double DurationSonginMin = 0;

            if (_selectedSongDuration > 60)
            {
                DurationSonginMin = RoundBasedOnFirstDecimal(_selectedSongDuration / 60);
            }



            MidiSliderContainer.Children.Clear();

            // Slider aanmaken
            slider = new Slider()
            {
                Minimum = 0,
                Maximum = _selectedSongDuration,
                Value = 0,
                Orientation = Orientation.Horizontal,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                IsEnabled = false

            };

            // TextBlock aanmaken
            sliderValue = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                FontSize = 16,

            };



            // Event koppelen
            slider.ValueChanged += (s, e) =>
            {
                int wholeNumber = (int)Math.Round(e.NewValue);  // Rond naar het dichtstbijzijnde hele getal
                int min = 0;
                int sec = 0;
                if (wholeNumber >= 60)
                {
                    min = wholeNumber / 60;
                    sec = wholeNumber % 60;
                }
                else
                {
                    sec = wholeNumber;
                }

                sliderValue.Text = $"Duur: {min}:{sec:00}";
            };

            MidiSliderContainer.Children.Add(slider);
            MidiSliderContainer.Children.Add(sliderValue);
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

        private async void Timer_Tick(object sender, EventArgs e)
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

                    if (elapsedTime - 4 >= _selectedSongDuration)
                    {
                        UpdateSlider(_selectedSongDuration);
                        _timer.Stop();
                        return;
                    }
                    else if (elapsedTime <= 4)
                    {
                        UpdateSlider(0);
                    }
                    else
                    {
                        UpdateSlider(elapsedTime - 4);
                    }
                    if (elapsedTime > 4 && muziekafspelen)
                    {
                        muziekafspelen = false;
                        await Task.Run(() => _playbackService.Start());
                    }

                    var feedbacknotestoplay = uitlezenLogic.HaalNotenOp(elapsedTime);
                    var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedTime);
                    _feedbacklogic.updateNotestoplay(feedbacknotestoplay, elapsedTime, _totalPauseDuration.TotalSeconds);
                    foreach (var note in notesToPlay)
                    {
                        StartAnimationForNote(note.NoteName.ToString(), note.Length, note.Octave);
                    }

                    if (elapsedTime >= _selectedSongDuration)
                    {
                        _timer.Stop();
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

            double animationDuration = 10;

            MetricTimeSpan noteInSeconds = TimeConverter.ConvertTo<MetricTimeSpan>(length, uitlezenLogic.tempoMap);
            double noteHeight = (noteInSeconds.TotalSeconds / animationDuration) * 2000 * 2;

            Rectangle fallingNote = new()
            {
                Width = targetKey.Width,
                Height = noteHeight,
                Tag = "FallingNote",
                Fill = Blackkeys.Contains(targetKey) ? Brushes.Black : Brushes.Blue,
                Stroke = Brushes.Red
            };

            double left = Canvas.GetLeft(targetKey);
            double bottom = 2000;
            Canvas.SetLeft(fallingNote, left);
            Canvas.SetBottom(fallingNote, bottom);
            PianoCanvas.Children.Add(fallingNote);

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

            // Asynchrone taak om de noot na 10 seconden te verwijderen
            _ = RemoveNoteAfterDelay(fallingNote, 10);

            storyboard.Completed += (s, e) =>
            {
                activeAnimations.Remove(storyboard);
            };

            activeAnimations.Add(storyboard);
            storyboard.Begin();
        }

        private async Task RemoveNoteAfterDelay(Rectangle note, double delayInSeconds)
        {
            int elapsedSeconds = 0;
            while (elapsedSeconds < delayInSeconds)
            {
                if (!_isPaused)
                {
                    await Task.Delay(1000); // Wacht 1 seconde
                    elapsedSeconds++;
                }
                else
                {
                    await Task.Delay(100); // Wacht korter als de applicatie gepauzeerd is
                }
            }
            Dispatcher.Invoke(() =>
            {
                if (PianoCanvas.Children.Contains(note))
                {
                    PianoCanvas.Children.Remove(note);
                }
            });
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

                _playbackService.Stop();

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

                if (!muziekafspelen)
                {
                    _playbackService.Start();
                }

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
            _playbackService.Stop();
            HandlePlaybackStopped();

            await Task.Delay(3000); // Wait for 3 seconds

            Dispatcher.Invoke(() =>
            {
                // Close the current PianoWindow
                this.Close();
                List<int> topScores = _data.GetTopScores(GetSongID(_selectedMidiName));

                // Show the End Menu in a new window
                var window = new Window
                {
                    Title = "End Menu",
                    Content = new EndMenu(_currentMidi, finalScore, topScores, _isCheckpointActive),
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
            _playbackService.Stop();
            _playbackService.Dispose();
        }

        private void OnScoreUpdated(double score)
        {
            if (_isCheckpointActive)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                _score.UpdateScore(score);
            });
        }

        private void HandlePlaybackStopped()
        {
            try
            {
                if (uitlezenLogic == null || uitlezenLogic.tempoMap == null)
                {
                    Debug.WriteLine("MIDI-logica of TempoMap is niet beschikbaar.");
                    return;
                }

                finalScore = _feedbacklogic.GetScore();
                // Haal de titel van het nummer op
                string songTitle = System.IO.Path.GetFileNameWithoutExtension(_midiPath) ?? "Onbekend Lied";

                // Bereken de duur van het nummer
                double songDuration = _currentMidi.GetTrackChunks().Sum(chunk => chunk.GetDuration<MetricTimeSpan>(uitlezenLogic.tempoMap).TotalSeconds);

                // Haal het bestandspad op
                string filePath = _midiPath;

                // Sla de gegevens op via FeedbackLogic
                if(!_isCheckpointActive)
                {
                    _feedbacklogic.OnSongFinished(songTitle, songDuration, filePath);
                    return;
                }

                //Debug uitvoer
                Debug.WriteLine($"Playback gestopt. Titel: {songTitle}, Duur: {songDuration} seconden, Bestand: {filePath}");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fout in HandlePlaybackStopped: {ex.Message}");
            }
        }

        private void AddCheckpoint(double timestamp, string name)
        {
            
            if (CheckpointsForSong.Count >= MaxSegments)
            {
                MessageBox.Show("Maximum aantal segmenten bereikt!");
                return;
            }
            if (timestamp < 4)
            {
                MessageBox.Show("Je kan pas een checkpoint plaatsen na 4 seconden!");
                return;
            }
            var newCheckpoint = new Checkpoint { TimeStamp = timestamp, Name = name };
            _checkpoints.Add(newCheckpoint);

            DrawSegmentMarker(timestamp);

            SaveCheckpoint(songID, newCheckpoint);
        }
        
        private void RemoveSegment(Checkpoint checkpoint)
        {
            // Remove checkpoint and cancel its associated timer or animations if active
            _checkpoints.Remove(checkpoint);

            if (_checkpointTimers.ContainsKey(checkpoint))
            {
                var timer = _checkpointTimers[checkpoint];
                timer.Stop();
                _checkpointTimers.Remove(checkpoint);
            }
            int songID = GetSongID(_selectedMidiName);

            DeleteCheckpoint(songID, checkpoint);

            RemoveSegmentMarker(checkpoint);
            Debug.WriteLine($"Removed checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
        }

        private async Task StartPlaybackWithDelayAsync(Playback playback, TimeSpan delay)
        {
            await Task.Delay(delay); // Wacht de opgegeven tijd
            playback.Start();

        }

        private async void StartSongAtSegment(Checkpoint checkpoint)
        {
            // Cancel all previous timers and animations
            CancelAllTimersAndAnimations();

            UpdateSlider(checkpoint.TimeStamp - 4);

            try
            {
                double timeInSeconds = checkpoint.TimeStamp - 4;

                int hours = (int)(timeInSeconds / 3600); // 1 hour = 3600 seconds
                timeInSeconds %= 3600;

                int minutes = (int)(timeInSeconds / 60); // 1 minute = 60 seconds
                timeInSeconds %= 60;

                int seconds = (int)timeInSeconds;
                int milliseconds = (int)((timeInSeconds - seconds) * 1000);
                // Zet de tijd in seconden om naar MetricTimeSpan
                MetricTimeSpan metricTimeSpan = new MetricTimeSpan(hours, minutes, seconds, milliseconds);

                _playbackService.Start();
                _playbackService.MoveToTime(metricTimeSpan);
                _score.UpdateScore(-99999);
                _isCheckpointActive = true;
                // Start a fresh timer
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.0 / 120), // 120 FPS
                    Tag = "1"
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het starten vanaf checkpoint: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CancelAllTimersAndAnimations()
        {
            // Stop the master timer
            if (_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            // Remove all falling note animations
            var fallingNotes = PianoCanvas.Children.OfType<Rectangle>()
                .Where(r => r.Tag != null && r.Tag.ToString().StartsWith("FallingNote"))
                .ToList();

            foreach (var note in fallingNotes)
            {
                PianoCanvas.Children.Remove(note);
            }

            //Debug.WriteLine("Canceled all timers and animations.");
        }

        private void ShowCheckpointsPopup()
        {
            // Maak een nieuw venster voor de pop-up
            Window checkpointsWindow = new Window
            {
                Title = "Checkpoints",
                Height = 300,
                Width = 400,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Maak een ListBox voor de checkpoints
            ListBox checkpointsListBox = new ListBox
            {
                Margin = new Thickness(10)
            };
            int songID = GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = LoadCheckpoints(songID);

            // Voeg alle checkpoints toe aan de ListBox
            foreach (var checkpoint in CheckpointsForSong)
            {
                // Maak een horizontale StackPanel
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                // Maak het listItem voor de checkpoint
                var listItem = new TextBlock
                {
                    Text = $"{checkpoint.Name}, Timestamp: {checkpoint.TimeStamp:F2}s",
                    Margin = new Thickness(0, 0, 10, 0), // Kleine marge aan de rechterkant
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Voeg klik event toe aan het listItem
                listItem.MouseLeftButtonUp += (sender, e) =>
                {
                    
                    
                    _startTime = DateTime.Now - TimeSpan.FromSeconds(checkpoint.TimeStamp);
                    
                    uitlezenLogic.HerlaadNoten(checkpoint.TimeStamp);
                    StartSongAtSegment(checkpoint);
                    checkpointsWindow.Close();
                };

                // Maak het removeCheckpoint item
                var removeCheckpoint = new TextBlock
                {
                    Text = "Remove Checkpoint",
                    Foreground = new SolidColorBrush(Colors.Red),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Voeg klik event toe aan removeCheckpoint
                removeCheckpoint.MouseLeftButtonUp += (sender, e) =>
                {
                    RemoveSegment(checkpoint);
                    checkpointsListBox.Items.Refresh();
                };
                //var changeName = new TextBlock
                //{
                //    Text = "Change Name"
                //    Foreground = new SolidColorBrush(Colors.Blue),
                //    Cursor = Cursors.Hand,
                //    VerticalAlignment = VerticalAlignment.Center
                //};
                //changeName.MouseLeftButtonUp += (sender, e) =>
                //{

                //}

                // Voeg beide items toe aan de StackPanel
                stackPanel.Children.Add(listItem);
                stackPanel.Children.Add(removeCheckpoint);
                var listBoxItem = new ListBoxItem
                {
                    Content = stackPanel
                };

                // Voeg de StackPanel toe aan de ListBox
                checkpointsListBox.Items.Add(listBoxItem);
            }


            // Voeg de ListBox toe aan de pop-up venster
            checkpointsWindow.Content = checkpointsListBox;


            // Toon de pop-up
            checkpointsWindow.ShowDialog();
        }

        private void DrawSegmentMarker(double timestamp)
        {
            double position = (timestamp / _selectedSongDuration) * slider.ActualWidth;
            //Debug.WriteLine($"slider width: {slider.ActualWidth}");
            //Debug.WriteLine($"Duration: {_selectedSongDuration}");
            //Debug.WriteLine($"Timestamp: {timestamp}");
            if (position >= 0 && position <= MarkerCanvas.ActualWidth)
            {
                // Maak de marker (bijvoorbeeld een rechthoek)
                var marker = new Rectangle
                {
                    Width = 5,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Black
                };

                // Zet de marker op de juiste horizontale positie
                Canvas.SetLeft(marker, position);
                Canvas.SetTop(marker, 0); // Zet de marker boven de slider

                // Voeg de marker toe aan de MarkerCanvas
                MarkerCanvas.Children.Add(marker);
                checkpointMarkers.Add(marker);
            }
            else
            {
                MessageBox.Show("De marker valt buiten het zichtbare bereik.");
            }

        }

        private void DrawAllMarkers()
        {
            int songID = GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = LoadCheckpoints(songID);
            foreach (Checkpoint checkpoint in CheckpointsForSong)
            {
                DrawSegmentMarker(checkpoint.TimeStamp);
            }
        }

        private void AddCheckpointButton_Click(object sender, RoutedEventArgs e)
        {
            double timestamp = slider.Value;


            //Debug.WriteLine(slider.Value);
            // Haal de naam op van de checkpoint (bijvoorbeeld uit een TextBox)
            int songID = GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = LoadCheckpoints(songID);
            string name = "Checkpoint " + (CheckpointsForSong.Count + 1);  // Genereer een naam voor het checkpoint

            // Voeg het checkpoint toe aan de lijst
            AddCheckpoint(timestamp, name);



        }
        
        private void PlaySegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCheckpointsPopup();
        }

        private void RemoveSegmentMarker(Checkpoint checkpoint)
        {
            double calculatedPosition = (checkpoint.TimeStamp / _selectedSongDuration) * slider.ActualWidth;

            var marker = checkpointMarkers.FirstOrDefault(m =>
                Math.Abs(Canvas.GetLeft(m) - calculatedPosition) < 2);

            if (marker != null)
            {
                checkpointMarkers.Remove(marker);
                MarkerCanvas.Children.Remove(marker);
                Debug.WriteLine($"Removed marker for checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
            }
            else
            {
                Debug.WriteLine("No marker found.");
            }
        }

        public void SaveCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Bewaar de segmenten in de database, bijvoorbeeld in een SQL-tabel
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("INSERT INTO Checkpoint (songID, Timestamp, Name) VALUES (@songID, @Timestamp, @Name)", connection);

                command.Parameters.AddWithValue("@songID", songID);
                command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                command.Parameters.AddWithValue("@Name", checkpoint.Name);

                command.ExecuteNonQuery();
            }
        }
        
        public void DeleteCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Verwijder het segment uit de database
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
            "DELETE FROM Checkpoint WHERE songID = @songID AND Timestamp = @Timestamp",
            connection
        );

                command.Parameters.AddWithValue("@songID", songID);
                command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                command.ExecuteNonQuery();
            }
        }
        
        public int GetSongID(string songName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT ID FROM Song WHERE Title = @songName", connection);
                command.Parameters.AddWithValue("@songName", songName);

                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int songID))
                {
                    return songID;
                }
            }
            throw new Exception($"Nummer met naam '{songName}' niet gevonden in de database.");
        }
        
        public List<Checkpoint> LoadCheckpoints(int songID)
        {
            
            var checkpoints = new List<Checkpoint>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "SELECT Timestamp, Name FROM Checkpoint WHERE songID = @songID",
                    connection
                );

                command.Parameters.AddWithValue("@songID", songID);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var checkpoint = new Checkpoint
                        {
                            TimeStamp = Convert.ToDouble(reader["Timestamp"]),
                            Name = reader["Name"]?.ToString()
                        };
                        checkpoints.Add(checkpoint);
                    }
                }
            }

            return checkpoints;
        }
    }
}