using System;
using System.IO;

namespace MapReduce.Services
{
    internal class Logger
    {
        private DateTime _mapStart;
        private DateTime _mapFinish;
        private DateTime _reduceStart;
        private DateTime _reduceFinish;

        public int MappedWords { get; set; }

        public int UniqueWords { get; set; }

        public int BatchesCount { get; set; }
        
        public long DocumentsSize { get; set; }

        public void StartMap() => _mapStart = DateTime.Now;

        public void FinishMap() => _mapFinish = DateTime.Now;

        public void StartReduce() => _reduceStart = DateTime.Now;

        public void FinishReduce() => _reduceFinish = DateTime.Now;

        public TimeSpan MapDuration() => _mapFinish - _mapStart;

        public TimeSpan ReduceDuration() => _reduceFinish - _reduceStart;

        public void SaveLogs(string path)
        {
            using var sw = new StreamWriter(path);
            sw.WriteLine($"Initial Collection Size: {DocumentsSize / 1000}KB");
            sw.WriteLine($"Batches count: {BatchesCount}");
            sw.WriteLine($"Total words mapped: {MappedWords}");
            sw.WriteLine($"Unique words: {UniqueWords}");
            var mapDur = MapDuration();
            sw.WriteLine($"Map duration: {mapDur.Hours} hours {mapDur.Minutes} minutes {mapDur.Seconds} seconds");
            var redDur = ReduceDuration();
            sw.WriteLine($"Reduce duration: {redDur.Hours} hours {redDur.Minutes} minutes {redDur.Seconds} seconds");
        }
    }
}