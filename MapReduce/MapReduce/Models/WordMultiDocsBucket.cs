using System;

namespace MapReduce.Models
{
    internal struct WordMultiDocsBucket : IComparable<WordMultiDocsBucket>
    {
        public WordMultiDocs WordMultiDocs { get; set; }

        public byte Bucket { get; set; }

        public int CompareTo(WordMultiDocsBucket other) => WordMultiDocs.CompareTo(other.WordMultiDocs);
    }
}