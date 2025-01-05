using Melanchall.DryWetMidi.Core;
using System.Windows;
using System.Windows.Controls;

namespace BeetHovenWPF
{
    public partial class EndMenu : Page
    {
        private readonly MidiFile _midiFile;
        private readonly double _finalScore;

        public EndMenu(MidiFile midiFile, double finalScore)
        {
            InitializeComponent();

            _midiFile = midiFile;
            _finalScore = finalScore;

            // Toon de score in de UI
            ScoreTextBlock.Text = $"Score: {Math.Round(_finalScore)}";
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            // Sluit het venster
            Window.GetWindow(this)?.Close();
        }
    }
}
