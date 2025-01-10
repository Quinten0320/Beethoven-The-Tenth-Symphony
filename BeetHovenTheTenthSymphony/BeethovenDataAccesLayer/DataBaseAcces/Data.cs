using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;

using System.Data.SQLite;

using System.Diagnostics;

using System.IO;
using System.Security.Policy;

namespace BeethovenDataAccesLayer.DataBaseAcces
{
    public class Data
    {
        private string _folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\BeethovenDataAccesLayer\MidiFiles"));
        private static string _connectionString = @"Data Source=..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";
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

        public void AddMissingMidiFilesToDatabase()
        {
            string[] midiFilePaths = Directory.GetFiles(_folderPath, "*.mid");

            foreach (string midiFilePath in midiFilePaths)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(midiFilePath);

                    if (!IsSongInDatabase(fileName))
                    {
                        double duration = GetMidiFileDuration(midiFilePath);

                        AddSong(fileName, duration, midiFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{midiFilePath}': {ex.Message}");
                }
            }
        }

        private bool IsSongInDatabase(string title)
        {
            string query = "SELECT COUNT(*) FROM Song WHERE Title = @Title";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", title);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count > 0;
                }
            }
        }

        private double GetMidiFileDuration(string filePath)
        {
            MidiFile midiFile = MidiFile.Read(filePath);
            var duration = midiFile.GetDuration<MetricTimeSpan>();
            return duration.TotalMicroseconds / 1_000_000.0; // Convert to seconds
        }

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

        public void DeleteSong(int songId)
        {
            string deleteSongQuery = "DELETE FROM Song WHERE ID = @SongID;";
            string deleteFavouriteQuery = "DELETE FROM Favourites WHERE SongID = @SongID;";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var favouriteCommand = new SQLiteCommand(deleteFavouriteQuery, connection))
                {
                    favouriteCommand.Parameters.AddWithValue("@SongID", songId);
                    favouriteCommand.ExecuteNonQuery();
                }

                using (var songCommand = new SQLiteCommand(deleteSongQuery, connection))
                {
                    songCommand.Parameters.AddWithValue("@SongID", songId);
                    songCommand.ExecuteNonQuery();
                }
            }
        }

        public void SaveScore(string songTitle, int score)
        {
            // Get the SongID from the Song table based on the songTitle
            int songID = GetSongIdByName(songTitle);
            Debug.WriteLine(songID);
            string scoreQuery = "INSERT INTO Score (SongID, Score) VALUES (@SongID, @Score);";

            if (songID == 0)
            {
                Debug.WriteLine($"Song with title '{songTitle}' not found.");
                return;
            }

            // Assuming you have a valid SQLite connection (dbConnection)
            using (var dbConnection = new SQLiteConnection(_connectionString))
            {
                dbConnection.Open();

                using (var cmd = new SQLiteCommand(scoreQuery, dbConnection)) // Use scoreQuery here
                {
                    cmd.Parameters.AddWithValue("@SongID", songID);
                    cmd.Parameters.AddWithValue("@Score", score);

                    cmd.ExecuteNonQuery();
                }

                Debug.WriteLine("Score successfully saved in the database.");
            }
        }

        public List<int> GetTopScores(int songID)
        {
            try { 
            var topScores = new List<int>();
            string query = @"
            SELECT Score
            FROM Score
            WHERE SongID = @SongID
            ORDER BY Score DESC
            LIMIT 3";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SongID", songID);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topScores.Add(reader.GetInt32(0)); // Get the score column
                        }
                    }
                }
            }

            return topScores;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting top scores: {ex.Message}");
                return new List<int>();
            }
        }
        public double SelectedSongDuration(string _selectedMidiName)
        {
            List<string> Nameslist = LoadMidiNames();
            List<double> DurationsList = LoadSongDuration();

            //int voor het goede nummer in de lijst
            int i = Nameslist.FindIndex(d => d.ToString().Contains(_selectedMidiName));
            if (i == -1)
                throw new Exception("Geen duur gevonden voor geselecteerde MIDI.");
            double SelectedDuration = DurationsList[i];
            return SelectedDuration;
        }
    }
}
