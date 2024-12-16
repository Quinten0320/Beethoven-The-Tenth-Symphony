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

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private List<Rectangle> Blackkeys = new List<Rectangle>();
        private readonly PianoInputHandler _inputHandler;

        private readonly UitlezenMidiLogica uitlezenLogic;
        private readonly string _midiPath;
        private readonly int _octaves = 8;
        private const int _whiteKeyCount = 7;
        private DateTime _startTime;
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

            UpdateMidiStatus();
            List<double> DurationsList = _data.LoadSongDuration();
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
                    double bpm = uitlezenLogic.BerekenBpm();

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
            bool isWindowOpen = Application.Current.Windows.OfType<PianoWindow>().Any(window => window.IsVisible);
            if (!isWindowOpen)
            {
                masterTimer?.Stop();
                return;
            }

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

        private void StartAnimationForNote(string note, double duration, int octave)
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
        private void AddCheckpoint(double timestamp, string name)
        {
            if (_checkpoints.Count >= MaxSegments)
            {
                MessageBox.Show("Maximum aantal segmenten bereikt!");
                return;
            }

            
            Checkpoint newCheckpoint = new Checkpoint { TimeStamp = timestamp, Name = name };
            _checkpoints.Add(newCheckpoint);

           
            //_data.SaveSegment(newCheckpoint);

            
            DrawSegmentMarker(timestamp);
            
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
        //public void SaveSegment(Checkpoint checkpoint)
        //{
        //    // Bewaar de segmenten in de database, bijvoorbeeld in een SQL-tabel
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        var command = new SqlCommand("INSERT INTO Segments (Timestamp, Name) VALUES (@Timestamp, @Name)", connection);
        //        command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
        //        command.Parameters.AddWithValue("@Name", checkpoint.Name);
        //        command.ExecuteNonQuery();
        //    }
        //}
        //public void DeleteSegment(Checkpoint checkpoint)
        //{
        //    // Verwijder het segment uit de database
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        var command = new SqlCommand("DELETE FROM Segments WHERE Timestamp = @Timestamp", connection);
        //        command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
        //        command.ExecuteNonQuery();
        //    }
        //}
        //private void SaveCheckpointToDatabase(double timestamp)
        //{
        //    // Voeg de timestamp van het segment toe aan de database
        //    // Dit kan worden uitgebreid om de tijd te verwerken en op te slaan in een segmentenlijst of database
        //    _data.SaveCheckpoint(timestamp);
        //}
    }
}