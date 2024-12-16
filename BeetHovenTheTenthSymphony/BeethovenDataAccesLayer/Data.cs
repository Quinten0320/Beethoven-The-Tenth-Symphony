using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BeethovenDataAccesLayer
{
    public class Data
    {
        private string folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\BeethovenDataAccesLayer\MidiFiles"));

        private void SearchFolder()
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            Debug.WriteLine(folderPath);
        }
        public string getFolderPath()
        {
            return folderPath;
        }

        public List<string> LoadMidiNames()
        {
            SearchFolder();

            var files = Directory.GetFiles(folderPath, "*.mid");
            List<string> names = new List<string>();

            foreach (string file in files) 
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                names.Add(fileName);
            }
            return names;
        }

        public List<double> LoadMidiBPM()
        {
            SearchFolder();

            string[] midiFilePaths = Directory.GetFiles(folderPath, "*.mid");
            List<double> midiBPMs = new List<double>();

            foreach (string midiFilePath in midiFilePaths)
            {
                    MidiFile midiFile = MidiFile.Read(midiFilePath);
                    var tempoMap = midiFile.GetTempoMap();

                    var tempo = tempoMap.GetTempoAtTime((MidiTimeSpan)0);

                    double microsecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote;
                    double bpm = 60_000_000.0 / microsecondsPerQuarterNote;

                    midiBPMs.Add((int)Math.Round(bpm));               
            }
            return midiBPMs;
        }

        public List<double> LoadSongDuration()
        {
            SearchFolder();

            string[] midiFilePaths = Directory.GetFiles(folderPath, "*.mid");
            List<double> songDurations = new List<double>();

            foreach (string midiFilePath in midiFilePaths)
            {
                    MidiFile midiFile = MidiFile.Read(midiFilePath);
                    var duration = midiFile.GetDuration<MetricTimeSpan>();

                    double durationInSeconds = duration.TotalMicroseconds / 1_000_000.0;

                    songDurations.Add((int)Math.Round(durationInSeconds));                
            }

            return songDurations;
        }

        public List<int> LoadTotalNotes()
        {
            SearchFolder();

            string[] midiFilePaths = Directory.GetFiles(folderPath, "*.mid");
            List<int> totalNotesList = new List<int>();

            foreach (string midiFilePath in midiFilePaths)
            {
                    MidiFile midiFile = MidiFile.Read(midiFilePath);
                    var notes = midiFile.GetNotes().OrderBy(n => n.Time).ToList();

                    int totalNotes = notes.Count(); 
                    totalNotesList.Add(totalNotes);
            }

            return totalNotesList;
        }

        public MidiFile LoadMidiFile(string name)
        {
            SearchFolder();

            string[] midiFilePaths = Directory.GetFiles(folderPath, "*.mid");

            foreach (string filePath in midiFilePaths)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        MidiFile midiFile = MidiFile.Read(filePath);
                        return midiFile;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error loading midi file '{name}': {ex.Message}");
                }
            }
            throw new Exception($"Midi file with name '{name}' not found.");
        }

        public void UploadMidiFile(string selectedFile)
        {
            SearchFolder();
            string destinationFilePath = Path.Combine(folderPath, Path.GetFileName(selectedFile));

            if (File.Exists(destinationFilePath))
            {
                throw new IOException("The selected MIDI file already exists in the destination folder.");
            }

            File.Copy(selectedFile, destinationFilePath);
        }
    }
}
