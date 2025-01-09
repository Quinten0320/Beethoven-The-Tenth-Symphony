using BeethovenDataAccesLayer;
using System;
using System.Collections.Generic;
using BeethovenDataAccesLayer.DataBaseAcces;
using Melanchall.DryWetMidi.Multimedia;
using System.Data.SQLite;
using System.Diagnostics;

namespace BeethovenBusiness.Checkpoints
{
    public class CheckpointLogic
    {
        private readonly double _selectedSongDuration;
        private readonly Data _data;
        private readonly string _connectionString;
        private readonly string _selectedMidiName;
        public CheckpointLogic(double selectedSongDuration, Data data, string connectionString, string selectedMidiName)
        {
            _selectedSongDuration = selectedSongDuration;
            _data = data;
            _connectionString = connectionString;
            _selectedMidiName = selectedMidiName;
        }

        public double RoundBasedOnFirstDecimal(double value)
        {
            // Haal de eerste decimaal op
            double fractionalPart = value - Math.Floor(value); // Geeft alleen de decimale waarde
            int firstDecimal = (int)(fractionalPart * 10); // Vermenigvuldig met 10 en cast naar int

            if (firstDecimal >= 5)
            {
                // Rond af naar boven
                return Math.Ceiling(value);
            }
            else
            {
                // Rond af naar beneden
                return Math.Floor(value);
            }
        }
        public void AddCheckpoint(double timestamp, string name)
        {

            var newCheckpoint = new Checkpoint { TimeStamp = timestamp, Name = name };
            
            SaveCheckpoint(GetSongID(_selectedMidiName), newCheckpoint);
        }

        public void RemoveSegment(Checkpoint checkpoint)
        {
            int songID = GetSongID(_selectedMidiName);

            DeleteCheckpoint(songID, checkpoint);


            Debug.WriteLine($"Removed checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
        }

        public void SaveCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Bewaar de segmenten in de database, bijvoorbeeld in een SQL-tabel
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("INSERT INTO Checkpoint (songID, Timestamp, Name) VALUES (@songID, @Timestamp, @Name)", connection);

                command.Parameters.AddWithValue("@songID", songID);
                command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                command.Parameters.AddWithValue("@Name", checkpoint.Name);

                command.ExecuteNonQuery();
            }
        }

        public void DeleteCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Verwijder het segment uit de database
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
            "DELETE FROM Checkpoint WHERE songID = @songID AND Timestamp = @Timestamp",
            connection
        );

                command.Parameters.AddWithValue("@songID", songID);
                command.Parameters.AddWithValue("@Timestamp", checkpoint.TimeStamp);
                command.ExecuteNonQuery();
            }
        }

        public int GetSongID(string songName)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT ID FROM Song WHERE Title = @songName", connection);
                command.Parameters.AddWithValue("@songName", songName);

                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int songID))
                {
                    return songID;
                }
            }
            throw new Exception($"Nummer met naam '{songName}' niet gevonden in de database.");
        }

        public List<Checkpoint> LoadCheckpoints(int songID)
        {

            var checkpoints = new List<Checkpoint>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "SELECT Timestamp, Name FROM Checkpoint WHERE songID = @songID",
                    connection
                );

                command.Parameters.AddWithValue("@songID", songID);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var checkpoint = new Checkpoint
                        {
                            TimeStamp = Convert.ToDouble(reader["Timestamp"]),
                            Name = reader["Name"]?.ToString()
                        };
                        checkpoints.Add(checkpoint);
                    }
                }
            }

            return checkpoints;
        }

    }
}