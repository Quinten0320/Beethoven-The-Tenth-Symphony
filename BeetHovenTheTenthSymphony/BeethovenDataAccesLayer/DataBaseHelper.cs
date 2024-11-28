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
        private static string connectionString = @"Data Source=..\..\..\..\BeetHovenDataAccesLayer\Files\BeethovenDataBase.db;Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(@"..\..\..\..\BeetHovenDataAccesLayer\Files\BeethovenDataBase.db"))
            {
                SQLiteConnection.CreateFile(@"..\..\..\..\BeetHovenDataAccesLayer\Files\BeethovenDataBase.db");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Create tables for the data
                    string createHighscoreTableQuery = @"
                        CREATE TABLE IF NOT EXISTS highscores (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL
                        );";

                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createHighscoreTableQuery;
                        command.ExecuteNonQuery();
                    }
                }


            }
        }


    }
}
