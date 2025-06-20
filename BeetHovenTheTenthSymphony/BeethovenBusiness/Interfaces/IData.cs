using BeethovenBusiness.Achievements;
using BeethovenBusiness.Checkpoints;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeethovenBusiness.MidiFileLogica;

namespace BeethovenBusiness.Interfaces
{
    public interface IData
    {
        double GetMidiFileDuration(string filePath);
        bool IsSongInDatabase(string title);
        bool ConfirmMidi(string filePath);
        void InitializeDatabase();
        void SearchFolder();
        List<string> LoadMidiNames();
        void AddFavourite(int songId);
        bool IsSongFavourite(string songName);
        int GetSongIdByName(string songName);
        MidiFile LoadMidiFile(string name);
        void UploadMidiFile(string selectedFile);
        string getFolderPath();
        public void AddSong(string title, double duration, string filePath, List<int> programNumber);
        List<double> LoadMidiBPM();
        List<double> LoadSongDuration();
        List<int> LoadTotalNotes();
        void AddMissingMidiFilesToDatabase();
        void DeleteSong(int songId);
        void SaveScore(string songTitle, int score);
        List<int> GetTopScores(int songID);
        double SelectedSongDuration(string _selectedMidiName);
        bool AchievementExists(Achievement achievement);
        void InitializeAchievements();
        void AddAchievement(Achievement achievement);
        List<Achievement> GetAchievements();
        void DeleteAchievement(Achievement achievement);
        void UpdateAchievementStatus(Achievement achievement);
        List<Checkpoint> LoadCheckpoints(int songID);
        int GetSongID(string songName);
        void DeleteCheckpoint(int songID, Checkpoint checkpoint);
        void SaveCheckpoint(int songID, Checkpoint checkpoint);
        void UpdateDatabase(string query);
        
        bool GetIfInstrumentIsSelected(int songID, int programNumber);

        void addTrack(int programNumber, int songId);
        public void saveInstrumentList(List<TrackSettings> trackSettings, int songId);

        public List<int> GetProgramNumbersWhoNeedsToPlay(int songId);

    }
}
