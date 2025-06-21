using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Security.Policy;

namespace BeethovenDataAccesLayer.DataBaseAcces
{
    public static class DataBaseHelper
    {

        private static string _connectionString = @"Data Source=..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(@"..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db"))
            {
                SQLiteConnection.CreateFile(@"..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db");


                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //Create tables for the data
                    string createScoreTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Score (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SongID INTEGER NOT NULL,
                            Score INT NOT NULL,
                            FOREIGN KEY(SongID) REFERENCES Song(ID)
                        );";

                    string createCheckpointTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Checkpoint (
                            songID INTEGER NOT NULL,
                            Timestamp REAL NOT NULL,
                            Name TEXT,
                            PRIMARY KEY (songID, Timestamp),
                            FOREIGN KEY (songID) REFERENCES Song(songID)
                        )";

                    string createSongTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Song (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            Duration DOUBLE NOT NULL,
                            FilePath VARCHAR NOT NULL
                        );";

                    string createFavouritesTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Favourites (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SongID INT NOT NULL,
                            FOREIGN KEY(SongID) REFERENCES Song(ID)
                        );";

                    string createTracksTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Tracks (
                            ProgramNumber INTEGER NOT NULL,
                            SongID INTEGER NOT NULL,
                            IsPlayed BOOLEAN NOT NULL,
                            PRIMARY KEY (ProgramNumber, SongID),
                            FOREIGN KEY (SongID) REFERENCES Song(ID)
                        );";

                    string createProgressTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Progress (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            TotalXP INTEGER NOT NULL DEFAULT 0,
                            CurrentXP INTEGER NOT NULL DEFAULT 0,   
                            Level INTEGER NOT NULL DEFAULT 1
                        );";

                    string createChallengesTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Challenges (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            GoalType TEXT NOT NULL,
                            GoalValue INTEGER NOT NULL,
                            RewardXP INTEGER NOT NULL
                        );";

                    string createChallengeProgressTableQuery = @"
                        CREATE TABLE IF NOT EXISTS ChallengeProgress (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            ChallengeId INTEGER NOT NULL,
                            CurrentValue INTEGER DEFAULT 0,
                            IsCompleted BOOLEAN DEFAULT 0,
                            FOREIGN KEY (ChallengeId) REFERENCES Challenges(ID)
                        );";


                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createScoreTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createCheckpointTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createSongTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createFavouritesTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createTracksTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createProgressTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createChallengesTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createChallengeProgressTableQuery;
                        command.ExecuteNonQuery();

                    }
                }
            }
        }

        public static void UpdateDatabase(string query)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
