using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Interaction;

namespace BeethovenBusiness.Interfaces
{
    public interface IPlayback
    {
        void Start();
        void Stop();
        void Dispose();
        void MoveToTime(MetricTimeSpan time);
        bool IsRunning { get; }
        event EventHandler Finished;
    }
}
