
    using BeethovenBusiness;
﻿   using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    namespace BeetHovenWPF
    {
        public partial class MainWindow : Window
        {
            private readonly UitlezenMidiLogica uitlezenLogic;
            private DateTime startTime;
            string midiPath = @"C:\Users\quint\Downloads\nyan cat.mid.mid";

            public MainWindow()
            {
                InitializeComponent();
                uitlezenLogic = new UitlezenMidiLogica();

            }
            private void OpenPianoWindow(object sender, RoutedEventArgs e)
            {
                PianoWindow pianoWindow = new PianoWindow();
                pianoWindow.Show();
                Midiuitlezen();
            }
        }
        
        private void Midiuitlezen()
        {
            string midiPath = @"C:\Users\quint\Downloads\nyan cat.mid.mid";

            try
            {
                uitlezenLogic.LaadMidiBestand(midiPath);
                double bpm = uitlezenLogic.BerekenBpm();
                NotesTextBox.AppendText($"BPM: {bpm:F2}\n");

                startTime = DateTime.Now;

                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.0 / 120) // 120 FPS
                };
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij initialisatie: {ex.Message}");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                double elapsedTime = (DateTime.Now - startTime).TotalSeconds;
                var notesToPlay = uitlezenLogic.HaalNotenOp(elapsedTime);

                if (notesToPlay.Count > 0)
                {
                    NotesTextBox.AppendText(string.Join("\n", notesToPlay) + "\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout in timer: {ex.Message}");
            }
        }
        
    }