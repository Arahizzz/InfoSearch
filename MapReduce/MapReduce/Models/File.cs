using System;
using MessagePack;

namespace MapReduce.Models
{
    [MessagePackObject]
    public struct File : IComparable<File>
    {
        [Key(0)] public int DocId { get; set; }
        [Key(1)] public string FilePath { get; set; }
        
        public int CompareTo(File other) => DocId.CompareTo(other.DocId);
    }
}