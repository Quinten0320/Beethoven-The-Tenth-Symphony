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

                    string createSessionTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Session (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Duration DOUBLE NOT NULL,
                            Date TEXT,
                            SongID INTERGER NOT NULL,
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
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createScoreTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createSessionTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createCheckpointTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createSongTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createFavouritesTableQuery;
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
