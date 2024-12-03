using BeethovenDataAccesLayer;
using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;

namespace BeethovenBusiness
{
    public class MidiService
    {
        private readonly Data _data;

        public MidiService()
        {
            _data = new Data();
        }

        public List<string> GetMidiFileNames()
        {
            return _data.LoadMidiNames();
        }

        public MidiFile GetMidiFile(string name)
        {
            return _data.LoadMidiFile(name);
        }

        public void UploadMidiFile(string selectedFile)
        {
            _data.UploadMidiFile(selectedFile);
        }

        public string getFolderPath()
        {
            return _data.getFolderPath();
        }

        public List<double> GetMidiBPM()
        {
            return _data.LoadMidiBPM();
        }

        public List<double> GetSongtime()
        {
            return _data.LoadSongDuration();
        }

        public List<int> GetTotalNotes()
        {
            return _data.LoadTotalNotes();
        }
    }
}
