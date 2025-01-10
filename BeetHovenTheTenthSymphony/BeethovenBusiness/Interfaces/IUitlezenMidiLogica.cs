using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness
{
    public interface IUitlezenMidiLogica
    {
        void LaadMidiBestand(string midiPath);
        List<Note> HaalNotenOp(double elapsedTime);
        void HerlaadNoten(double elapsedTime);
    }
}

