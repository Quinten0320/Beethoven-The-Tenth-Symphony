using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace BeethovenDataAccesLayer
{
    public class Data
    {
        private static string _folderPath = "C:\\MIDI_Files";
        private static string _connectionString = @"Data Source=..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";

        private void SearchFolder()
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public string getFolderPath()
        {
            return _folderPath;
        }

        public List<string> LoadMidiNames()
        {
            SearchFolder();

            var files = Directory.GetFiles(_folderPath, "*.mid");
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

            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");
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

            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");
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

            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");
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

            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");

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
            if (!ConfirmMidi(selectedFile))
            {
                throw new InvalidDataException("The selected file is not a valid MIDI file.");
            }

            SearchFolder();
            string destinationFilePath = Path.Combine(_folderPath, Path.GetFileName(selectedFile));

            if (File.Exists(destinationFilePath))
            {
                throw new IOException("The selected MIDI file already exists in the destination folder.");
            }

            File.Copy(selectedFile, destinationFilePath);
        }

        private bool ConfirmMidi(string filePath)
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

        //////////////////////////////////////////// SQL Lite \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

        public void AddSong(string title, double duration, string filePath)
        {
            string insertSongQuery = @"
            INSERT INTO Song (Title, Duration, FilePath)
            VALUES (@Title, @Duration, @FilePath);";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(insertSongQuery, connection))
                {
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Duration", duration);
                    command.Parameters.AddWithValue("@FilePath", filePath);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddFavourite(int songId)
        {
            string checkQuery = "SELECT COUNT(*) FROM Favourites WHERE SongID = @SongID;";
            string insertQuery = "INSERT INTO Favourites (SongID) VALUES (@SongID);";
            string deleteQuery = "DELETE FROM Favourites WHERE SongID = @SongID;";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var checkCommand = new SQLiteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@SongID", songId);

                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (count > 0)
                    {
                        using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@SongID", songId);
                            deleteCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var insertCommand = new SQLiteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@SongID", songId);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        public bool IsSongFavourite(string songName)
        {
            string query = "SELECT COUNT(*) FROM Favourites WHERE SongID = (SELECT ID FROM Song WHERE Title = @Title);";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", songName);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count > 0;
                }
            }
        }

        public int GetSongIdByName(string songName)
        {
            string query = "SELECT ID FROM Song WHERE Title = @Title";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", songName);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
    }
}
