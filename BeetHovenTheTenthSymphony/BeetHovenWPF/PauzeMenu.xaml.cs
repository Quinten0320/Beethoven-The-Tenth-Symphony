using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class PauzeMenu : Page
    {
        private readonly MidiFile _midiFile;
        public PauzeMenu(MidiFile midiFile)
        {
            InitializeComponent();
            _midiFile = midiFile;
        }

        private void ContinueClick(object sender, RoutedEventArgs e)
        {
            //verberg pauzemenu
            var pianoWindow = Window.GetWindow(this) as PianoWindow;
            if (pianoWindow != null)
            {
                pianoWindow.ShowPauseMenu(false); //pauzemenu sluiten
                pianoWindow.TogglePause();       //pauze beëindigen
            }
        }

        private void StopClick(object sender, RoutedEventArgs e)
        {
            var pianoWindow = Window.GetWindow(this) as PianoWindow;
            if (pianoWindow != null)
            {
                pianoWindow.StopAndDisposePlayback();
                //maak een nieuwe CancelEventArgs
                var cancelEventArgs = new CancelEventArgs();

                //roep de sluitmethode van PianoWindow aan
                pianoWindow.PianoWindow_Closing(sender, cancelEventArgs);

                //sluit het venster
                if (!cancelEventArgs.Cancel) //controleer of sluiting niet is geannuleerd
                {
                    pianoWindow.Close();
                }
            }
        }

        private void RestartClick(object sender, RoutedEventArgs e)
        {
            var pianoWindow = Window.GetWindow(this) as PianoWindow;
            if (pianoWindow != null)
            {
                pianoWindow.StopAndDisposePlayback();
                //sla het pad van het huidige MIDI-bestand op
                string midiPath = pianoWindow._midiPath;
                string selectedMidiName = pianoWindow._selectedMidiName;
                //sluit het huidige venster
                pianoWindow.Close();

                //maak een nieuw exemplaar van PianoWindow
                var newPianoWindow = new PianoWindow(midiPath, _midiFile, selectedMidiName);
                newPianoWindow.Show();
            }
        }
    }
}
