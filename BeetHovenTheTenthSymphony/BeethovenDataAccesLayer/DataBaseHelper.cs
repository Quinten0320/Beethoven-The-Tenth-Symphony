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

namespace BeethovenDataAccesLayer
{
    public static class DataBaseHelper
    {
        private static string _connectionString = @"Data Source=..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(@"..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db"))
            {
                SQLiteConnection.CreateFile(@"..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db");


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
                            Score INT NOT NULL
                        );";

                    string createSongTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Song (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            Duration INT NOT NULL,
                            FilePath VARCHAR NOT NULL,
                            Checkpoint INT,
                            ScoreID INT,
                            FOREIGN KEY (ScoreID) REFERENCES Score(ID)
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

                        command.CommandText = createSongTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createFavouritesTableQuery;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void AddSong(string title, int duration, string filePath)
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

        public static void AddFavourite(int songId)
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
        public static bool IsSongFavourite(string songName)
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

        public static int GetSongIdByName(string songName)
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


        public static void GetDataInfoDEBUG()                                         //DEBUG
        {
            string query = "SELECT Id, Title, Duration FROM Song;";  // song query
            //string query = "SELECT Id, SongID FROM Favourites;";   // favourite query
            var info = new List<string>();

            try
            {            
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        // Voor Song tabel
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string title = reader.GetString(1);
                            int duration = reader.GetInt32(2);

                            info.Add($"Id: {id}, Title: {title}, Duration: {duration}");
                        }
                        // Voor Favourite tabel
                        /*while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            int songId = reader.GetInt32(1);

                            info.Add($"Id: {id}, SongId: {songId}");
                        }*/
                    }
                }
                foreach (var info_ in info)
                {
                    Debug.WriteLine(info_);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
    }
}
