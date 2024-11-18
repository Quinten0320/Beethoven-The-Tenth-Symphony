using Melanchall.DryWetMidi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BeetHovenWPF
{
    public partial class MainWindow : Window
    {
        private IEnumerable<Melanchall.DryWetMidi.Interaction.Note> notes;
        private DateTime startTime; 
        private TempoMap tempoMap;  

        public MainWindow()
        {
            InitializeComponent();

           
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.0 / 120); // 120 FPS
            timer.Tick += timer_Tick;
            timer.Start();

            string midiPath = @"C:\Users\quint\Downloads\nyan cat.mid.mid";

            try
            {
                var midiFile = MidiFile.Read(midiPath);
                tempoMap = midiFile.GetTempoMap(); 
                notes = midiFile.GetNotes();

                
                notes = notes.OrderBy(n => n.Time).ToList();

                startTime = DateTime.Now; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het lezen van MIDI-bestand: {ex.Message}");
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                double elapsedTime = (DateTime.Now - startTime).TotalSeconds;

                var notesToPlay = new StringBuilder(); 
                var notesToRemove = new List<Melanchall.DryWetMidi.Interaction.Note>();

                foreach (var note in notes.ToList())
                {
                   
                    var noteTimeInTicks = note.Time;
                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(noteTimeInTicks, tempoMap);
                    double noteTimeInSeconds = metricTime.TotalSeconds;

                   
                    if (elapsedTime >= noteTimeInSeconds)
                    {
                        string noteName = GetNoteName(note.NoteNumber);

                        notesToPlay.AppendLine($"Note {noteName} speelt af op {elapsedTime:F2} seconden");

                        notesToRemove.Add(note);
                    }
                }

                if (notesToPlay.Length > 0)
                {
                    NotesTextBox.AppendText(notesToPlay.ToString()); 

                    foreach (var note in notesToRemove)
                    {
                        notes = notes.Where(n => n != note).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij timer tick: {ex.Message}");
            }
        }

        private string GetNoteName(int midiNoteNumber)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            int octave = (midiNoteNumber / 12) - 1;

            string noteName = noteNames[midiNoteNumber % 12];

            return $"{noteName}{octave}";
        }
    }
}
