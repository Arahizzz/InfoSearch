using System;

namespace MapReduce.Models
{
    internal struct WordDoc : IComparable<WordDoc>
    {
        public int Word { get; set; }
        public int DocId { get; set; }

        public byte Bucket { get; set; }

        public int CompareTo(WordDoc other)
        {
            int cmp = Word.CompareTo(other.Word);
            if (cmp < 0)
                return -1;
            if (cmp > 0)
                return 1;
            return DocId.CompareTo(other.DocId);
        }
    }
}