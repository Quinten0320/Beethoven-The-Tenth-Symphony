using System.IO.Enumeration;
using System.Text;
using System.Windows;
using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace BeetHovenWPF
{
    public partial class MidiList : Window
    {
        public MidiList()
        {
            InitializeComponent();
            fillList();
        }

        public void fillList()
        {
            Data data = new Data();

            List<string> midiNames = data.LoadMidiNames();

            MidiFileList.ItemsSource = midiNames;
        }

        private void MidiFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string selectedMidiName = MidiFileList.SelectedItem.ToString();

            Data data = new Data();

            try
            {
                MidiFile midiFile = data.LoadMidiFile(selectedMidiName);

                var tempoTrack = midiFile.GetTempoMap().GetTempoChanges();



                MessageBox.Show($"Naam: {selectedMidiName} BPM: {tempoTrack}", "Tempo Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}