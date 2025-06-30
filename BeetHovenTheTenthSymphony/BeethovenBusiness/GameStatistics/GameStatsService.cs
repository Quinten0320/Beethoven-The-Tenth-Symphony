using BeethovenBusiness.GameStatistics;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.NewFolder
{
    public class GameStatsService
    {
        public int AmountOfSongs {get; set;}
        public int AmountOfSongsThisWeek { get; set; }
        public int AmountOfSongsThisMonth { get; set; }

        public string LastPlayedSong { get; set; }
        public List<int> scores { get; private set; }
        public double Duration { get; set; }
        public int LastScore { get; set; }
        public double AverageTimeSession { get; set; }
        public List<KeyValuePair<string, int>> Notes { get; private set; }
        public Session session { get; set; }

        pu

        private IData _data;
        private Session _sessionDetails;
        private string _folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\BeethovenDataAccesLayer\MidiFiles"));

        public GameStatsService(IData data)
        {
            _data = data;
            initialize();
        }

        public void initialize()
        {
            AmountOfSongs = GetTotalAmountOfSongs();
            AmountOfSongsThisWeek = _data.GetAmountOfSongsThisWeek();
            AmountOfSongsThisMonth = GetAmountOfSongsthisMont();


            _sessionDetails = GetSessionDetails();
            session = _sessionDetails;
            scores = _data.GetScoresBySongId(_sessionDetails.SongID);

            if (_sessionDetails != null)
            {
                LastPlayedSong = getSongDetails(_sessionDetails.SongID).Title;
                Duration = _sessionDetails.Duration;
                Notes = GetNoteCounts(getPath(LastPlayedSong)).ToList();
            }
        }

        public void SaveSessionDetails(double duration, string date, string title)
        {
            _data.saveSessionDetails(duration, date, title);
        }

        public Session GetSessionDetails()
        {

            return _data.getSessionDetails();
        }

        public void updateSessionDetails()
        {
            _sessionDetails = GetSessionDetails();
        }

        public List<int> GetScoreBoardData()
        {
            return _data.GetScoresBySongId(_sessionDetails.SongID);
        }

        public Song getSongDetails(int SongID)
        {
            //momenteel enkel de 
            return _data.GetSongDetails(SongID);
        }

        public int GetTotalAmountOfSongs()
        {
            //Dmv van de sessions
            return _data.GetTotalAmountOfSongs();
        }

        public int GetAmountOfSongsthisMont()
        {
            return _data.GetAmountOfSongsthisMont();
        }

        public static Dictionary<string, int> GetNoteCounts(string midiFilePath)
        {
            var noteCounts = new Dictionary<string, int>();

            // Lees MIDI-bestand
            var midiFile = MidiFile.Read(midiFilePath);

            // Alle noten verzamelen uit alle trackchunks
            var notes = midiFile.GetTrackChunks()
                                .SelectMany(chunk => chunk.GetNotes());

            foreach (var note in notes)
            {
                var noteName = note.NoteName.ToString();

                if (noteCounts.ContainsKey(noteName))
                    noteCounts[noteName]++;
                else
                    noteCounts[noteName] = 1;
            }

            return noteCounts;
        }

        public string getPath(string name)
        {
            _data.SearchFolder();

            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");

            foreach (string filePath in midiFilePaths)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return filePath;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error loading midi file '{name}': {ex.Message}");
                }
            }
            throw new Exception($"Midi file with name '{name}' not found.");
        }
    }
}


