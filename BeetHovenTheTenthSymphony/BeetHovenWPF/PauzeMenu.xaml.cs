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
        public PauzeMenu()
        {
            InitializeComponent();
        }

        private void ContinueClick(object sender, RoutedEventArgs e)
        {
            // Verberg het pauzemenu
            var pianoWindow = Window.GetWindow(this) as PianoWindow;
            if (pianoWindow != null)
            {
                pianoWindow.ShowPauseMenu(false); // Pauzemenu sluiten
                pianoWindow.TogglePause();       // Pauze beëindigen
            }
        }

        private void StopClick(object sender, RoutedEventArgs e)
        {
            // Haal de referentie naar het hoofdvenster
            var pianoWindow = Window.GetWindow(this) as PianoWindow;
            if (pianoWindow != null)
            {
                // Maak een nieuwe CancelEventArgs
                var cancelEventArgs = new CancelEventArgs();

                // Roep de sluitmethode van PianoWindow aan
                pianoWindow.PianoWindow_Closing(sender, cancelEventArgs);

                // Sluit het venster
                if (!cancelEventArgs.Cancel) // Controleer of sluiting niet is geannuleerd
                {
                    pianoWindow.Close();
                }
            }
        }

    }
}
