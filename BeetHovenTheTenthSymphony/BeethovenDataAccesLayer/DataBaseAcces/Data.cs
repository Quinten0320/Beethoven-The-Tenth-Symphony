using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.Achievements;
using BeethovenBusiness.Progress;
using System.Diagnostics;

using System.IO;
using System.Security.Policy;
using BeethovenBusiness.Checkpoints;
using BeethovenBusiness.MidiFileLogica;

namespace BeethovenDataAccesLayer.DataBaseAcces
{
    public class Data : IData
    {
        private string _folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\BeethovenDataAccesLayer\MidiFiles"));
        private static string _connectionString = @"Data Source=..\..\..\..\..\BeethovenDataAccesLayer\BeethovenDataBase.db;Version=3";
        
        public void InitializeDatabase()
        {
            DataBaseHelper.InitializeDatabase();
        }

        public void UpdateDatabase(string query)
        {
            DataBaseHelper.UpdateDatabase(query);
        }
        
        
        public bool GetIfInstrumentIsSelected(int songID, int programNumber)
        {
            string query = @"
            SELECT IsPlayed
            FROM Tracks
            WHERE SongID = @SongID and ProgramNumber = @ProgramNumber";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SongID", songID);
                    command.Parameters.AddWithValue("@ProgramNumber", programNumber);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException(
                                $"No track found for SongID={songID} and ProgramNumber={programNumber}.");
                        }

                        return Convert.ToBoolean(reader["IsPlayed"]);
                    }
                }
            }
        }

        public void saveInstrumentList(List<TrackSettings> trackSettings, int songId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var track in trackSettings)
                {
                    string query = @"
                    UPDATE tracks
                    SET IsPlayed = @IsPlayed
                    WHERE SongID = @SongID AND ProgramNumber = @ProgramNumber";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SongID", songId);
                        command.Parameters.AddWithValue("@ProgramNumber", track.programNumber);
                        command.Parameters.AddWithValue("@IsPlayed", track.IsSelected);
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<int> GetProgramNumbersWhoNeedsToPlay(int songId)
        {
            List<int> programNumbers = new List<int>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT ProgramNumber
                    FROM tracks
                    WHERE SongID = @SongID AND IsPlayed = 1;";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SongID", songId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                programNumbers.Add(reader.GetInt32(0)); // index 0 = eerste kolom: ProgramNumber
                            }
                        }
                    }
            }
            return programNumbers;
        }

        public void SearchFolder()
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

        #region GeenAchievements

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

        public bool ConfirmMidi(string filePath)
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
                    MidiFile midiFile = MidiFile.Read(midiFilePath);


                    List<int> usedProgramNumbers = midiFile.GetTrackChunks()
                        .SelectMany(track => track.Events.OfType<ProgramChangeEvent>())
                        .Select(pc => (int)pc.ProgramNumber)
                        .Distinct()
                        .OrderBy(num => num)
                        .ToList();
                    
                    if (!IsSongInDatabase(fileName))
                    {
                        double duration = GetMidiFileDuration(midiFilePath);

                        AddSong(fileName, duration, midiFilePath, usedProgramNumbers);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{midiFilePath}': {ex.Message}");
                }
            }
        }

        public bool IsSongInDatabase(string title)
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

        public double GetMidiFileDuration(string filePath)
        {
            MidiFile midiFile = MidiFile.Read(filePath);
            var duration = midiFile.GetDuration<MetricTimeSpan>();
            return duration.TotalMicroseconds / 1_000_000.0; // Convert to seconds
        }

        public void AddSong(string title, double duration, string filePath, List<int> programNumbers)
        {
            string insertSongQuery = @"
            INSERT INTO Song (Title, Duration, FilePath)
            VALUES (@Title, @Duration, @FilePath);";

            int songId;
            
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(insertSongQuery, connection))
                {
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Duration", duration);
                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid();";
                    command.Parameters.Clear();
                    songId = Convert.ToInt32(command.ExecuteScalar());
                }
                foreach (int programNumber in programNumbers)
                {
                    addTrack(programNumber, songId);
                }
            }
        }

        public void addTrack(int programNumber, int songId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                
                string insertTrackQuery = @"
                        INSERT INTO Tracks (ProgramNumber, SongId, IsPlayed)
                        VALUES (@ProgramNumber, @SongId, 1);"; // automaties wel afspelen 
                
                using (var trackCommand = new SQLiteCommand(insertTrackQuery, connection))
                {
                    trackCommand.Parameters.AddWithValue("@ProgramNumber", programNumber);
                    trackCommand.Parameters.AddWithValue("@SongId", songId);
                    trackCommand.ExecuteNonQuery();
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

        #endregion

        #region Achievements

        public bool AchievementExists(Achievement achievement)
        {
            string query = "SELECT COUNT(*) FROM Achievements WHERE Name = @Name";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", achievement.Name);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }


        public void InitializeAchievements()
        {

            var achievements = new List<Achievement>();
            achievements.Add(new Achievement("Eerste Noot", "Speel je eerste noot."));
            achievements.Add(new Achievement("First Song", "Speel een volledig liedje."));
            achievements.Add(new Achievement("Practice Mode", "Gebruik oefenmodus."));
            achievements.Add(new Achievement("Perfect Score", "Speel een noot tot op de miliseconde perfect."));
            achievements.Add(new Achievement("Early Bird", "Speel voor 8 uur 's ochtends."));
            achievements.Add(new Achievement("Night Owl", "Speel na 10 uur 's avonds."));
            achievements.Add(new Achievement("Noob", "Behaal 0 score op een level"));

            foreach (var achievement in achievements)
            {
                if (!AchievementExists(achievement))
                {
                    AddAchievement(achievement);
                }
            }
            if (DateTime.Now.TimeOfDay < new TimeSpan(8, 0, 0))
            {
                Achievement achievement = achievements.Where(a => a.Name == "Early Bird").FirstOrDefault();
                UpdateAchievementStatus(achievement);
            }
            else if (DateTime.Now.TimeOfDay > new TimeSpan(22, 0, 0))
            {
                Achievement achievement = achievements.Where(a => a.Name == "Night Owl").FirstOrDefault();
                UpdateAchievementStatus(achievement);
            }
        }


        public void AddAchievement(Achievement achievement)
        {
            string insertAchievementQuery = @"
            INSERT INTO Achievements (Name, Description, DatumBehaald, IsBehaald)
            VALUES (@Name, @Description, NULL, 0);";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(insertAchievementQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", achievement.Name);
                    command.Parameters.AddWithValue("@Description", achievement.Description);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateAchievementStatus(Achievement achievement)
        {
            string updateAchievementQuery = @"
            UPDATE Achievements
            SET IsBehaald = 1, DatumBehaald = datetime('now') 
            WHERE Name = @AchievementNaam;";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(updateAchievementQuery, connection))
                {
                    command.Parameters.AddWithValue("@AchievementNaam", achievement.Name);
                    command.ExecuteNonQuery();
                }
            }
        }

        
        public List<Achievement> GetAchievements()
        {
            List<Achievement> achievements = new List<Achievement>();
            string query = "SELECT Name, Description, DatumBehaald, IsBehaald FROM Achievements";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Achievement achievement = new Achievement(
                            reader.GetString(0),
                            reader.GetString(1))
                        {
                            DatumBehaald = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                            IsBehaald = reader.GetBoolean(3)
                        };
                        achievements.Add(achievement);
                    }
                }
            }
            return achievements;
        }

        public void DeleteAchievement(Achievement achievement)
        {
            string deleteAchievementQuery = "DELETE FROM Achievements WHERE Name = @Name;";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(deleteAchievementQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", achievement.Name);
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Checkpoint
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

        #endregion

        #region Progress

        public int GetCurrentLevel()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM Progress", connection);
                var count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count == 0)
                {
                    var insertCommand = new SQLiteCommand("INSERT INTO Progress (TotalXP, Level) VALUES (0, 1)", connection);
                    insertCommand.ExecuteNonQuery();
                }

                var command = new SQLiteCommand("SELECT Level FROM Progress ORDER BY ID DESC LIMIT 1", connection);
                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int level))
                {
                    return level;
                }
                return 1;
            }
        }

        public int GetCurrentXP()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT TotalXP FROM Progress ORDER BY ID DESC LIMIT 1", connection);
                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int xp))
                {
                    return xp;
                }
                return 0;
            }
        }

        public void AddXP(int xp)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                var selectCmd = new SQLiteCommand("SELECT ID, TotalXP, CurrentXP, Level FROM Progress ORDER BY ID DESC LIMIT 1", connection);
                using (var reader = selectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        int totalXP = reader.GetInt32(1);
                        int currentXP = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        int level = reader.GetInt32(3);

                        int newTotalXP = totalXP + xp;
                        int newCurrentXP = currentXP + xp;

                        int levelUps = newCurrentXP / 1000;
                        newCurrentXP = newCurrentXP % 1000;
                        level += levelUps;

                        var updateCmd = new SQLiteCommand(
                            "UPDATE Progress SET TotalXP = @totalXP, CurrentXP = @currentXP, Level = @level WHERE ID = @id", connection);
                        updateCmd.Parameters.AddWithValue("@totalXP", newTotalXP);
                        updateCmd.Parameters.AddWithValue("@currentXP", newCurrentXP);
                        updateCmd.Parameters.AddWithValue("@level", level);
                        updateCmd.Parameters.AddWithValue("@id", id);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        int levelUps = xp / 1000;
                        int currentXP = xp % 1000;
                        int level = 1 + levelUps;

                        var insertCmd = new SQLiteCommand(
                            "INSERT INTO Progress (TotalXP, CurrentXP, Level) VALUES (@totalXP, @currentXP, @level)", connection);
                        insertCmd.Parameters.AddWithValue("@totalXP", xp);
                        insertCmd.Parameters.AddWithValue("@currentXP", currentXP);
                        insertCmd.Parameters.AddWithValue("@level", level);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        #endregion
    }
}
