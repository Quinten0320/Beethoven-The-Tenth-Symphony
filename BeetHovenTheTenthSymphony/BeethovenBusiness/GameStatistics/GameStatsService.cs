using BeethovenBusiness.Interfaces;
using System;
using System.Collections.Generic;
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

        private IData _data;

        public GameStatsService(IData data)
        {
            _data = data;
            AmountOfSongs = GetTotalAmountOfSongs();
            AmountOfSongsThisMonth = GetAmountOfSongsthisMont();

        }

        // TODO: fix de datums
        public void SaveSessionDetails(double duration, DateTime date, string title)
        {
            _data.saveSessionDetails(10, "04-02-2024", title);
        }

        public void GetSessionDetails()
        {

        }

        public void getSongDuration()
        {

        }

        public void calculateTotalInGameTime()
        {

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


