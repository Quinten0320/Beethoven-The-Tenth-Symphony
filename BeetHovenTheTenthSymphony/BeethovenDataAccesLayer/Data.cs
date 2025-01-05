using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;

using System.Data.SQLite;

using System.Diagnostics;

using System.IO;

namespace BeethovenDataAccesLayer
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

        public static void SaveScore(string songTitle, double songDuration, string filePath, int score)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertScoreQuery = @"
                    INSERT INTO Score (Score)
                    VALUES (@Score);
                    SELECT last_insert_rowid();";

                        long scoreId;
                        using (var command = new SQLiteCommand(insertScoreQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Score", score);
                            scoreId = (long)command.ExecuteScalar();
                        }

                        string insertSongQuery = @"
                    INSERT INTO Song (Title, Duration, FilePath, Checkpoint, ScoreID)
                    VALUES (@Title, @Duration, @FilePath, @Checkpoint, @ScoreID);";

                        using (var command = new SQLiteCommand(insertSongQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Title", songTitle);
                            command.Parameters.AddWithValue("@Duration", songDuration);
                            command.Parameters.AddWithValue("@FilePath", filePath);
                            command.Parameters.AddWithValue("@Checkpoint", DBNull.Value);
                            command.Parameters.AddWithValue("@ScoreID", scoreId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Debug.WriteLine("Score en lied opgeslagen in de database.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Fout bij opslaan in database: {ex.Message}");
                        throw;
                    }
                }
            }
        }


    }
}
