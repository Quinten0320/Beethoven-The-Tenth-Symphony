using System.Windows;
using BeethovenBusiness;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Win32;

namespace BeetHovenWPF
{
    public partial class MidiList : Window
    {
        private readonly MidiService _midiService;
        public MidiList()
        {
            InitializeComponent();

            _midiService = new MidiService();
            fillList();
        }

        public void fillList()
        {

            List<string> midiNames = _midiService.GetMidiFileNames();

            MidiFileList.ItemsSource = midiNames;
        }

        private void MidiFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string selectedMidiName = MidiFileList.SelectedItem.ToString();

            try
            {
                MidiFile midiFile = _midiService.GetMidiFile(selectedMidiName);

                string folderPath = _midiService.getFolderPath();
                string completePath = folderPath + "\\" + selectedMidiName + ".mid";

                var tempoMap = midiFile.GetTempoMap();

                //var tempo = tempoMap.GetTempoAtTime((MidiTimeSpan)0);
                //double microsecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote; 
                //double bpm = 60_000_000.0 / microsecondsPerQuarterNote;

                //MessageBox.Show($"Naam: {selectedMidiName} BPM: {bpm:F2}", "Tempo Info", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow mainwindow = new MainWindow(completePath);
                mainwindow.ShowDialog();
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

                try 
                { 
                    _midiService.UploadMidiFile(selectedFile);
                    string fileName = System.IO.Path.GetFileName(selectedFile);
                    MessageBox.Show($"{fileName} uploaded successfully!", "Upload Successful", MessageBoxButton.OK, MessageBoxImage.Information);

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