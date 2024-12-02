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
                // Dispose of the existing PianoInputHandler instance and its MIDI device
                var pianoInputHandler = PianoInputHandlerService.Instance;
                pianoInputHandler.Dispose();

                // Reinitialize MIDI input to detect and connect to a new device
                pianoInputHandler.InitializeMidiInput();

                MessageBox.Show("MIDI input detection and initialization successful.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"No MIDI device detected. Please connect a device and try again.\n\nDetails: {ex.Message}",
                                "No MIDI Device Detected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (MidiDeviceException ex)
            {
                MessageBox.Show($"Error: The MIDI device is already in use. Please close other programs using the device and try again.\n\nDetails: {ex.Message}",
                                "MIDI Device In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}