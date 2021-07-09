using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using MessagePack;

namespace MapReduce.Models
{
    [MessagePackObject]
    public struct WordMultiDocs : IComparable<WordMultiDocs>
    {
        [Key(0)] public int Word { get; set; }
        [Key(1)] public List<int> DocId { get; set; }

        public int CompareTo(WordMultiDocs other) => Word.CompareTo(other.Word);
    }
}