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

            foreach (var note in notes)
            {
                // Calculate note start time in seconds
                var noteTimeInTicks = note.Time;
                var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap);
                double noteTimeInSeconds = metricTime.TotalSeconds;

                // Check if the note should be played (allowing a small threshold for precision)
                if (elapsedTime >= noteTimeInSeconds && elapsedTime <= noteTimeInSeconds + 0.05)
                {
                    notesToPlay.Add(note);
                }
            }

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





        private string GetNoteName(int midiNoteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (midiNoteNumber / 12);
            string noteName = noteNames[midiNoteNumber % 12];
            return $"{noteName}{octave}";
        }
    }
}