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

        private IData _data;

        public GameStatsService(IData data)
        {
            _data = data;
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

        public void GetTotalAmountOfSongs()
        {
            //Dmv van de sessions
            _data.GetTotalAmountOfSongs();
        }
    }
}


