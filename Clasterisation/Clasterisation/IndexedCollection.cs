using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Clasterisation
{
    public class IndexedCollection
    {
        public readonly ConcurrentDictionary<string, int> _words = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<int, float> Idf = new ConcurrentDictionary<int, float>();
        
        public readonly ConcurrentDictionary<int, ConcurrentDictionary<int, int>> _dict =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, int>>();

        public readonly string[] _docs;

        private readonly object _lock = new object();
        private int _counter = -1;

        private int GetId(string word)
        {
            if (!_words.TryGetValue(word, out var id))
            {
                lock (_lock)
                {
                    return _words.GetOrAdd(word, _ => ++_counter);
                }
            }
            return id;
        }

        private void CalculateIDf()
        {
            Parallel.ForEach(_words.Values, new ParallelOptions{MaxDegreeOfParallelism = 12},word =>
            {
                var df = _dict.Values.Count(d => d.ContainsKey(word));
                Idf[word] = (float) Math.Log10((double) _docs.Length / df);
            });
        }

        public IndexedCollection(string[] docs)
        {
            _docs = docs;
        }

        public void AddWord(string word, int doc)
        {
            _dict.AddOrUpdate(doc, _ =>
            {
                var dict = new ConcurrentDictionary<int, int>();
                var id = GetId(word);
                dict[id] = 1;
                return dict;
            }, (_, dict) =>
            {
                var id = GetId(word);
                dict.AddOrUpdate(id, _ => 1, (_, freq) => freq + 1);
                return dict;
            });
        }

        public float[] GetVector(int doc)
        {
            float[] tfIdf = new float[_words.Count];
            var frequencies = _dict[doc];
            foreach (var (word, tf) in frequencies)
            {
                tfIdf[word] = tf * Idf[word];
            }
            return tfIdf;
        }

        public static IndexedCollection GetCollection(string path)
        {
            var files = Directory.GetFiles(path, "", SearchOption.AllDirectories);
            var filenames = files.Select(d => new FileInfo(d).Name).ToArray();
            var collection = new IndexedCollection(filenames);
            Parallel.ForEach(files, new ParallelOptions{MaxDegreeOfParallelism = 12},(file, _, index) =>
            {
                using var reader = new StreamReader(file);
                while (!reader.EndOfStream)
                {
                    foreach (var word in reader.ReadLine()
                        .Split(new[] {" ", "--", ";", ".", ",", "!", "?", "*", ":", "(", ")", "[", "]"},
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›',
                            '„', '‟', '〝', '_'))
                        .Where(s => Regex.IsMatch(s, "^\\w+$", RegexOptions.Compiled))
                        .Where(s => s.Length <= 30)
                        .Select(s => s.ToLowerInvariant()))
                    {
                        collection.AddWord(word, (int) index);
                    }
                }
            });
            Console.WriteLine("Finished indexing");
            collection.CalculateIDf();
            return collection;
        }
    }
}