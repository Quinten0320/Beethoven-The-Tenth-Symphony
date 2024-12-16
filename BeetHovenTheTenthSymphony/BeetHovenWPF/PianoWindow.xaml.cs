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
using BeethovenDataAccesLayer;
using System.Reflection;
using System.Xml.Linq;
using System.Configuration;

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

        private bool allesopgevraagd = true;
        long getmaxlength;
        long getgemiddeldelengte;

        private List<Storyboard> activeAnimations = new List<Storyboard>();
        private DateTime _pauseStartTime;
        private TimeSpan _totalPauseDuration = TimeSpan.Zero;
        private bool _isPaused = false;

        

        private DispatcherTimer masterTimer;
        private readonly Data _data;
        private string _selectedMidiName;
        private Slider slider;
        private TextBlock sliderValue;
        private double _elapsedTime;
        private List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private const int MaxSegments = 5;
        private List<Rectangle> checkpointMarkers = new List<Rectangle>();
        private Dictionary<Checkpoint, DispatcherTimer> _checkpointTimers = new Dictionary<Checkpoint, DispatcherTimer>();
        private bool checkpointEnded = false;
        private bool checkpointIsProcessed = false;
        private readonly string connectionString = @"Data Source=C:\Users\gielz\source\repos\Beethoven-The-Tenth-Symphony\BeetHovenTheTenthSymphony\BeethovenDataAccesLayer\Files\BeethovenDataBase.db;Version=3;";



        public PianoWindow(string midiPath, string selectedMidiName)

        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            _midiPath = midiPath;
            _data = new Data();
            _selectedMidiName = selectedMidiName;

            Loaded += PianoWindow_Loaded;
            SizeChanged += PianoWindow_SizeChanged;
            Closing += PianoWindow_Closing; // Koppel het Closing-evenement

            _inputHandler = PianoInputHandlerService.Instance;

            _inputHandler.NotePressed -= OnMidiNotePressed; //veiligheid, niet perse nodig
            _inputHandler.NotePressed += OnMidiNotePressed;
            this.KeyDown += PianoWindowPauze;

            this.WindowState = WindowState.Maximized; // Set window to fullscreen
            this.WindowStyle = WindowStyle.None;     // Remove the title bar and borders
            this.ResizeMode = ResizeMode.NoResize;  // Prevent resizing to enforce fullscreen

            UpdateMidiStatus();
            List<double> DurationsList = _data.LoadSongDuration();
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

        public void PianoWindow_Closing(object sender, CancelEventArgs e)
        {
            // Verwijder de oude midicontroller wanneer de pianowindow sluit (dit is nodig)
            _inputHandler.Dispose();

            // Stop de timer
            if (masterTimer != null)
            {
                masterTimer.Stop();
                masterTimer.Tick -= MasterTimer_Tick;
            }
        }

        private void PianoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePiano();
            GenerateSlider();

            if (_midiPath != null)
            {
                try
                {
                    uitlezenLogic.LaadMidiBestand(_midiPath);
                    _startTime = DateTime.Now;


                    masterTimer = new DispatcherTimer

                    {
                        Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
                    };
                    masterTimer.Tick +=  MasterTimer_Tick;
                    masterTimer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij initialisatie: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); //Herteken de piano bij venstergrootte-aanpassing
            GenerateSlider();
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

            double DurationSonginSec = SelectedSongDuration() % 60;
            double DurationSonginMin = 0;

            if (SelectedSongDuration() > 60)
            {
                DurationSonginMin = RoundBasedOnFirstDecimal(SelectedSongDuration() / 60);
            }



            MidiSliderContainer.Children.Clear();

            // Slider aanmaken
            slider = new Slider()
            {
                Minimum = 0,
                Maximum = SelectedSongDuration(),
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

        private void MasterTimer_Tick(object sender, EventArgs e)
        {
            if (_isPaused)
            {
                // Timer is actief, maar logica wordt niet uitgevoerd als de applicatie is gepauzeerd.
                return;
            }

            bool isWindowOpen = Application.Current.Windows.OfType<PianoWindow>().Any(window => window.IsVisible);
            if (!isWindowOpen)
            {
            try
            {
                // Calculate elapsed time since start
                double elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

                // Ensure slider reflects elapsed time
                double songDuration = SelectedSongDuration();
                if (elapsedSeconds >= songDuration)
                {
                    UpdateSlider(songDuration);
                    masterTimer.Stop();
                    checkpointEnded = true;
                    return;
                }
                else
                {
                    UpdateSlider(elapsedSeconds);
                }


                // Fetch notes to play for this elapsed time
                var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedSeconds);

                // Animate each note
                foreach (var note in notesToPlay)
                {
                    StartAnimationForNote(note.NoteName.ToString(), note.Length, note.Octave);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartAnimationForNote(string note, double length, int octave)
        {


            Rectangle fallingNote;
            double animationDuration = 5 * (120 / uitlezenLogic.BerekenBpm());
            

            // Bereken de werkelijke duur van de noot in seconden
            double actualduration = (duration / uitlezenLogic.GetTicksPerBeat()) * (60 / uitlezenLogic.BerekenBpm());

            // Zoek de rechthoek die overeenkomt met de noot

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

            fallingNote = new Rectangle
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

                fallingNote = new Rectangle
                {
                    Width = keyWidth,  // De breedte van de vallende noot is een derde van de breedte van de doeltoets
                    Height = noteHeight,
                    Fill = Brushes.Black,
                    Stroke = Brushes.Red,
                    Tag = "FallingNote"
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
                    Tag = "FallingNote"
                };

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

            //fallAnimation.Completed += (s, e) => PianoCanvas.Children.Remove(fallingNote);
            //fallingNote.BeginAnimation(Canvas.BottomProperty, fallAnimation);
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
                PauseFrame.Navigate(new PauzeMenu());       //navigeer naar PauzeMenu
            }
            else
            {
                PauseFrame.Content = null;                 //leeg de Frame
                PauseFrame.Visibility = Visibility.Collapsed; //verberg de Frame
            }
        }
        private void AddCheckpoint(double timestamp, string name)
        {
            if (_checkpoints.Count >= MaxSegments)
            {
                MessageBox.Show("Maximum aantal segmenten bereikt!");
                return;
            }

            
            Checkpoint newCheckpoint = new Checkpoint { TimeStamp = timestamp, Name = name };
            _checkpoints.Add(newCheckpoint);

           
            //SaveSegment(newCheckpoint);

            
            DrawSegmentMarker(timestamp);
            
        }
        private void RemoveSegment(Checkpoint checkpoint)
        {
            // Remove checkpoint and cancel its associated timer or animations if active
            _checkpoints.Remove(checkpoint);

            DeleteSegment(checkpoint);
            RemoveSegmentMarker(checkpoint);
            Debug.WriteLine($"Removed checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
        }





        private void StartSongAtSegment(Checkpoint checkpoint)
        {
            // Cancel all previous timers and animations
            CancelAllTimersAndAnimations();
            _startTime = DateTime.Now - TimeSpan.FromSeconds(checkpoint.TimeStamp);
            // Display countdown in a TextBlock
            //CountdownTextBlock.Visibility = Visibility.Visible;
            UpdateSlider(checkpoint.TimeStamp);

            //for (int i = 3; i > 0; i--)
            //{
            //    CountdownTextBlock.Text = i.ToString();
            //    await Task.Delay(1000); // Wait 1 second between each number
            //}

            //CountdownTextBlock.Visibility = Visibility.Hidden;

            

            // Start a fresh timer
            masterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
            };
            masterTimer.Tick += MasterTimer_Tick;
            masterTimer.Start();

            // Start animations for notes from this checkpoint
            ShowNotesFromCheckpoint(checkpoint);

            Debug.WriteLine($"Started song at checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s after countdown.");
        }
        private void CancelAllTimersAndAnimations()
        {
            // Stop the master timer
            if (masterTimer != null && masterTimer.IsEnabled)
            {
                masterTimer.Stop();
                masterTimer.Tick -= MasterTimer_Tick;
            }

            // Remove all falling note animations
            var fallingNotes = PianoCanvas.Children.OfType<Rectangle>()
                .Where(r => r.Tag != null && r.Tag.ToString().StartsWith("FallingNote"))
                .ToList();

            foreach (var note in fallingNotes)
            {
                PianoCanvas.Children.Remove(note);
            }

            Debug.WriteLine("Canceled all timers and animations.");
        }


        private void ShowNotesFromCheckpoint(Checkpoint checkpoint)
        {
            // Calculate the elapsed duration from the checkpoint's timestamp to the song's end
            double remainingDuration = SelectedSongDuration() - checkpoint.TimeStamp;

            // Fetch notes starting from the checkpoint's timestamp
            var notesToPlay = uitlezenLogic.HaalNotenOp(remainingDuration);

            // Animate the notes
            foreach (var note in notesToPlay)
            {
                StartAnimationForNote(note.NoteName.ToString(), note.Length, note.Octave);
            }

            Debug.WriteLine($"Showing notes from checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
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

            // Voeg alle checkpoints toe aan de ListBox
            foreach (var checkpoint in _checkpoints)
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
            double position = (timestamp / SelectedSongDuration()) * slider.ActualWidth;
            

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
        private void AddCheckpointButton_Click(object sender, RoutedEventArgs e)
        {
            double timestamp = slider.Value;
           

            //Debug.WriteLine(slider.Value);
            // Haal de naam op van de checkpoint (bijvoorbeeld uit een TextBox)
            string name = "Checkpoint " + (_checkpoints.Count + 1);  // Genereer een naam voor het checkpoint

            // Voeg het checkpoint toe aan de lijst
            AddCheckpoint(timestamp, name);

           

        }
        private void PlaySegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCheckpointsPopup();
        }

        private void RemoveSegmentMarker(Checkpoint checkpoint)
        {
            double calculatedPosition = (checkpoint.TimeStamp / SelectedSongDuration()) * slider.ActualWidth;

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

        private void StartCheckpointTimer(Checkpoint checkpoint)
        {
            if (masterTimer != null && masterTimer.IsEnabled)
            {
                masterTimer.Stop();
                checkpointIsProcessed = false; // Stop de timer eerst voordat je deze opnieuw start
            }
            // Maak een nieuwe timer
            masterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
            };

            // Starttijd instellen
            DateTime checkpointStartTime = DateTime.Now + TimeSpan.FromSeconds(checkpoint.TimeStamp);

            masterTimer.Tick += MasterTimer_Tick;
            masterTimer.Start();

            _startTime = DateTime.Now;
            // Voeg de timer toe aan de lijst en start deze

        }
        private void StopCheckpointTimers()
        {
            checkpointIsProcessed = false;
            foreach (var timer in _checkpointTimers.Values)
            {
                timer.Stop();
                Debug.WriteLine("Stopped checkpoint timer.");
            }
            _checkpointTimers.Clear();
        }
        private void StartAnimationFromCheckpoint(Checkpoint checkpoint)
        {
            
            if (checkpointEnded)
            {
                
                StartSongAtSegment(checkpoint);
            }
            
            // Logica om de animatie vanaf het checkpoint te starten
            //Debug.WriteLine($"Animatie gestart vanaf checkpoint: {checkpoint.Name} op {checkpoint.TimeStamp} seconden.");
            ShowNotesFromCheckpoint(checkpoint);
        }
        public void SaveSegment(Checkpoint checkpoint)
        {
            // Bewaar de segmenten in de database, bijvoorbeeld in een SQL-tabel
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("INSERT INTO Segments (Timestamp, Name) VALUES (@Timestamp, @Name)", connection))
                {
                    command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                    command.Parameters.AddWithValue("@Name", checkpoint.Name);
                    command.ExecuteNonQuery();
                }
            }
        }
        public void DeleteSegment(Checkpoint checkpoint)
        {
            // Verwijder het segment uit de database
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand("DELETE FROM Segments WHERE Timestamp = @Timestamp", connection))
                {
                    command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                    command.ExecuteNonQuery();
                }
            }
        }
        private void SaveCheckpointToDatabase(double timestamp)
        {
            // Voeg de timestamp van het segment toe aan de database
            // Dit kan worden uitgebreid om de tijd te verwerken en op te slaan in een segmentenlijst of database
            var checkpoint = new Checkpoint
            {
                TimeStamp = timestamp,
                Name = $"Checkpoint at {timestamp:F2} seconds"
            };

            SaveSegment(checkpoint);
        }
        public List<Checkpoint> LoadAllCheckpoints()
        {
            var checkpoints = new List<Checkpoint>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT Timestamp, Name FROM Segments", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        checkpoints.Add(new Checkpoint
                        {
                            TimeStamp = reader.GetDouble(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }

            Debug.WriteLine($"{checkpoints.Count} checkpoints loaded from the database.");
            return checkpoints;
        }
    }
}