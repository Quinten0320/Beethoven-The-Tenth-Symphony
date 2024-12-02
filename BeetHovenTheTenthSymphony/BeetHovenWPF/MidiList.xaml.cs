using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BeethovenBusiness;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Win32;
using static BeetHovenWPF.MidiList;

namespace BeetHovenWPF
{
    public partial class MidiList : Window
    {
        private readonly MidiService _midiService;
        private List<MidiFileInfo> _midiFileInfos;
        private string _currentFilter = "Default";
        public MidiList()
        {
            InitializeComponent();

            _midiService = new MidiService();
            _midiFileInfos = new List<MidiFileInfo>();
            fillList();
        }

        public void fillList()
        {
            _midiFileInfos = CalculateDifficulty();
            ApplyFilter();
        }
        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            fillList();
        }

        private void MidiFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MidiFileList.SelectedItem is MidiFileInfo selectedMidiInfo)
            {
                string selectedMidiName = selectedMidiInfo.Name;

                try
                {
                    MidiFile midiFile = _midiService.GetMidiFile(selectedMidiName);// niet nodig maar kan handig zijn misschien

                    string folderPath = _midiService.getFolderPath();
                    string completePath = folderPath + "\\" + selectedMidiName + ".mid";

                    PianoWindow pianowindow = new PianoWindow(completePath);
                    pianowindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid MIDI file.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void FilterButton_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (FilterButton.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                _currentFilter = selectedItem.Content.ToString();
                ApplyFilter();
            }
        }
        private void ApplyFilter()
        {
            if (_currentFilter == "Default")
            {
                MidiFileList.ItemsSource = _midiFileInfos;
            }
            else if (new[] { "Easy", "Medium", "Hard" }.Contains(_currentFilter))
            {
                MidiFileList.ItemsSource = _midiFileInfos
                    .Where(file => file.Difficulty == _currentFilter)
                    .ToList();
            }
            else if (_currentFilter == "A-Z")
            {
                MidiFileList.ItemsSource = _midiFileInfos
                    .OrderBy(file => file.Name)
                    .ToList();
            }
            else if (_currentFilter == "Z-A")
            {
                MidiFileList.ItemsSource = _midiFileInfos
                    .OrderByDescending(file => file.Name)
                    .ToList();
            }
        }

        public List<MidiFileInfo> CalculateDifficulty()
        {
            List<MidiFileInfo> midiFileInfos = new List<MidiFileInfo>();

            List<string> midiNames = _midiService.GetMidiFileNames();
            List<double> bpm = _midiService.GetMidiBPM();
            List<double> duration = _midiService.GetSongtime();
            List<int> totalNotes = _midiService.GetTotalNotes();

            return bpm.Select((b, i) =>
            {
                double difficultyValue = (Math.Pow(b, 2) / 10000) * (totalNotes[i] / duration[i]);

                string difficulty = difficultyValue switch
                {
                    <= 5 => "Easy",
                    <= 15 => "Medium",
                    _ => "Hard"
                };

                return new MidiFileInfo
                {
                    Name = midiNames[i],
                    Difficulty = difficulty
                };
            }).ToList();
        }
    }
}