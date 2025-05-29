using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeethovenDataAccesLayer.DataBaseAcces;

namespace BeethovenBusiness.Interfaces
{
    public static class DataFactory
    {
        public static IData CreateDefaultData()
        {
            return new Data();
        }
    }
}
