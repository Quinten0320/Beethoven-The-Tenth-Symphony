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
        // public List<bool> SelectedTracks { get; private set; }
        List<TrackSettings> tracks = new List<TrackSettings>();
        int songId;
        MidiService service;
        
        public SongSettingsWindow(string completePath, MidiService service, MidiFileInfo midiInfo)
        {
            InitializeComponent();
            
            MidiFile midiFile = MidiFile.Read(completePath);
            songId = service.GetSongIdByName(midiInfo.Name);
            this.service = service;

            // Haal alle unieke ProgramNumbers op uit alle tracks
            List<int> usedProgramNumbers = midiFile.GetTrackChunks()
                .SelectMany(track => track.Events.OfType<ProgramChangeEvent>())
                .Select(pc => (int)pc.ProgramNumber)
                .Distinct()
                .OrderBy(num => num)
                .ToList();
            
            foreach (var programNumber in usedProgramNumbers)
            {
                tracks.Add(new TrackSettings()
                {
                    Name = GeneralMidiInstrumentName.GetGeneralMidiInstrumentName(programNumber), 
                    IsSelected = service.GetIfInstrumentIsSelected(songId, programNumber),
                    programNumber = programNumber
                });
            }
            
            TrackListBox.ItemsSource = tracks;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            service.saveInstrumentList(tracks, songId);
            
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