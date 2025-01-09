using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeethovenBusiness.MidiFileLogica
{
    public class UitlezenMidiLogica : IUitlezenMidiLogica
    {
        public double FallPercentage { get; set; } = 0;
        public double AnimationDurationUitlezenMidiLogica { get; set; } = 0;
        private IEnumerable<Note> notes;
        private bool tweeKeerOphalen = false;
        private string midiPath;
        public TempoMap tempoMap;
        private MidiFile midiFile;

        public void LaadMidiBestand(string midiPath)
        {
            this.midiPath = midiPath;
            try
            {
                midiFile = MidiFile.Read(midiPath);
                tempoMap = midiFile.GetTempoMap();
                notes = ExtractPianoNotes(midiFile);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fout bij het laden van MIDI-bestand: {ex.Message}", ex);
            }
        }

        private IEnumerable<Note> ExtractPianoNotes(MidiFile midiFile)
        {
            var pianoTracks = midiFile.GetTrackChunks()
                                      .Where(track => track.Events
                                          .OfType<ProgramChangeEvent>()
                                          .Any(ev => ev.ProgramNumber >= 0 && ev.ProgramNumber <= 7) ||
                                          !track.Events.OfType<ProgramChangeEvent>().Any())
                                      .ToList();

            if (!pianoTracks.Any())
            {
                throw new InvalidOperationException("Geen pianotracks gevonden in het MIDI-bestand.");
            }

            return pianoTracks.SelectMany(track => track.GetNotes())
                              .OrderBy(n => n.Time)
                              .ToList();
        }

        public List<Note> HaalNotenOp(double elapsedTime)
        {
            if (notes == null || tempoMap == null)
                throw new InvalidOperationException("Noten of TempoMap zijn niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var notesToPlay = new List<Note>();
            var notesToRemove = new List<Note>();

            foreach (var note in notes)
            {
                double noteTimeInSeconds = GetNoteTimeInSeconds(note);

                if (elapsedTime >= noteTimeInSeconds)
                {
                    notesToRemove.Add(note);
                    notesToPlay.Add(note);
                }
            }

            if (tweeKeerOphalen)
            {
                notes = notes.Except(notesToRemove).ToList();
                tweeKeerOphalen = false;
            }
            else
            {
                tweeKeerOphalen = true;
            }

            return notesToPlay;
        }

        public void HerlaadNoten(double elapsedTime)
        {
            LaadMidiBestand(midiPath);
            notes = notes.Where(note => elapsedTime < GetNoteTimeInSeconds(note)).ToList();
        }

        private double GetNoteTimeInSeconds(Note note)
        {
            long noteTimeInTicks = note.Time;
            MetricTimeSpan metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap);
            return metricTime.TotalSeconds;
        }
    }
}