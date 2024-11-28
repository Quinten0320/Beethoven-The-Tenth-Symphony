
    using BeethovenBusiness;
using BeethovenDataAccesLayer;
using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
using System.Windows.Threading;

    namespace BeetHovenWPF
{
    public partial class MainWindow : Window
    {
        private readonly UitlezenMidiLogica uitlezenLogic;
        private DateTime startTime;
        string midiPath;

        public MainWindow(string filePath)
        {
            InitializeComponent();
            uitlezenLogic = new UitlezenMidiLogica();
            midiPath = filePath;
            Midiuitlezen();
            DataBaseHelper.InitializeDatabase();
        }
        private void OpenPianoWindow(object sender, RoutedEventArgs e)
        {
            PianoWindow pianoWindow = new PianoWindow();
            pianoWindow.Show();
        }

        private void Midiuitlezen()
        {
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



}