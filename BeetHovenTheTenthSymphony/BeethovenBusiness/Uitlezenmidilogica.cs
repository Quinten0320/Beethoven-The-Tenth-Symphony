﻿using Melanchall.DryWetMidi.Core;
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
        MidiFile midiFile;

        public void LaadMidiBestand(string midiPath)
        {
            try
            {
                midiFile = MidiFile.Read(midiPath);
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
            return 60000000.0 / microsecondsPerQuarterNote;
        }


        public List<Melanchall.DryWetMidi.Interaction.Note> HaalNotenOp(double elapsedTime)
        {
            if (notes == null || tempoMap == null)
                throw new InvalidOperationException("Noten of TempoMap zijn niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var notesToPlay = new List<Melanchall.DryWetMidi.Interaction.Note>();
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
                    notesToPlay.Add(note);
                    notesToRemove.Add(note);
                }
            }

            // verwijder afgespeelde noten
            notes = notes.Where(n => !notesToRemove.Contains(n)).ToList();

            return notesToPlay;
        }
        public double GetTicksPerBeat()
        {
            if (tempoMap == null)
                throw new InvalidOperationException("TempoMap is niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var timeDivision = midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision;
            if (timeDivision == null)
                throw new InvalidOperationException("TimeDivision is niet van het type TicksPerQuarterNoteTimeDivision.");

            return timeDivision.TicksPerQuarterNote;
        }
        public long getMaxLength()
        {
            var longestNote = notes.OrderByDescending(n => n.Length).First();
            long longesnotelength = longestNote.Length;
            return longesnotelength;
        }
        public double BerekenGemiddeldeLengte()
        {
            if (notes == null || !notes.Any())
                throw new InvalidOperationException("Noten zijn niet geïnitialiseerd of de lijst is leeg. Laad eerst een MIDI-bestand.");

            return notes.Average(n => n.Length);
        }
}
}