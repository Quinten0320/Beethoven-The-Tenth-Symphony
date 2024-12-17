using Melanchall.DryWetMidi.Core;
using System.Windows;
using System.Windows.Controls;

namespace BeetHovenWPF
{
    public partial class EndMenu : Page
    {
        private readonly MidiFile _midiFile;

        public EndMenu(MidiFile midiFile)
        {
            InitializeComponent();
            _midiFile = midiFile;
        }

        private void RestartClick(object sender, RoutedEventArgs e)
        {
            // Navigate to the PianoWindow or restart logic
            var pianoWindow = new PianoWindow(null, _midiFile);
            pianoWindow.Show();
            Window.GetWindow(this)?.Close(); // Close the parent window hosting the Page
        }

        private void StopClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Stop the application
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close(); // Close the parent window hosting the Page
        }
    }
}
