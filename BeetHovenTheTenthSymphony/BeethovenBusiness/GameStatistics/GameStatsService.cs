using BeethovenBusiness.Interfaces;
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.GameStatistics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.NewFolder
{
    public class GameStatsService
    {
        public int AmountOfSongs {get; set;}
        public int AmountOfSongsThisWeek { get; set; }
        public int AmountOfSongsThisMonth { get; set; }

        public string LastPlayedSong { get; set; }
        public List<Object> scores { get; private set; }
        public double Duration { get; set; }
        public int LastScore { get; set; }
        public double AverageTimeSession { get; set; }

        private IData _data;
        private Session _sessionDetails;

        public GameStatsService(IData data)
        {
            _data = data;
            AmountOfSongs = GetTotalAmountOfSongs();
            AmountOfSongsThisMonth = GetAmountOfSongsthisMont();
            _sessionDetails = GetSessionDetails();

            LastPlayedSong = getSongDetails(_sessionDetails.SongID).Title;
            Duration = _sessionDetails.Duration;
        }

        public void SaveSessionDetails(double duration, string date, string title)
        {
            _data.saveSessionDetails(10, date, title);
        }

        public Session GetSessionDetails()
        {

            return _data.getSessionDetails();
        }

        public List<int> GetScoreBoardData()
        {
            return _data.GetScoresBySongId(_sessionDetails.SongID);
        }

        public Song getSongDetails(int SongID)
        {
            //momenteel enkel de 
            return _data.GetSongDetails(SongID);
        }

        public int GetTotalAmountOfSongs()
        {
            //Dmv van de sessions
            return _data.GetTotalAmountOfSongs();
        }

        public int GetAmountOfSongsthisMont()
        {
            return _data.GetAmountOfSongsthisMont();
        }

    }
}


