using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.KeyboardReplay
{
    public class KeyboardInputHandler
    {
        private readonly Action<string> notePressedCallback;
        private readonly Action<string> noteReleasedCallback;
        private readonly HashSet<string> _pressedKeys = new();

        private readonly Dictionary<string, string> keyToNote = new()
        {
            { "Q", "C2" },
            { "W", "D2" },
            { "E", "E2" },
            { "R", "F2" },
            { "T", "G2" },
            { "Y", "A2" },
            { "U", "B2" },
            { "1", "CSharp2" },
            { "2", "DSharp2" },
            { "3", "FSharp2" },
            { "4", "GSharp2" },
            { "5", "ASharp2" },
            { "A", "C3" },
            { "S", "D3" },
            { "D", "E3" },
            { "F", "F3" },
            { "G", "G3" },
            { "H", "A3" },
            { "J", "B3" },
            { "6", "CSharp3" },
            { "7", "DSharp3" },
            { "8", "FSharp3" },
            { "9", "GSharp3" },
            { "0", "ASharp3" },
            { "Z", "C4" },
            { "X", "D4" },
            { "C", "E4" },
            { "V", "F4" },
            { "B", "G4" },
            { "N", "A4" },
            { "M", "B4" },
            { "I", "CSharp4" },
            { "O", "DSharp4" },
            { "P", "FSharp4" },
            { "K", "GSharp4" },
            { "L", "ASharp4" }
        };

        public bool Enabled { get; set; } = true;

        public KeyboardInputHandler(Action<string> notePressed, Action<string> noteReleased)
        {
            notePressedCallback = notePressed;
            noteReleasedCallback = noteReleased;
        }

        public void HandleKeyDown(string key)
        {
            if (!Enabled) return;

            key = NormalizeKey(key);

            if (keyToNote.TryGetValue(key, out var note))
            {
                notePressedCallback?.Invoke(note);
                _pressedKeys.Add(note);

            }
        }

        public void HandleKeyUp(string key)
        {
            if (!Enabled) return;

            key = NormalizeKey(key);

            if (keyToNote.TryGetValue(key, out var note))
            {
                noteReleasedCallback?.Invoke(note);
                _pressedKeys.Remove(note);

            }
        }

        private string NormalizeKey(string key)
        {
            return key switch
            {
                "D1" => "1",
                "D2" => "2",
                "D3" => "3",
                "D4" => "4",
                "D5" => "5",
                "D6" => "6",
                "D7" => "7",
                "D8" => "8",
                "D9" => "9",
                "D0" => "0",
                _ => key.ToUpper()
            };
        }

        public void HandleReplayNote(string note)
        {
            notePressedCallback?.Invoke(note);
            _pressedKeys.Add(note);
        }
        public void HandleNoteRelease(string note)
        {
            noteReleasedCallback?.Invoke(note);
            _pressedKeys.Remove(note);

        }

        public void ReleaseAllKeys()
        {
            foreach (var note in _pressedKeys.ToList())
            {
                noteReleasedCallback?.Invoke(note);
            }

            _pressedKeys.Clear();
        }


    }
}
