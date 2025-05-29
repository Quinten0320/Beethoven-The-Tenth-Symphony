using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.Achievements
{
    public class Achievement
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DatumBehaald { get; set; }
        public bool IsBehaald { get; set; }

        public Achievement(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

}
