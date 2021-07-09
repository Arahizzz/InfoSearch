﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MessagePack;

namespace BinaryDictionary
{
    [MessagePackObject]
    public class SetDictionary : IBooleanDictionary
    {
        [Key(0)] protected readonly ConcurrentDictionary<string, HashSet<int>> _dictionary;

        [Key(1)] protected readonly string[] _documents;

        public SetDictionary(string[] documents)
        {
            _documents = documents;
            _dictionary = new ConcurrentDictionary<string, HashSet<int>>(1, _documents.Length * 25000);
        }

        [SerializationConstructor]
        private SetDictionary()
        {
        } //For serialization purposes

        public void AddWord(string word, int document)
        {
            _dictionary.AddOrUpdate(word, _ => new HashSet<int> {document}, (_, files) =>
            {
                files.Add(document);
                return files;
            });
        }

        public string GetDocument(int index) => _documents[index];

        private HashSet<int> GetSet(string word)
        {
            if (_dictionary.TryGetValue(word.ToLowerInvariant(), out var value))
                return value;
            else
                return new HashSet<int>();
        }

        public SetVector this[string word] => new SetVector(this, GetSet(word));

        public IEnumerable<string> GetContainingDocuments(string word) => GetSet(word).Select(GetDocument);

        public IEnumerable<string> And(params string[] words) => VAnd(words);

        public IEnumerable<string> Or(params string[] words) => VOr(words);

        public SetVector VAnd(params string[] words)
        {
            if (words.Length > 0)
            {
                var sets = words.Select(GetSet);
                var newSet = sets.Skip(1).Aggregate(
                    new HashSet<int>(sets.First()),
                    (main, other) =>
                    {
                        main.IntersectWith(other);
                        return main;
                    });
                return new SetVector(this, newSet);
            }

            return new SetVector(this);
        }

        public SetVector VOr(params string[] words)
        {
            if (words.Length > 0)
            {
                var sets = words.Select(GetSet);
                var newSet = sets.Skip(1).Aggregate(
                    new HashSet<int>(sets.First()),
                    (main, other) =>
                    {
                        main.UnionWith(other);
                        return main;
                    });
                return new SetVector(this, newSet);
            }

            return new SetVector(this);
        }

        public SetVector GetPhrase(string phrase)
        {
            var words = phrase.ToLowerInvariant().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var biWords = words.Skip(1).Zip(words, (second, first) => first + " " + second).ToArray();
            return VAnd(biWords);
        }

        [IgnoreMember] public int Size => _dictionary.Count;

        public struct SetVector : IEnumerable<string>
        {
            private readonly SetDictionary _dictionary;
            private readonly HashSet<int> _set;

            internal SetVector(SetDictionary dictionary, HashSet<int> set)
            {
                _dictionary = dictionary;
                _set = set;
            }

            internal SetVector(SetDictionary dictionary)
            {
                _dictionary = dictionary;
                _set = new HashSet<int>();
            }

            public int Length => _set.Count;

            public static SetVector operator |(SetVector v1, SetVector v2) =>
                new SetVector(v1._dictionary, v1._set.Union(v2._set).ToHashSet());

            public static SetVector operator &(SetVector v1, SetVector v2) =>
                new SetVector(v1._dictionary, v1._set.Intersect(v2._set).ToHashSet());

            public static SetVector operator !(SetVector v)
            {
                var documents = v._dictionary._documents;
                var inverted = Enumerable.Range(0, documents.Length).Where(i => !v._set.Contains(i));
                return new SetVector(v._dictionary, inverted.ToHashSet());
            }

            public IEnumerator<string> GetEnumerator() => _set.Select(_dictionary.GetDocument).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}