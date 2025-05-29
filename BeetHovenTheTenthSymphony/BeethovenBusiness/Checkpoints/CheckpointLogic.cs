using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Multimedia;
using System.Diagnostics;
using BeethovenBusiness.Interfaces;

namespace BeethovenBusiness.Checkpoints
{
    public class CheckpointLogic
    {
        private readonly double _selectedSongDuration;
        private readonly IData _data;
        private readonly string _connectionString;
        private readonly string _selectedMidiName;
        public CheckpointLogic(double selectedSongDuration, IData data, string connectionString, string selectedMidiName)
        {
            _selectedSongDuration = selectedSongDuration;
            _data = data;
            _connectionString = connectionString;
            _selectedMidiName = selectedMidiName;
        }

        public double RoundBasedOnFirstDecimal(double value)
        {
            // Haal de eerste decimaal op
            double fractionalPart = value - Math.Floor(value); // Geeft alleen de decimale waarde
            int firstDecimal = (int)(fractionalPart * 10); // Vermenigvuldig met 10 en cast naar int

            if (firstDecimal >= 5)
            {
                // Rond af naar boven
                return Math.Ceiling(value);
            }
            else
            {
                // Rond af naar beneden
                return Math.Floor(value);
            }
        }
        public void AddCheckpoint(double timestamp, string name)
        {

            var newCheckpoint = new Checkpoint { TimeStamp = timestamp, Name = name };

            SaveCheckpoint(GetSongID(_selectedMidiName), newCheckpoint);
        }

        public void RemoveSegment(Checkpoint checkpoint)
        {
            int songID = GetSongID(_selectedMidiName);

            DeleteCheckpoint(songID, checkpoint);


            Debug.WriteLine($"Removed checkpoint: {checkpoint.Name}, Timestamp: {checkpoint.TimeStamp}s");
        }

        public void SaveCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Bewaar de segmenten in de database, bijvoorbeeld in een SQL-tabel
            _data.SaveCheckpoint(songID, checkpoint);
        }

        public void DeleteCheckpoint(int songID, Checkpoint checkpoint)
        {
            // Verwijder het segment uit de database
            _data.DeleteCheckpoint(songID, checkpoint);
        }

        public int GetSongID(string songName)
        {
            return _data.GetSongID(songName);
        }

        public List<Checkpoint> LoadCheckpoints(int songID)
        {
            return _data.LoadCheckpoints(songID);
        }
    }
}