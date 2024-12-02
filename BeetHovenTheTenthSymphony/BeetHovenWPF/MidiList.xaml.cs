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
        PianoInputHandler pianoInputHandler = new PianoInputHandler();


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
            if (MidiFileList.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid MIDI file.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedMidiName = MidiFileList.SelectedItem.ToString();

            try
            {
                MidiFile midiFile = _midiService.GetMidiFile(selectedMidiName);

                string folderPath = _midiService.getFolderPath();
                string completePath = folderPath + "\\" + selectedMidiName + ".mid";

                // Open the piano window without assuming the MIDI device state
                PianoWindow pianowindow = new PianoWindow(completePath);
                pianowindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening the MIDI file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void DetectMidiInputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pianoInputHandler = new PianoInputHandler();

                // Reinitialize MIDI input
                pianoInputHandler.InitializeMidiInput();

                MessageBox.Show("MIDI input detection and initialization successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "No MIDI Device Detected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}