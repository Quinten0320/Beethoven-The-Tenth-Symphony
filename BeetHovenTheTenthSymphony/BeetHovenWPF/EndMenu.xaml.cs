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

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void ScoresClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}