using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dictionary;
using MessagePack;
using static Dictionary.Trie;

namespace BinaryDictionary
{
    public class PermutationDictionary
    {
        private const char Endofword = '\u0003';

        private readonly ConcurrentDictionary<string, string> _dictionary =
            new ConcurrentDictionary<string, string>();

        private readonly ForwardTrie _index = new ForwardTrie();

        private readonly SetDictionary _documents;

        public PermutationDictionary(string[] documents)
        {
            _documents = new SetDictionary(documents);
        }

        private bool AddPermutation(string permutation, string word) => _dictionary.TryAdd(permutation, word);

        private void CycleSwap(char[] array)
        {
            for (int i = 0; i < array.Length - 1; i++)
            {
                var buffer = array[i];
                array[i] = array[i + 1];
                array[i + 1] = buffer;
            }
        }

        [IgnoreMember] public int Size => _dictionary.Values.Distinct().Count();

        public void AddWord(string word, int docID)
        {
            var charArray = new char[word.Length + 1];
            for (int i = word.Length - 1; i >= 0; i--)
                charArray[i] = word[i];
            charArray[word.Length] = Endofword;
            _index.AddWord(charArray);
            if (!AddPermutation(new string(charArray), word)) return;
            {
                for (int i = 0; i < charArray.Length - 1; i++)
                {
                    CycleSwap(charArray);
                    _index.AddWord(charArray);
                    AddPermutation(new string(charArray), word);
                }

                _documents.AddWord(word, docID);
            }
        }

        public SortedSetVector GetWords(string query)
        {
            int occurrences = query.Occurrences('*');

            char[] GetArray(string word)
            {
                var charArray = new char[word.Length + 1];
                for (int i = word.Length - 1; i >= 0; i--)
                    charArray[i] = word[i];
                charArray[word.Length] = Endofword;

                while (charArray[^1] != '*')
                    CycleSwap(charArray);

                return charArray;
            }

            if (occurrences == 1)
            {
                var charArray = GetArray(query);
                return new SortedSetVector(_index.Words(new string(charArray, 0, charArray.Length - 1))
                    .Select(p => _dictionary[p]), _documents);
            }

            if (occurrences == 2)
            {
                var strings = query.Split('*');
                var word = strings[0] + '*' + strings[2];
                var charArray = GetArray(word);
                return new SortedSetVector(_index.Words(new string(charArray, 0, charArray.Length - 1))
                    .Select(p => _dictionary[p]).Where(w => w.Contains(strings[1])), _documents);
            }

            return new SortedSetVector(_documents);
        }
    }

    public static class MyExtensions
    {
        public static int Occurrences(this string s, char c)
        {
            int counter = 0;
            foreach (var character in s)
            {
                if (character == c)
                    counter++;
            }

            return counter;
        }

        public static string ReplaceBetween(this string s, int x, int y, string with)
        {
            string textBefore = s.Substring(0, x);
            string textAfter = s.Substring(y + 1);
            return textBefore + with + textAfter;
        }
    }
}