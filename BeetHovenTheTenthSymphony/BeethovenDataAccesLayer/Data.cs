using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace BeethovenDataAccesLayer
{
    public class Data
    {
        private string folderPath = "C:\\MIDI_Files";

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
