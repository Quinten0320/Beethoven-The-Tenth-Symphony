using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.NewFolder;
using BeethovenBusiness.PianoLogica;
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
        string selectedMidiName;
        private readonly IData _data;
        private GameStatsService _gameStatsService;

        public MidiList()
        {
            _data = DataFactory.CreateDefaultData();
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            _midiService = new MidiService(_data);
            _midiFileInfos = new ObservableCollection<MidiFileInfo>();
            _midiService.InitializeDatabaseAndSync();
            fillList();

            _gameStatsService = new GameStatsService(_data);
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

        private void GameStats_Bttn(Object sender, RoutedEventArgs e)
        {
            GameStats gameStatsWindow = new GameStats(_gameStatsService);
            gameStatsWindow.ShowDialog(); 
        }

        private void MidiFileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MidiFileList.SelectedItem is MidiFileInfo selectedMidiInfo)
            {
                 selectedMidiName = selectedMidiInfo.Name;

                try
                {
                    MidiFile midiFile = _midiService.LoadMidiFile(selectedMidiName);// niet nodig maar kan handig zijn misschien ??

                    string folderPath = _midiService.getFolderPath();
                    string completePath = folderPath + "\\" + selectedMidiName + ".mid";
                    
                    PianoWindow pianowindow = new PianoWindow(completePath, midiFile, selectedMidiName, _data, selectedMidiInfo.Difficulty, _gameStatsService);
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
            MidiFileList.ItemsSource = _midiService.ApplyFilter(_midiFileInfos.ToList(), _currentFilter);
        }

        public ObservableCollection<MidiFileInfo> CalculateDifficulty()
        {
            return new ObservableCollection<MidiFileInfo>(_midiService.CalculateDifficulty());
        }

        private void FavouriteFunction(object sender, DataGridCellEditEndingEventArgs e)
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
                    MidiService midiService = new MidiService(_data);
                    midiService.DeleteSong(songName); 

                    MidiFileList.ItemsSource = midiService.LoadMidiNames();
                    fillList();
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string songName = button.Tag.ToString();
                MidiFileInfo midiInfo = _midiFileInfos.FirstOrDefault(m => m.Name == songName);
        
                if (midiInfo != null)
                {
                    string folderPath = _midiService.getFolderPath();
                    string completePath = folderPath + "\\" +midiInfo.Name + ".mid";
                    
                    var settingsDialog = new SongSettingsWindow(completePath, _midiService, midiInfo)
                    {
                        Owner = this // Set the owner to make it modal
                    };
            
                    if (settingsDialog.ShowDialog() == true)
                    {
                        // Here you would use settingsDialog.SelectedTracks to update playback
                        // For example:
                        // _midiService.UpdateTrackSelection(midiInfo.Name, settingsDialog.SelectedTracks);
                
                        MessageBox.Show("Settings saved!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void AchievementsButton_Click(object sender, RoutedEventArgs e)
        {
            var achievementWindow = new Achievements(_data);
            achievementWindow.ShowDialog();
        }

        private void ProgressButton_Click(object sender, RoutedEventArgs e)
        {
            var progressPage = new Progress();
            var window = new Window
            {
                Title = "Progress",
                Content = progressPage,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
        }
    }
}