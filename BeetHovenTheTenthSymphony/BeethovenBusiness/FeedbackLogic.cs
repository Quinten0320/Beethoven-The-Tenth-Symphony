using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeethovenBusiness;

namespace BeethovenBusiness
{
    public class FeedbackLogic
    {
        private readonly PianoInputHandler _inputHandler;
        private List<Melanchall.DryWetMidi.Interaction.Note> _notes;


        public FeedbackLogic(List<Melanchall.DryWetMidi.Interaction.Note> notes)
        {
            _notes = notes;
           
            _inputHandler = PianoInputHandlerService.Instance;
            _inputHandler.NotePressed += CheckNoteTiming;
        }

        private void CheckNoteTiming(string note)
        {
            foreach (Melanchall.DryWetMidi.Interaction.Note noteToCheck in _notes)
            {
                string noteName = noteToCheck.NoteName.ToString() + noteToCheck.Octave.ToString();
                if(noteName == note)
                {
                    Debug.WriteLine("NOTE INGEDRUKT");
                }
            }
        }
    }
}
