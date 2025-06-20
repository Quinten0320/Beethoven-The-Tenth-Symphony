// SongSettingsWindow.xaml.cs
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using BeethovenDataAccesLayer.DataBaseAcces;
using Melanchall.DryWetMidi.Core;

namespace BeetHovenWPF
{
    public partial class SongSettingsWindow : Window
    {
        public class TrackSetting
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }

        public List<bool> SelectedTracks { get; private set; }

        public SongSettingsWindow(string completePath)
        {
            InitializeComponent();
            
            MidiFile midiFile = MidiFile.Read(completePath);

            // Haal alle unieke ProgramNumbers op uit alle tracks
            List<int> usedProgramNumbers = midiFile.GetTrackChunks()
                .SelectMany(track => track.Events.OfType<ProgramChangeEvent>())
                .Select(pc => (int)pc.ProgramNumber)
                .Distinct()
                .OrderBy(num => num)
                .ToList();
            
            List<TrackSetting> tracks = new List<TrackSetting>();
            foreach (var programNumber in usedProgramNumbers)
            {
                tracks.Add(new TrackSetting()
                {
                    Name = GeneralMidiInstrumentName.GetGeneralMidiInstrumentName(programNumber), 
                    IsSelected = false  //falko dit moet uiteindelijk opgehaalt worden uit database of andere opslag zodat hij het herindert
                });
            }
            
            TrackListBox.ItemsSource = tracks;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // Collect which tracks are selected
            SelectedTracks = new List<bool>();
            foreach (TrackSetting item in TrackListBox.Items)
            {
                SelectedTracks.Add(item.IsSelected);
            }
            
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}