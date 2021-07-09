using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MapReduce.Models;

namespace MapReduce.Services
{
    public static class Serializer
    {
        private const byte LastBlockMasc = 0b1000_0000;
        private static readonly Memory<byte> EndOfWord;

        static Serializer()
        {
            Memory<byte> bytes = BitConverter.GetBytes(LastBlockMasc);
            EndOfWord = bytes.Slice(0, 1);
        }

        private static int GetBytesCount(int val)
        {
            if (val >= 0x80)
            {
                if (val >= 0x4000)
                {
                    if (val >= 0x20_0000)
                    {
                        return val >= 0x1000_0000 ? 5 : 4;
                    }

                    return 3;
                }

                return 2;
            }

            return 1;
        }

        private static int WriteDoc(PipeWriter writer, WordMultiDocs wordMultiDocs)
        {
            var writeSpan = writer.GetSpan(4 * (wordMultiDocs.DocId.Count + 2));
            var docs = wordMultiDocs.DocId;
            //writeSpan.Slice(0, 4).Clear();
            // if (wordMultiDocs.Word < 0)
            //     throw  new SerializationException("AAAAA");
            BitConverter.GetBytes(wordMultiDocs.Word).CopyTo(writeSpan);
            int counter = 4;
            int lastId = 0;
            foreach (var doc in docs)
            {
                var current = doc - lastId;
                lastId = doc;
                var bytesCount = GetBytesCount(current);
                if (bytesCount > 1)
                {
                    current = (current & 0x7F) | ((current >> 7) << 8);
                    if (bytesCount > 2)
                    {
                        current = (current & 0x7FFF) | ((current >> 15) << 16);
                        if (bytesCount > 3)
                        {
                            current = (current & 0x7FFFFF) | ((current >> 23) << 24);
                            if (bytesCount > 4)
                                throw new SerializationException(
                                    "Cannot serialize a number that is bigger than 28 bits");
                        }
                    }
                }

                Span<byte> byteNum = BitConverter.GetBytes(current);
                byteNum[bytesCount - 1] |= LastBlockMasc;
                var segment = writeSpan.Slice(counter, bytesCount);
                byteNum.Slice(0, bytesCount).CopyTo(segment);
                counter += bytesCount;
            }

            EndOfWord.Span.CopyTo(writeSpan.Slice(counter, 4));
            counter += 1;

            return counter;
        }

        public static ValueTask<FlushResult> Serialize(PipeWriter writer, WordMultiDocs docs)
        {
            var count = WriteDoc(writer, docs);
            writer.Advance(count);
            return writer.FlushAsync();
        }

        public static async Task Serialize(string path, List<WordMultiDocs> docs)
        {
            using var fs = new FileStream(path, FileMode.Create);
            var pipeWriter = PipeWriter.Create(fs, new StreamPipeWriterOptions());
            foreach (var wordMultiDocs in docs)
            {
                var count = WriteDoc(pipeWriter, wordMultiDocs);
                pipeWriter.Advance(count);
                await pipeWriter.FlushAsync();
            }

            pipeWriter.Complete();
        }
    }
}