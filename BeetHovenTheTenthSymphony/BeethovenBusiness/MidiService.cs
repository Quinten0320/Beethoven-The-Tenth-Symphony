using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;

namespace BeethovenBusiness
{
    public class MidiService
    {
        public List<string> GetMidiFileNames()
        {
            return Data.LoadMidiNames();
        }

        public void AddFavourite(string songName)
        {
            int songId = GetSongIdByName(songName);

            DataBaseHelper.AddFavourite(songId);
        }

        public bool IsSongFavourite(string songName)
        {
            return DataBaseHelper.IsSongFavourite(songName);
        }

        public int GetSongIdByName(string songName)
        {
            return DataBaseHelper.GetSongIdByName(songName);
        }

        public MidiFile GetMidiFile(string name)
        {
            return Data.LoadMidiFile(name);
        }

        public void UploadMidiFile(string selectedFile)
        {
            Data.UploadMidiFile(selectedFile);

            string fileName = Path.GetFileNameWithoutExtension(selectedFile);
            string folderPath = Data.getFolderPath();
            string fullPath = Path.Combine(folderPath, Path.GetFileName(selectedFile));
            MidiFile midiFile = Data.LoadMidiFile(fileName);

            var duration = midiFile.GetDuration<MetricTimeSpan>();
            double durationInSeconds = duration.TotalMicroseconds / 1_000_000.0;
            double secondsDecimals = Math.Round(durationInSeconds, 2);

            DataBaseHelper.AddSong(fileName, secondsDecimals, fullPath);
        }

        public string getFolderPath()
        {
            return Data.getFolderPath();
        }

        public List<double> GetMidiBPM()
        {
            return Data.LoadMidiBPM();
        }

        public List<double> GetSongtime()
        {
            return Data.LoadSongDuration();
        }

        public List<int> GetTotalNotes()
        {
            return Data.LoadTotalNotes();
        }
    }
}
