using BeethovenBusiness.Checkpoints;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.PianoLogica;
using BeethovenBusiness.Progress;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using BeethovenBusiness.Checkpoints;
using BeethovenBusiness.PianoLogica;
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.NewFolder;
using BeethovenBusiness.Achievements;
using BeethovenBusiness.KeyboardReplay;




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
        private readonly IData _data;
        public string _selectedMidiName;
        private Slider slider;
        private TextBlock sliderValue;
        private readonly MidiFile _currentMidi;
        private OutputDevice _outputDevice;
        private Playback _playback;
        double elapsedTime;
        bool muziekafspelen = true;
        private AchievementLogic achievementlogic;
        private KeyboardInputHandler keyboardInputHandler;
        private ReplayManager _replayManager = new ReplayManager();
        private bool _isRecording = false;



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
        public string _difficulty;
        private int _earnedXP;
        private bool _leveledUp = false;

        int songID;
        List<Checkpoint> CheckpointsForSong;
        private PlaybackService _playbackService;
        private bool _isCheckpointActive;
        private CheckpointLogic _checkpointLogic;
        private ProgressService _progressService;
        private GameStatsService _gameStats;
        private readonly Dictionary<string, string> noteToKeyLabel = new()
        {
            { "C2", "Q" }, { "D2", "W" }, { "E2", "E" }, { "F2", "R" }, { "G2", "T" }, { "A2", "Y" }, { "B2", "U" },
            { "CSharp2", "1" }, { "DSharp2", "2" }, { "FSharp2", "3" }, { "GSharp2", "4" }, { "ASharp2", "5" },

            { "C3", "A" }, { "D3", "S" }, { "E3", "D" }, { "F3", "F" }, { "G3", "G" }, { "A3", "H" }, { "B3", "J" },
            { "CSharp3", "6" }, { "DSharp3", "7" }, { "FSharp3", "8" }, { "GSharp3", "9" }, { "ASharp3", "0" },

            { "C4", "Z" }, { "D4", "X" }, { "E4", "C" }, { "F4", "V" }, { "G4", "B" }, { "A4", "N" }, { "B4", "M" },
            { "CSharp4", "I" }, { "DSharp4", "O" }, { "FSharp4", "P" }, { "GSharp4", "K" }, { "ASharp4", "L" }
        };
        public PianoWindow(string midiPath, MidiFile midiFile, string MidiName, IData data, string difficulty, GameStatsService gameStats)
        {
            InitializeComponent();
            MidiService service = new MidiService(data);
            achievementlogic = new AchievementLogic(data);
            int songId = service.GetSongIdByName(MidiName);
            List<int> programNumbers = service.GetProgramNumbersWhoNeedsToPlay(songId);
            uitlezenLogic = new UitlezenMidiLogica(programNumbers);
            _progressService = new ProgressService(data);



            _midiPath = midiPath;
            _data = data;
            _difficulty = difficulty;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement
            _isCheckpointActive = false;

            _inputHandler = PianoInputHandlerService.Instance;
            _selectedMidiName = MidiName;
            _feedbacklogic = new FeedbackLogic(uitlezenLogic, _data);

            _checkpointLogic = new CheckpointLogic(_selectedSongDuration, _data, connectionString, _selectedMidiName);

            _currentMidi = midiFile;
            _playbackService = new PlaybackService(midiFile);
            _playbackService.Finished += OnPlaybackFinished;

            _feedbacklogic.NewFeedback += UpdateKeyFeedback;

            _inputHandler.NotePressed -= OnMidiNotePressed; //veiligheid, niet perse nodig
            _inputHandler.NotePressed += OnMidiNotePressed;

            _inputHandler.NoteReleased -= OnMidiNoteReleased;
            _inputHandler.NoteReleased += OnMidiNoteReleased;

            keyboardInputHandler = new KeyboardInputHandler(
                note => {
                    if (!_isRecording)
                    {
                        _isRecording = true;
                        _replayManager.StartRecording();
                    }

                    _replayManager.RecordNote(note);
                    _inputHandler?.TriggerNotePressed(note);
                },
                note => {
                _replayManager.RecordNote(note); // optioneel, of laat dit weg
                _inputHandler?.TriggerNoteReleased(note);
                }
            );


            this.KeyDown += (s, e) => keyboardInputHandler.HandleKeyDown(e.Key.ToString());
            this.KeyUp += (s, e) => keyboardInputHandler.HandleKeyUp(e.Key.ToString());

            this.Focusable = true;
            this.Focus();

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
            _gameStats = gameStats;
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
                    // laad het midi bestand en start tde timer
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    _startTime = DateTime.Now;
                    _selectedSongDuration = _data.SelectedSongDuration(_selectedMidiName);
                    songID = _checkpointLogic.GetSongID(_selectedMidiName);
                    CheckpointsForSong = _checkpointLogic.LoadCheckpoints(songID);
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
        
        private void GenerateSlider()
        {


            string totalSeconds = string.Empty;

            double DurationSonginSec = _selectedSongDuration % 60;
            double DurationSonginMin = 0;

            if (_selectedSongDuration > 60)
            {
                DurationSonginMin = _checkpointLogic.RoundBasedOnFirstDecimal(_selectedSongDuration / 60);
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
                .Where(child => child is Rectangle rect && (rect.Tag is string tag && tag.StartsWith("PianoNote")) ||
                (child is TextBlock))
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
                    PianoCanvas.Children.Add(whiteKey);
                    if (noteToKeyLabel.TryGetValue(whiteNote, out var label))
                    {
                        var text = new TextBlock
                        {
                            Text = label,
                            FontSize = 10,
                            Foreground = Brushes.Black
                        };
                        Canvas.SetLeft(text, currentX + 4);
                        Canvas.SetBottom(text, 4);
                        PianoCanvas.Children.Add(text);
                    }
                    Panel.SetZIndex(whiteKey, 0);
                    whiteKey.MouseDown += Key_MouseDown;

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

                        double keyLeft = currentX + whiteKeyWidth * 0.75 - (blackKeyWidth / 2) + whiteKeyWidth * 0.25;
                        double keyBottom = whiteKeyHeight - blackKeyHeight;

                        Canvas.SetLeft(blackKey, keyLeft);
                        Canvas.SetBottom(blackKey, keyBottom);
                        Panel.SetZIndex(blackKey, 1);
                        PianoCanvas.Children.Add(blackKey);
                        Blackkeys.Add(blackKey);
                        blackKey.MouseDown += Key_MouseDown;

                        if (noteToKeyLabel.TryGetValue(blackNote, out var blackLabel))
                        {
                            var text = new TextBlock
                            {
                                Text = blackLabel,
                                FontSize = 9,
                                Foreground = Brushes.White
                            };

                            Canvas.SetLeft(text, keyLeft + 4);
                            Canvas.SetBottom(text, keyBottom + 2);
                            Panel.SetZIndex(text, 2);
                            PianoCanvas.Children.Add(text);
                        }
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
                    if (elapsedTime > 4 && muziekafspelen) //als de eerste noot de piano bereikt
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
            var targetKey = PianoCanvas.Children //piano toets waar de noot op gespeeld moet worden
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

            Rectangle fallingNote = new() //noot
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

            LinearGradientBrush opacityMask = new LinearGradientBrush //onderkant van de noot onzichtbaar maken
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
                PauseFrame.Navigate(new PauzeMenu(_currentMidi, _data, _gameStats));       //navigeer naar PauzeMenu
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
                List<int> topScores = _data.GetTopScores(_checkpointLogic.GetSongID(_selectedMidiName));

                var endMenu = new EndMenu(_currentMidi, finalScore, topScores, _isCheckpointActive, _earnedXP, _leveledUp);

                endMenu.ReplayRequested += async () =>
                {
                    var currentWindow = Window.GetWindow(endMenu);
                    currentWindow?.Close();

                    var pianoWindow = new PianoWindow(_midiPath, _currentMidi, _selectedMidiName, _data, "replay", _gameStats);
                    pianoWindow.Show();

                    pianoWindow.ResetKeys();
                    await Task.Delay(500);

                    await _replayManager.ReplayAsync(async note =>
                    {
                        pianoWindow.ReceiveReplayNote(note);

                        await Task.Delay(100);
                        pianoWindow.ReceiveNoteRelease(note);
                    });
                };

                // Show the End Menu in a new window
                var window = new Window
                {
                    Title = "End Menu",
                    Content = endMenu,
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
        public void ReceiveReplayNote(string note)
        {
            keyboardInputHandler.HandleReplayNote(note);
        }

        public void ReceiveNoteRelease(string note)
        {
            keyboardInputHandler.HandleNoteRelease(note);
        }

        public void ResetKeys()
        {
            keyboardInputHandler.ReleaseAllKeys();
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
                elapsedTime = (DateTime.Now - _startTime).TotalSeconds;

                // Sla de gegevens op via FeedbackLogic
                if (!_isCheckpointActive)
                {
                    _feedbacklogic.OnSongFinished(songTitle, songDuration, filePath);
                    _gameStats.SaveSessionDetails(elapsedTime, DateTime.Now.ToString("yyyy-MM-dd"), songTitle);
                    _gameStats.initialize();

                    var xpResult = _progressService.CalculateXP(_difficulty, (int)finalScore);
                    _earnedXP = xpResult.earnedXP;
                    _leveledUp = xpResult.leveledUp;
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

        private async Task StartPlaybackWithDelayAsync(Playback playback, TimeSpan delay)
        {
            await Task.Delay(delay); // Wacht de opgegeven tijd
            playback.Start();

        }

        private async void StartSongAtSegment(Checkpoint checkpoint)
        {
            achievementlogic.UpdateAchievementStatus(new Achievement("Practice Mode", "Gebruik oefenmodus."));
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
            int songID = _checkpointLogic.GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = _checkpointLogic.LoadCheckpoints(songID);

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
                    _checkpointLogic.RemoveSegment(checkpoint);
                    RemoveSegmentMarker(checkpoint);
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
            int songID = _checkpointLogic.GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = _checkpointLogic.LoadCheckpoints(songID);
            foreach (Checkpoint checkpoint in CheckpointsForSong)
            {
                DrawSegmentMarker(checkpoint.TimeStamp);
            }
        }

        private void AddCheckpointButton_Click(object sender, RoutedEventArgs e)
        {
            double timestamp = slider.Value;
            int songID = _checkpointLogic.GetSongID(_selectedMidiName);
            List<Checkpoint> CheckpointsForSong = _checkpointLogic.LoadCheckpoints(songID);
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
            
            // Haal de naam op van de checkpoint (bijvoorbeeld uit een TextBox)
            
            string name = "Checkpoint " + (CheckpointsForSong.Count + 1);
            
            // Voeg het checkpoint toe aan de lijst
            _checkpointLogic.AddCheckpoint(timestamp, name);
            
            // Maak de marker voor het checkpoint
            DrawSegmentMarker(timestamp);

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

        
    }
}