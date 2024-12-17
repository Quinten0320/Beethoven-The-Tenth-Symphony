using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness
{
    public interface IData
    {
        List<string> LoadMidiNames();
        void AddFavourite(int songId);
        bool IsSongFavourite(string songName);
        int GetSongIdByName(string songName);
        MidiFile LoadMidiFile(string name);
        void UploadMidiFile(string selectedFile);
        string getFolderPath();
        void AddSong(string fileName, double duration, string fullPath);
        List<double> LoadMidiBPM();
        List<double> LoadSongDuration();
        List<int> LoadTotalNotes();
        void AddMissingMidiFilesToDatabase();
        void DeleteSong(string songName);
    }
}
