using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BeethovenBusiness;
using BeethovenDataAccesLayer;
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
        private ObservableCollection<MidiFileInfo> _midiFileInfos;
        private string _currentFilter = "Default";
        public MidiList()
        {
            InitializeComponent();
            DataBaseHelper.InitializeDatabase();
            this.WindowState = WindowState.Maximized;
            _midiService = new MidiService();
            _midiFileInfos = new ObservableCollection<MidiFileInfo>();
            _midiService.AddMissingMidiFilesToDatabase();
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
                    MidiFile midiFile = _midiService.LoadMidiFile(selectedMidiName);// niet nodig maar kan handig zijn misschien

                    string folderPath = _midiService.getFolderPath();
                    string completePath = folderPath + "\\" + selectedMidiName + ".mid";

                    PianoWindow pianowindow = new PianoWindow(completePath, midiFile);
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
            else if (_currentFilter == "Favourites")
            {
                MidiFileList.ItemsSource = _midiFileInfos
                    .Where(file => file.Favourite == true)
                    .ToList();
            }
        }

        public ObservableCollection<MidiFileInfo> CalculateDifficulty()
        {
            List<string> midiNames = _midiService.LoadMidiNames();
            List<double> bpm = _midiService.LoadMidiBPM();
            List<double> duration = _midiService.LoadSongDuration();
            List<int> totalNotes = _midiService.LoadTotalNotes();

            var midiFileInfos = bpm.Select((b, i) =>
            {
                double difficultyValue = (Math.Pow(b, 2) / 10000) * (totalNotes[i] / duration[i]);

                string difficulty = difficultyValue switch
                {
                    <= 5 => "Easy",
                    <= 15 => "Medium",
                    _ => "Hard"
                };

                bool isFavourite = _midiService.IsSongFavourite(midiNames[i]);

                return new MidiFileInfo
                {
                    Name = midiNames[i],
                    Difficulty = difficulty,
                    Favourite = isFavourite,
                };
            });

            return new ObservableCollection<MidiFileInfo>(midiFileInfos);
        }

        private void FavouriteFucntion(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column is DataGridCheckBoxColumn && e.EditingElement is CheckBox checkBox)
            {
                MidiFileInfo selectedMidiInfo = e.Row.Item as MidiFileInfo;

                if (selectedMidiInfo != null)
                {
                    bool isFavourite = checkBox.IsChecked ?? false;

                    selectedMidiInfo.Favourite = isFavourite;

                    _midiService.AddFavouriteHelper(selectedMidiInfo.Name);
                }
            }
        }

        private void DetectMidiInputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pianoInputHandler = PianoInputHandlerService.Instance;
                pianoInputHandler.Dispose();

                pianoInputHandler.InitializeMidiInput();

                MessageBox.Show("MIDI input detection and initialization successful.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"No MIDI device detected. Please connect a device and try again.\n\nDetails: {ex.Message}","No MIDI Device Detected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (MidiDeviceException ex)
            {
                MessageBox.Show($"Error: The MIDI device is already in use. Please close other programs using the device and try again.\n\nDetails: {ex.Message}","MIDI Device In Use", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}","Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string songName = button.Tag.ToString();

                var result = MessageBox.Show($"Are you sure you want to delete '{songName}'?",
                                             "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    MidiService midiService = new MidiService();
                    midiService.DeleteSong(songName); 

                    MidiFileList.ItemsSource = midiService.LoadMidiNames();
                    fillList();
                }
            }
        }
    }
}