using BeethovenBusiness.Interfaces;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.PreviewLogic
{
    public class PreviewLogic
    {
        private double durationInSeconds; 
        private readonly IData _data;

        public PreviewLogic(IData data)
        {
            _data = data;
        }

        public string GetDuration(string name)
        {
            durationInSeconds = _data.SelectedSongDuration(name);
            int min = (int)durationInSeconds / 60;
            int sec = (int)durationInSeconds % 60;
            return $"{min}:{sec:00}";
        }
    }
}
