using BeethovenBusiness.Interfaces;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using System.Security.Policy;

namespace BeethovenBusiness.MidiFileLogica
{
    public class MidiService
    {
        private readonly IData _data;
        
        public MidiService(IData data)
        {
            _data = data;
        }

        public List<int> GetProgramNumbersWhoNeedsToPlay(int songId)
        {
            return _data.GetProgramNumbersWhoNeedsToPlay(songId);
        }

        public bool GetIfInstrumentIsSelected(int songID, int programNumber)
        {
            try
            {
                return _data.GetIfInstrumentIsSelected(songID, programNumber);
            }
            catch (InvalidOperationException e)
            {
                _data.addTrack(programNumber, songID);
                return true;
            }
        }

        public void saveInstrumentList(List<TrackSettings> trackSettings, int songID)
        {
            _data.saveInstrumentList(trackSettings, songID);
        }
        
        public List<string> LoadMidiNames()
        {
            return _data.LoadMidiNames();
        }
        public void AddFavouriteHelper(string songId)
        {
            int songName = GetSongIdByName(songId);

            AddFavourite(songName);
        }
        public void AddFavourite(int songId)
        {
            _data.AddFavourite(songId);
        }
        public bool IsSongFavourite(string songName)
        {
            return _data.IsSongFavourite(songName);
        }

        public int GetSongIdByName(string songName)
        {
            return _data.GetSongIdByName(songName);
        }

        public MidiFile LoadMidiFile(string name)
        {
            return _data.LoadMidiFile(name);
        }
        public void AddSong(string fileName, double duration, string fullPath, List<int> usedProgramNumbers)
        {
            _data.AddSong(fileName, duration, fullPath, usedProgramNumbers);
        }

        public string getFolderPath()
        {
            return _data.getFolderPath();
        }

        public List<double> LoadMidiBPM()
        {
            return _data.LoadMidiBPM();
        }

        public List<double> LoadSongDuration()
        {
            return _data.LoadSongDuration();
        }

        public List<int> LoadTotalNotes()
        {
            return _data.LoadTotalNotes();
        }
        public void AddMissingMidiFilesToDatabase()
        {
            _data.AddMissingMidiFilesToDatabase();
        }
        public void UploadMidiFile(string selectedFile)
        {
            _data.UploadMidiFile(selectedFile);

            string fileName = Path.GetFileNameWithoutExtension(selectedFile);
            string folderPath = _data.getFolderPath();
            string fullPath = Path.Combine(folderPath, Path.GetFileName(selectedFile));
            MidiFile midiFile = _data.LoadMidiFile(fileName);

            var duration = midiFile.GetDuration<MetricTimeSpan>();
            double durationInSeconds = duration.TotalMicroseconds / 1_000_000.0; //._.
            double secondsDecimals = Math.Round(durationInSeconds, 2);

            List<int> usedProgramNumbers = midiFile.GetTrackChunks()
                .SelectMany(track => track.Events.OfType<ProgramChangeEvent>())
                .Select(pc => (int)pc.ProgramNumber)
                .Distinct()
                .OrderBy(num => num)
                .ToList();
            
            

            AddSong(fileName, secondsDecimals, fullPath, usedProgramNumbers);
        }

        public void DeleteSong(string songName)
        {
            int songId = GetSongIdByName(songName);

            _data.DeleteSong(songId);

            string folderPath = getFolderPath();
            string filePath = Path.Combine(folderPath, songName + ".mid");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        public List<MidiFileInfo> CalculateDifficulty()
        {
            List<string> midiNames = LoadMidiNames();
            List<double> bpm = LoadMidiBPM();
            List<double> duration = LoadSongDuration();
            List<int> totalNotes = LoadTotalNotes();
            return bpm.Select((b, i) =>
            {
                double difficultyValue = (Math.Pow(b, 2) / 10000) * (totalNotes[i] / duration[i]);
                string difficulty = difficultyValue switch
                {
                    <= 5 => "Easy",
                    <= 15 => "Medium",
                    _ => "Hard"
                };
                bool isFavourite = IsSongFavourite(midiNames[i]);
                return new MidiFileInfo
                {
                    Name = midiNames[i],
                    Difficulty = difficulty,
                    Favourite = isFavourite,
                };
            }).ToList();
        }
        public List<MidiFileInfo> ApplyFilter(List<MidiFileInfo> midiFileInfos, string filter)
        {
            return filter switch
            {
                "Default" => midiFileInfos,
                "Easy" or "Medium" or "Hard" => midiFileInfos.Where(file => file.Difficulty == filter).ToList(),
                "A-Z" => midiFileInfos.OrderBy(file => file.Name).ToList(),
                "Z-A" => midiFileInfos.OrderByDescending(file => file.Name).ToList(),
                "Favourites" => midiFileInfos.Where(file => file.Favourite).ToList(),
            };
        }
        public void InitializeDatabaseAndSync()
        {
            _data.InitializeDatabase();
            AddMissingMidiFilesToDatabase();
            UpdateDatabase();
            _data.InitializeAchievements();
        }

        //voeg hier je database update dingetjes voor je feature toe
        public void UpdateDatabase()
        {
            var querys = new List<string>();
            querys.Add(@"
                        CREATE TABLE IF NOT EXISTS Achievements (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Description TEXT NOT NULL,
                            DatumBehaald DATETIME,
                            IsBehaald BOOLEAN NOT NULL
                        );"); //:(
            querys.Add(
                @"
                        CREATE TABLE IF NOT EXISTS Tracks (
                            ProgramNumber INTEGER NOT NULL,
                            SongID INTEGER NOT NULL,
                            IsPlayed BOOLEAN NOT NULL,
                            PRIMARY KEY (ProgramNumber, SongID),
                            FOREIGN KEY (SongID) REFERENCES Song(ID)
                        );");


            querys.Add(@"
                        CREATE TABLE IF NOT EXISTS Progress (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            TotalXP INTEGER NOT NULL DEFAULT 0,
                            CurrentXP INTEGER NOT NULL DEFAULT 0,   
                            Level INTEGER NOT NULL DEFAULT 1
                        );");

            foreach (var query in querys)
            {
                _data.UpdateDatabase(query);
            }
        }
    }
}
