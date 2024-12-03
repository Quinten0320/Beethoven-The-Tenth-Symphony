using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace BeethovenDataAccesLayer
{
    public static class DataBaseHelper
    {
        private static string connectionString = @"Data Source=..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(@"..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db"))
            {
                SQLiteConnection.CreateFile(@"..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Create tables for the data
                    string createScoreTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Score (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Score INT NOT NULL
                        );";

                    string createSongTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Song (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            Duration INT NOT NULl,
                            FilePath VARCHAR NOT NULL,
                            Checkpoint INT,
                            ScoreID INT,
                            FOREIGN KEY (ScoreID) REFERENCES Score(ScoreID)
                        );";

                    string createFavouritesTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Favourites (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SongID INT NOT NULL
                            FOREIGN KEY (SongID) REFERENCES Song(SongID)
                        );";

                    string createUserTableQuery = @"
                        CREATE TABLE IF NOT EXISTS User (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            FavouritesID INT,
                            FOREIGN KEY (FavouritesID) REFERENCES Favourites(FavouritesID)
                        );";

                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createScoreTableQuery;
                        command.CommandText = createSongTableQuery;
                        command.CommandText = createFavouritesTableQuery;
                        command.CommandText = createUserTableQuery;

                        command.ExecuteNonQuery();
                    }
                }


            }
        }


    }
}
