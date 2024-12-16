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
    }
}
