using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.IO;

namespace BeethovenDataAccesLayer
{
    public static class Data
    {
        private static string folderPath = "C:\\MIDI_Files";

        private static void SearchFolder()
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
        }
        public static string getFolderPath()
        {
            return folderPath;
        }

        public static List<string> LoadMidiNames()
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

        public static List<double> LoadMidiBPM()
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

        public static List<double> LoadSongDuration()
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

        public static List<int> LoadTotalNotes()
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

        public static MidiFile LoadMidiFile(string name)
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

        public static void UploadMidiFile(string selectedFile)
        {
            if (!ConfirmMidi(selectedFile))
            {
                throw new InvalidDataException("The selected file is not a valid MIDI file.");
            }

            SearchFolder();
            string destinationFilePath = Path.Combine(folderPath, Path.GetFileName(selectedFile));

            if (File.Exists(destinationFilePath))
            {
                throw new IOException("The selected MIDI file already exists in the destination folder.");
            }

            File.Copy(selectedFile, destinationFilePath);
        }

        private static bool ConfirmMidi(string filePath)
        {
            try
            {
                MidiFile.Read(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
