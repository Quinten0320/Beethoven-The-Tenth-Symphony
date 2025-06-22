using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.KeyboardReplay
{
    public class ReplayManager
    {
        private readonly List<ReplayNote> _notes = new();
        private Stopwatch _timer = new();

        public void StartRecording()
        {
            _notes.Clear();
            _timer.Restart();
        }

        public void RecordNote(string note)
        {
            _notes.Add(new ReplayNote
            {
                Note = note,
                TimeOffset = _timer.Elapsed
            });
        }

        public async Task ReplayAsync(Action<string> playCallback)
        {
            var start = DateTime.Now;
            var notesCopy = _notes.ToList();

            foreach (var replayNote in notesCopy)
            {
                var waitTime = replayNote.TimeOffset - (DateTime.Now - start);
                if (waitTime > TimeSpan.Zero)
                    await Task.Delay(waitTime);

                playCallback(replayNote.Note);
            }
        }

        private class ReplayNote
        {
            public string Note { get; set; }
            public TimeSpan TimeOffset { get; set; }
        }
    }
}
