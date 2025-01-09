using Melanchall.DryWetMidi.Core;
using System.Windows;
using System.Windows.Controls;

namespace BeetHovenWPF
{
    public partial class EndMenu : Page
    {
        private readonly MidiFile _midiFile;
        private readonly double _finalScore;

        public EndMenu(MidiFile midiFile, double finalScore, List<int> topScores, bool isCheckpointActive)
        {
            InitializeComponent();

            _midiFile = midiFile;
            _finalScore = finalScore;

            // Toon de score in de UI
            if (isCheckpointActive)
            {
                ScoreTextBlock.Text = $"Score: INVALID, CHECKPOINTS HAVE BEEN USED!!!";
            }
            else
            {
                ScoreTextBlock.Text = $"Score: {Math.Round(_finalScore)}";
            }

            TopScoresTextBlock.Text = "Top 3 Scores:\n" +
            string.Join("\n", topScores.Select((score, index) => $"{index + 1}. {score}"));
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            // Sluit het venster
            Window.GetWindow(this)?.Close();
        }
    }
}
