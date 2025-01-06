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
        public void HaalNotenOp()
        {
            UitlezenMidiLogica uitlezenMidiLogica = new UitlezenMidiLogica();
            string midiPath = "C:\\MIDI_Files\\MidiBestand130bpmF#major26noten.mid";
            uitlezenMidiLogica.LaadMidiBestand(midiPath);
            List<Melanchall.DryWetMidi.Interaction.Note> notes = uitlezenMidiLogica.HaalNotenOp(0);
            Assert.AreEqual(1, notes.Count);
        }
    }
}