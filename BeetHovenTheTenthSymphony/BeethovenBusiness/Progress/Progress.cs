using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.Progress
{
    internal class Progress
    {
        public int ID { get; set; }
        public int totalXP { get; set; }
        public int level { get; set; }

        public Progress(int ID, int totalXP, int level)
        {
            this.ID = ID;
            this.totalXP = totalXP;
            this.level = level;
        }
    }
}
