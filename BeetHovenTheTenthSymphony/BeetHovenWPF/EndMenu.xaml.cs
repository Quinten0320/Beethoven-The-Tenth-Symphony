
using Melanchall.DryWetMidi.Core;
using System.Windows;
using System.Windows.Controls;


namespace BeetHovenWPF
{
    public partial class EndMenu : Page
    {
        private readonly MidiFile _midiFile;
        private readonly double _finalScore;
        private bool _leveledUp;
        public event Func<Task> ReplayRequested;


        public EndMenu(MidiFile midiFile, double finalScore, List<int> topScores, bool isCheckpointActive, int earnedXP, bool leveledUp)
        {
            InitializeComponent();

            _midiFile = midiFile;
            _finalScore = finalScore;
            _leveledUp = leveledUp;


            this.Loaded += EndMenu_Loaded;
            // Toon de score in de UI
            if (isCheckpointActive)
            {
                ScoreTextBlock.Text = $"Score: INVALID, CHECKPOINTS HAVE BEEN USED!!!";
            }
            else
            {
                ScoreTextBlock.Text = $"Score: {Math.Round(_finalScore)}";
                XPEarnedTextBlock.Text = $"XP earned: {earnedXP}";
            }

            TopScoresTextBlock.Text = "Top 3 Scores:\n" +
            string.Join("\n", topScores.Select((score, index) => $"{index + 1}. {score}"));
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            // Sluit het venster
            Window.GetWindow(this)?.Close();
        }

        private void EndMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (_leveledUp)
            {
                var dialog = new LevelUpDialog
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
        }

        private async void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplayRequested != null)
            {
                await ReplayRequested.Invoke();
            }
        }
    }
}
