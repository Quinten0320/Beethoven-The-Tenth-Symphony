using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness
{
    public interface IMidiService
    {
        List<string> GetMidiFileNames();
        MidiFile GetMidiFile(string name);
        void UploadMidiFile(string selectedFile);
        string GetFolderPath();
        List<double> GetMidiBPM();
        List<double> GetSongTime();
        List<int> GetTotalNotes();
    }
}
