using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BinaryDictionary
{
    public struct SortedSetVector : IEnumerable<string>
    {
        private readonly SortedSet<string> _set;
        private readonly SetDictionary _dictionary;

        public SortedSetVector(SortedSet<string> set, SetDictionary dictionary)
        {
            _set = set;
            _dictionary = dictionary;
        }

        public SortedSetVector(IEnumerable<string> words, SetDictionary dictionary)
        {
            _dictionary = dictionary;
            _set = new SortedSet<string>(words);
        }

        public SortedSetVector(SetDictionary dictionary)
        {
            _set = new SortedSet<string>();
            _dictionary = dictionary;
        }

        public int Length => _set.Count;

        public static SortedSetVector operator &(SortedSetVector v1, SortedSetVector v2)
        {
            var set = new SortedSet<string>(v1._set);
            set.IntersectWith(v2._set);
            return new SortedSetVector(new SortedSet<string>(set), v1._dictionary);
        }
        
        public static SortedSetVector operator |(SortedSetVector v1, SortedSetVector v2)
        {
            var set = new SortedSet<string>(v1._set);
            set.UnionWith(v2._set);
            return new SortedSetVector(new SortedSet<string>(set), v1._dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        public SetDictionary.SetVector GetContainingDocs() => _dictionary.VOr(_set.ToArray());
    }
}