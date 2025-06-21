using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeethovenBusiness.MidiFileLogica
{
    public class UitlezenMidiLogica(List<int> programNumbers) : IUitlezenMidiLogica
    {
        private IEnumerable<Note> notes;
        private bool tweeKeerOphalen = false;
        private string midiPath;
        public TempoMap tempoMap;
        private MidiFile midiFile;

        //laad een midi bestand in
        public void LaadMidiBestand(string midiPath)
        {
            this.midiPath = midiPath;
            try
            {
                midiFile = MidiFile.Read(midiPath);
                tempoMap = midiFile.GetTempoMap();
                notes = ExtractNotes(midiFile);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fout bij het laden van MIDI-bestand: {ex.Message}", ex);
            }
        }

        //haalt alle noten op uit de pianotracks
        private IEnumerable<Note> ExtractNotes(MidiFile midiFile)
        {
            var pianoTracks = midiFile.GetTrackChunks()
                .Where(track =>
                    track.Events.OfType<ProgramChangeEvent>()
                        .Any(ev => programNumbers.Contains(ev.ProgramNumber)) ||
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

        //haalt alle noten op die op dit exacte moment afgespeeld moeten worden
        public List<Note> HaalNotenOp(double elapsedTime)
        {
            if (notes == null || tempoMap == null)
                throw new InvalidOperationException("Noten of TempoMap zijn niet geïnitialiseerd. Laad eerst een MIDI-bestand.");

            var notesToPlay = new List<Note>();
            var notesToRemove = new List<Note>();

            foreach (var note in notes)
            {
                double noteTimeInSeconds = GetNoteTimeInSeconds(note);

                if (elapsedTime >= noteTimeInSeconds) //als note afgespeeld had moeten worden/moet worden
                {
                    notesToRemove.Add(note);
                    notesToPlay.Add(note);
                }
            }

            if (tweeKeerOphalen) //2 keer ophalen omdat hij wordt opgehaald door de feedbacklogic en de animatielogic
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

        //verwijder noten tot een bepaald moment (bijvoorbeeld checkpoints)
        public void HerlaadNoten(double elapsedTime) 
        {
            LaadMidiBestand(midiPath);
            notes = notes.Where(note => elapsedTime < GetNoteTimeInSeconds(note)).ToList();
        }

        //haalt de tijd op wanneer de noot afgespeeld moet worden
        private double GetNoteTimeInSeconds(Note note) 
        {
            long noteTimeInTicks = note.Time;
            MetricTimeSpan metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap);
            return metricTime.TotalSeconds;
        }
    }
}