using System.Collections.Generic;

namespace MapReduce.Models
{
    public struct BlockInfo
    {
        public int Position { get; set; }

        public List<int> WordIds { get; set; }
    }
}