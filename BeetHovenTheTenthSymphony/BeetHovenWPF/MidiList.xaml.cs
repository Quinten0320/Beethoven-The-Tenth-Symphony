using System.Windows;
using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;

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

                var tempoMap = midiFile.GetTempoMap();

                var tempo = tempoMap.GetTempoAtTime((MidiTimeSpan)0);
                double microsecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote; 
                double bpm = 60_000_000.0 / microsecondsPerQuarterNote;

                MessageBox.Show($"Naam: {selectedMidiName} BPM: {bpm:F2}", "Tempo Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            fillList();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "MIDI Files (*.mid)|*.mid";

            if (dialog.ShowDialog() == true)
            {
                string selectedFile = dialog.FileName;
                Data data = new Data();

                try 
                { 
                    data.UploadMidiFile(selectedFile);
                    MessageBox.Show($"{selectedFile} uploaded successfully!", "Upload Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    fillList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading the MIDI file: \n{ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}