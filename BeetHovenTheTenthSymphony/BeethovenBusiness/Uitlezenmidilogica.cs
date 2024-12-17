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
        public double fallPercentage = 0;
        public double animationDurationUitlezenMidiLogica = 0;
        private IEnumerable<Melanchall.DryWetMidi.Interaction.Note> notes;
        public TempoMap tempoMap;
        MidiFile midiFile;

        public void LaadMidiBestand(string midiPath)
        {
            try
            {
                // Laad het MIDI-bestand en haal de tempo map op
                midiFile = MidiFile.Read(midiPath);
                tempoMap = midiFile.GetTempoMap();

                // Vind alle trackchunks die een pianoinstrument bevatten
                var pianoTracks = midiFile.GetTrackChunks()
                                          .Where(track => track.Events
                                              .OfType<ProgramChangeEvent>()
                                              .Any(ev => ev.ProgramNumber >= 0 && ev.ProgramNumber <= 7) ||
                                              !track.Events.OfType<ProgramChangeEvent>().Any()) // Neem ook tracks zonder ProgramChangeEvent mee
                                          .ToList();

                if (!pianoTracks.Any())
                {
                    throw new InvalidOperationException("Geen pianotracks gevonden in het MIDI-bestand.");
                }

                // Haal de noten uit de pianotracks
                notes = pianoTracks
                    .SelectMany(track => track.GetNotes())
                    .OrderBy(n => n.Time)
                    .ToList();
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

            Tempo tempo = tempoMap.GetTempoAtTime((MidiTimeSpan)0);
            double microsecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote;
            return 60000000.0 / microsecondsPerQuarterNote;
        }


        public List<Melanchall.DryWetMidi.Interaction.Note> HaalNotenOp(double elapsedTime)
        {
            if (notes == null || tempoMap == null)
                throw new InvalidOperationException("Noten of TempoMap zijn niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            List<Note> notesToPlay = new List<Melanchall.DryWetMidi.Interaction.Note>();
            List<Note> notesToRemove = new List<Melanchall.DryWetMidi.Interaction.Note>();

            foreach (var note in notes.ToList())
            {
                //bereken elke noot welke tijd afgespeeld moet worden
                long noteTimeInTicks = note.Time;
                MetricTimeSpan metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap);
                double noteTimeInSeconds = metricTime.TotalSeconds;

                //als noot nu afgespeeld moet worden
                if ((elapsedTime - (animationDurationUitlezenMidiLogica * fallPercentage)) >= noteTimeInSeconds) //DIT MOET NOG AANGEPAST WORDEN NAAR + EN MET EEN TIMER
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
            var timeDivision = midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision;
            return timeDivision.TicksPerQuarterNote;
        }
}
}