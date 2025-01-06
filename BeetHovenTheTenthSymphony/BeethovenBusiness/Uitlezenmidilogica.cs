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
        bool tweeKeerOphalen = false;

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

            
            // verwijder afgespeelde noten

            return notesToPlay;
        }
}
}