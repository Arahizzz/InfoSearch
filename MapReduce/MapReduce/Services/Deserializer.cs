using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Collections.Pooled;
using MapReduce.Models;

namespace MapReduce.Services
{
    public static class Deserializer
    {
        private const byte LastBlockMasc = 0b1000_0000;
        private const byte LastBlockInverted = 0x7F;

        public static async IAsyncEnumerable<WordMultiDocs> Deserialize(string path)
        {
            await using var fs = new FileStream(path, FileMode.Open);
            var pipeReader = PipeReader.Create(fs);
            while (true)
            {
                var wordMultiDocs = await Deserialize(pipeReader);
                if (wordMultiDocs == null)
                    break;
                yield return wordMultiDocs.Value;
            }

            pipeReader.Complete();
        }

        public static async Task<WordMultiDocs?> Deserialize(PipeReader reader)
        {
            var result = await reader.ReadAsync();
            var sequence = result.Buffer;
            if (sequence.IsEmpty)
                return null;
            if (sequence.Length < 4)
            {
                reader.AdvanceTo(sequence.Start, sequence.End);
                result = await reader.ReadAsync();
                sequence = result.Buffer;
            }
            using var tempList = new PooledList<int>(10000);
            if (!ParseSequence(ref sequence, tempList, out var stack, out var counter, out var id, out var lastVal,
                out var read))
            {
                do
                {
                    reader.AdvanceTo(read);
                    result = await reader.ReadAsync();
                    sequence = result.Buffer;
                    if (sequence.IsEmpty)
                        return null;
                } while (!ParseSequence(ref sequence, tempList, ref stack, ref counter, ref lastVal, out read));
            }

            reader.AdvanceTo(read);
            return new WordMultiDocs {DocId = new List<int>(tempList), Word = id};
        }

        private static bool ParseSequence(ref ReadOnlySequence<byte> sequence, PooledList<int> list, out uint stack,
            out int counter,
            out int id, out int lastId,
            out SequencePosition read)
        {
            var reader = new SequenceReader<byte>(sequence);
            Span<byte> buffer = stackalloc byte[4];
            lastId = 0;
            counter = 0;
            if (reader.TryReadLittleEndian(out id))
            {
                // if (id < 0)
                //     throw new SerializationException();
                while (reader.TryRead(out var block))
                {
                    if (block == LastBlockMasc)
                    {
                        read = reader.Position;
                        lastId = 0;
                        stack = 0;
                        return true;
                    }
                    if ((block & LastBlockMasc) > 0)
                    {
                        block &= LastBlockInverted;
                        buffer[counter++] = block;
                        var val = BitConverter.ToInt32(buffer);
                        if (counter > 3) val = (val & 0x7FFFFF) | ((val >> 24) << 23);
                        if (counter > 2) val = (val & 0x7FFF) | ((val >> 16) << 15);
                        if (counter > 1) val = (val & 0x7F) | ((val >> 8) << 7);
                        val += lastId;
                        lastId = val;
                        list.Add(val);
                        buffer.Clear();
                        counter = 0;
                    }
                    else
                        buffer[counter++] = block;
                }
            }

            read = reader.Position;
            stack = BitConverter.ToUInt32(buffer);
            return false;
        }

        private static bool ParseSequence(ref ReadOnlySequence<byte> sequence, PooledList<int> list, ref uint stack,
            ref int counter,
            ref int lastId,
            out SequencePosition read)
        {
            var reader = new SequenceReader<byte>(sequence);
            Span<byte> buffer = stackalloc byte[4];
            BitConverter.GetBytes(stack).CopyTo(buffer);
            while (reader.TryRead(out var block))
            {
                if (block == LastBlockMasc)
                {
                    read = reader.Position;
                    return true;
                }
                if ((block & LastBlockMasc) > 0)
                {
                    block &= LastBlockInverted;
                    buffer[counter++] = block;
                    var val = BitConverter.ToInt32(buffer);
                    if (counter > 3) val = (val & 0x7FFFFF) | ((val >> 24) << 23);
                    if (counter > 2) val = (val & 0x7FFF) | ((val >> 16) << 15);
                    if (counter > 1) val = (val & 0x7F) | ((val >> 8) << 7);
                    val += lastId;
                    lastId = val;
                    list.Add(val);
                    buffer.Clear();
                    counter = 0;
                }
                else
                    buffer[counter++] = block;
            }

            read = reader.Position;
            stack = BitConverter.ToUInt32(buffer);
            return false;
        }
    }
}