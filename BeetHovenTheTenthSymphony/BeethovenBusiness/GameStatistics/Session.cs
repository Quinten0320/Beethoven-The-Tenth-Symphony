using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.GameStatistics
{
    public class Session
    {
        public int Id { get; set; }
        public double Duration { get; set; }
        public string Date { get; set; }
        public int SongID { get; set; }
    }
}
