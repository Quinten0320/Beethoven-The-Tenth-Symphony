using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using System.Security.Policy;

namespace BeethovenBusiness
{
    public class MidiService : IData
    {
        private readonly Data _data = new Data();

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

        public void UploadMidiFile(string selectedFile)
        {
            _data.UploadMidiFile(selectedFile);

            string fileName = Path.GetFileNameWithoutExtension(selectedFile);
            string folderPath = _data.getFolderPath();
            string fullPath = Path.Combine(folderPath, Path.GetFileName(selectedFile));
            MidiFile midiFile = _data.LoadMidiFile(fileName);

            var duration = midiFile.GetDuration<MetricTimeSpan>();
            double durationInSeconds = duration.TotalMicroseconds / 1_000_000.0;
            double secondsDecimals = Math.Round(durationInSeconds, 2);

            AddSong(fileName, secondsDecimals, fullPath);
        }

        public void AddSong(string fileName, double duration, string fullPath)
        {
            _data.AddSong(fileName, duration, fullPath);
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
                _ => midiFileInfos,
            };
        }

        public void InitializeDatabaseAndSync()
        {
            DataBaseHelper.InitializeDatabase();
            AddMissingMidiFilesToDatabase();
        }
    }
}
