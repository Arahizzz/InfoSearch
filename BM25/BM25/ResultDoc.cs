using System;

namespace BM25
{
    public struct ResultDoc : IComparable<ResultDoc>
    {
        public string Name { get; set; }
        public double Score { get; set; }
        
        public int CompareTo(ResultDoc other) => Score.CompareTo(other.Score);
    }
}