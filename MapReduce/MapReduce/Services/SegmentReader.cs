using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MapReduce.Models;
using MessagePack;

namespace MapReduce.Services
{
    internal class SegmentReader : IEnumerator<WordMultiDocs>
    {
        private int _counter;
        public readonly int SegmentSize;
        private readonly FileStream _stream;
        public WordMultiDocs Current { get; private set; }

        public SegmentReader(string path)
        {
            _stream = new FileStream(path, FileMode.Open);
            Span<byte> bytes = stackalloc byte[4];
            _stream.Read(bytes);
            SegmentSize = BitConverter.ToInt32(bytes);
        }


        public void Dispose()
        {
            _stream.Dispose();
        }

        public bool MoveNext()
        {
            if (_counter++ >= SegmentSize)
                return false;
            Current = MessagePackSerializer.Deserialize<WordMultiDocs>(_stream);
            return true;
        }

        public void Reset()
        {
            _stream.Seek(4, SeekOrigin.Begin);
            _counter = 0;
        }

        object IEnumerator.Current => Current;
    }
}