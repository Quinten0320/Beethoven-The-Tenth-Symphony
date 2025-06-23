using BeethovenBusiness.Checkpoints;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.NewFolder;
using BeethovenBusiness.PianoLogica;
using BeethovenBusiness.PreviewLogic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
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
using System.Windows.Shapes;

namespace BeetHovenWPF
{
    public partial class PreviewWindow : Window
    {
        private PreviewLogic _previewLogic;
        private Playback _playback;
        private PlaybackService _playbackService;
        private readonly MidiService _midiService;
        private readonly string _selectedMidiName;
        private readonly string _difficulty;
        private readonly string _selectedSongDuration;
        private readonly MidiFile _midiFile;
        private readonly IData _data;
        private GameStatsService _gameStatsService;
        public PreviewWindow(string name, string difficulty, MidiFile midiFile, IData data, GameStatsService gameStatsService)
        {
            InitializeComponent();
            _selectedMidiName = name;
            _difficulty = difficulty;
            _midiFile = midiFile;
            _data = data;
            _previewLogic = new PreviewLogic(data);
            _selectedSongDuration = _previewLogic.GetDuration(name);
            _playbackService = new PlaybackService(midiFile);
            _midiService = new MidiService(data);
            _gameStatsService = gameStatsService;

            Closing += PreviewWindow_Closing;

            SongNameText.Text = _selectedMidiName;
            DifficultyText.Text = $"Difficulty: {_difficulty}";
            DurationText.Text = $"Duration: {_selectedSongDuration}";
        }

        public void PreviewWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_playbackService.IsRunning)
            {
                _playbackService.Stop();
            }

            _playbackService.Dispose();
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_playbackService.IsRunning)
            {
                _playbackService.Finished += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PlayButton.Content = new Image
                        {
                            Source = new BitmapImage(new Uri("/Images/play-button.png", UriKind.Relative)),
                            Stretch = Stretch.Uniform
                        };
                        _playbackService.Stop();
                        _playbackService.Dispose();
                        _playbackService = new PlaybackService(_midiFile);
                    });
                };
                _playbackService.Start();
                PlayButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("/Images/pause-button.png", UriKind.Relative)),
                    Stretch = Stretch.Uniform
                };
            }
            else
            {
                _playbackService.Stop();
                PlayButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("/Images/play-button.png", UriKind.Relative)),
                    Stretch = Stretch.Uniform
                };
            }
   
        }

        private void PlaySongButton_Click(object sender, RoutedEventArgs e)
        {
            _playbackService?.Stop();
            _playbackService?.Dispose();
            _playbackService = new PlaybackService(_midiFile);

           this.Close();

            try
            {
                string folderPath = _midiService.getFolderPath();
                string completePath = folderPath + "\\" + _selectedMidiName + ".mid";

                PianoWindow pianowindow = new PianoWindow(completePath, _midiFile, _selectedMidiName, _data, _difficulty, _gameStatsService);
                pianowindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
