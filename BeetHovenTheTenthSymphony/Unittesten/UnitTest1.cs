using BeethovenBusiness;

namespace Unittesten
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void BerekenBPM()
        {
            UitlezenMidiLogica uitlezenMidiLogica = new UitlezenMidiLogica();
            string midiPath = "C:\\MIDI_Files\\MidiBestand130bpmF#major26noten.mid";
            uitlezenMidiLogica.LaadMidiBestand(midiPath);
            double bpm = uitlezenMidiLogica.BerekenBpm();
            bpm = Math.Round(bpm);
            Assert.AreEqual(130, bpm);
        }
        [Test]
        public void HaalNotenOp()
        {
            UitlezenMidiLogica uitlezenMidiLogica = new UitlezenMidiLogica();
            string midiPath = "C:\\MIDI_Files\\MidiBestand130bpmF#major26noten.mid";
            uitlezenMidiLogica.LaadMidiBestand(midiPath);
            List<Melanchall.DryWetMidi.Interaction.Note> notes = uitlezenMidiLogica.HaalNotenOp(0);
            Assert.AreEqual(1, notes.Count);
        }
        [Test]
        public void GetTicksPerBeat()
        {
            UitlezenMidiLogica uitlezenMidiLogica = new UitlezenMidiLogica();
            string midiPath = "C:\\MIDI_Files\\MidiBestand130bpmF#major26noten.mid";
            uitlezenMidiLogica.LaadMidiBestand(midiPath);
            double ticksPerBeat = uitlezenMidiLogica.GetTicksPerBeat();
            Assert.AreEqual(96, ticksPerBeat);
        }
    }
}