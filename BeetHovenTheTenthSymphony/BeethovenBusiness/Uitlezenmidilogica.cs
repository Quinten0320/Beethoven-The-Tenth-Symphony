using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeethovenBusiness
{
    public class UitlezenMidiLogica
    {
        private IEnumerable<Melanchall.DryWetMidi.Interaction.Note> notes;
        private TempoMap tempoMap;

        
        public void LaadMidiBestand(string midiPath)
        {
            try
            {
                var midiFile = MidiFile.Read(midiPath);
                tempoMap = midiFile.GetTempoMap();
                notes = midiFile.GetNotes().OrderBy(n => n.Time).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fout bij het laden van MIDI-bestand: {ex.Message}", ex);
            }
        }

        
        public double BerekenBpm()
        {
            if (tempoMap == null)
                throw new InvalidOperationException("TempoMap is niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var tempo = tempoMap.GetTempoAtTime((MidiTimeSpan)0);
            double microsecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote;
            return 60_000_000.0 / microsecondsPerQuarterNote;
        }

        
        public List<string> HaalNotenOp(double elapsedTime)
        {
            if (notes == null || tempoMap == null)
                throw new InvalidOperationException("Noten of TempoMap zijn niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var notesToPlay = new List<string>();
            var notesToRemove = new List<Melanchall.DryWetMidi.Interaction.Note>();

            foreach (var note in notes.ToList())
            {
                //bereken elke noot welke tijd afgespeeld moet worden
                var noteTimeInTicks = note.Time;
                var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap); 
                double noteTimeInSeconds = metricTime.TotalSeconds;

                //als noot nu afgespeeld moet worden
                if (elapsedTime >= noteTimeInSeconds)
                {
                    string noteName = GetNoteName(note.NoteNumber);
                    notesToPlay.Add($"Note {noteName} speelt op {elapsedTime:F2} seconden");
                    notesToRemove.Add(note);
                }
            }

            // verwijder afgespeelde noten
            notes = notes.Where(n => !notesToRemove.Contains(n)).ToList();

            return notesToPlay;
        }

        public void telishnabidad()
        {

        }
        
        private string GetNoteName(int midiNoteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (midiNoteNumber / 12);
            string noteName = noteNames[midiNoteNumber % 12];
            return $"{noteName}{octave}";
        }
    }
}
