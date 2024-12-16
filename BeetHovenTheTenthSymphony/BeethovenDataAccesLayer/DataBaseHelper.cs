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
        private static string connectionString = @"Data Source=..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(@"..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db"))
            {
                SQLiteConnection.CreateFile(@"..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db");


                using (var connection = new SQLiteConnection(connectionString))
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


    }
}
