using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MessagePack;

namespace BinaryDictionary
{
    [MessagePackObject()]
    public class ThreeGrammDictionary
    {
        [IgnoreMember] private const char EndStartofword = '\u0003';

        [Key(0)] private readonly ConcurrentDictionary<string, SortedSet<string>> _dictionary =
            new ConcurrentDictionary<string, SortedSet<string>>();

        private readonly SetDictionary _documents;

        public ThreeGrammDictionary(string[] documents)
        {
            _documents = new SetDictionary(documents);
        }

        public void AddGramm(string gramm, string word)
        {
            _dictionary.AddOrUpdate(gramm, new SortedSet<string> {word}, (_, set) =>
            {
                set.Add(word);
                return set;
            });
        }

        public void AddWord(string word, int docID)
        {
            if (word.Length > 3)
            {
                AddGramm(EndStartofword + word.Substring(0, 2), word);
                for (var i = 0; i < word.Length - 2; i++)
                {
                    AddGramm(word.Substring(i, 3), word);
                }

                AddGramm(word.Substring(word.Length - 2, 2) + EndStartofword, word);
            }
            else if (word.Length == 2)
            {
                AddGramm(EndStartofword + word.Substring(0, 2), word);
                AddGramm(word.Substring(0, 2) + EndStartofword, word);
            }
            else
            {
                AddGramm(EndStartofword + word + EndStartofword, word);
            }

            _documents.AddWord(word, docID);
        }

        public SortedSetVector StartsWith(string word)
        {
            if (word.Length >= 2)
            {
                var res = GetVector(EndStartofword + word.Substring(0, 2));
                for (int i = 0; i < word.Length - 2; i++)
                {
                    res &= GetVector(word.Substring(i, 3));
                }

                return new SortedSetVector(res.Where(w => w.StartsWith(word)), _documents);
            }

            return new SortedSetVector(new SortedSet<string>(), _documents);
        }

        public SortedSetVector EndsWith(string word)
        {
            if (word.Length >= 2)
            {
                var res = GetVector(word.Substring(word.Length - 2, 2) + EndStartofword);
                for (int i = word.Length - 1; i > 1; i--)
                {
                    res &= GetVector(word.Substring(i - 2, 3));
                }

                return new SortedSetVector(res.Where(w => w.EndsWith(word)), _documents);
            }

            return new SortedSetVector(new SortedSet<string>(), _documents);
        }

        public SortedSetVector StartAndEnds(string start, string ends) => StartsWith(start) & EndsWith(ends);

        private SortedSetVector GetVector(string word)
        {
            if (_dictionary.TryGetValue(word, out var words))
            {
                return new SortedSetVector(words, _documents);
            }

            return new SortedSetVector(new SortedSet<string>(), _documents);
        }

        public SortedSetVector GetWord(string word)
        {
            if (word.Length >= 2)
            {
                if (_dictionary.ContainsKey(EndStartofword + word.Substring(0, 2)))
                {
                    for (int i = 0; i < word.Length - 2; i++)
                    {
                        if (!_dictionary.ContainsKey(word.Substring(i, 3)))
                            return new SortedSetVector(_documents);
                    }

                    return new SortedSetVector(new SortedSet<string>{word}, _documents);
                }

                return new SortedSetVector(_documents);
            }

            return new SortedSetVector(_documents);
        }

        // public void Test()
        // {
        //     var sw = Stopwatch.StartNew();
        //     var query = _dictionary.Values
        //         .Select(s => new GrammVector(s))
        //         .Aggregate((f, s) => f & s);
        //     sw.Stop();
        //     Console.WriteLine(sw.ElapsedMilliseconds);
        // }
    }
}