using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapReduce.Models;

namespace MapReduce.Services
{
    public static class DictionaryDeserializer
    {
        private static readonly ReadOnlyMemory<byte> Delimeter = new byte[] {255, 255};

        public static async Task<ConcurrentDictionary<string, int>> Deserialize(string path)
        {
            var dict = new ConcurrentDictionary<string, int>();
            await using var fs = new FileStream(path, FileMode.Open);
            var pipe = PipeReader.Create(fs);
            var result = await pipe.ReadAsync();
            var sequence = result.Buffer;
            if (sequence.Length < 8)
                return null;
            if (!ReadTable(ref sequence, out var table, out var blockSize,
                out var read, out var current,
                out var position, out var list))
            {
                do
                {
                    pipe.AdvanceTo(read);
                    result = await pipe.ReadAsync();
                    sequence = result.Buffer;
                } while (!ReadTable(ref sequence, table, blockSize, out read, ref current, ref position, ref list));
            }

            pipe.AdvanceTo(read);
            result = await pipe.ReadAsync();
            sequence = result.Buffer;

            foreach (var blockInfo in table)
            {
                var wordIds = new List<WordId>();
                string prefix = null;
                if (sequence.IsEmpty)
                {
                    result = await pipe.ReadAsync();
                    sequence = result.Buffer;
                }
                while (!ReadBlock(wordIds, ref sequence, blockInfo, ref prefix))
                {
                    pipe.AdvanceTo(sequence.Start, sequence.End);
                    result = await pipe.ReadAsync();
                    sequence = result.Buffer;
                }

                foreach (var wordId in wordIds)
                {
                    dict.TryAdd(wordId.Word, wordId.Id);
                }

                pipe.AdvanceTo(sequence.Start);
            }

            return dict;
        }

        private static bool ReadBlock(List<WordId> list, ref ReadOnlySequence<byte> sequence,
            BlockInfo info, ref string prefix)
        {
            var encoding = Encoding.Unicode;
            Span<byte> numBuff = stackalloc byte[1];
            Span<byte> buffer = stackalloc byte[200];
            int counter = 0;
            if (prefix == null)
            {
                var id = info.WordIds.First();
                sequence.Slice(counter, 1).CopyTo(numBuff);
                var wordLength = numBuff[0];
                var askedSize = 2 * (wordLength + 1);
                if (sequence.Length < askedSize + 1)
                    return false;
                var wordSpan = sequence.Slice(1, askedSize);
                counter += (int) wordSpan.Length + 1;
                wordSpan.CopyTo(buffer);
                int index = buffer.IndexOf(Delimeter.Span);
                prefix = encoding.GetString(buffer.Slice(0, index));
                string second = encoding.GetString(buffer.Slice(index + 2, 2 * (wordLength - prefix.Length)));
                list.Add(new WordId {Id = id, Word = prefix + second});
                buffer.Clear();
                sequence = sequence.Slice(counter);
                counter = 0;
            }

            for (var i = list.Count; i < info.WordIds.Count; i++)
            {
                if (sequence.IsEmpty)
                    return false;
                var id = info.WordIds[i];
                sequence.Slice(counter, 1).CopyTo(numBuff);
                var wordLength = numBuff[0];
                var askedSize = 2 * wordLength;
                if (sequence.Length < askedSize + 1)
                    return false;
                var wordSpan = sequence.Slice(1, 2 * wordLength);
                counter += (int) wordSpan.Length + 1;
                wordSpan.CopyTo(buffer);
                string second = encoding.GetString(buffer.Slice(0, 2*wordLength));
                list.Add(new WordId {Id = id, Word = prefix + second});
                buffer.Clear();
                sequence = sequence.Slice(counter);
                counter = 0;
            }

            prefix = null;
            return true;
        }

        private static void ReadTableInfo(ref SequenceReader<byte> reader, out int blockSize, out int count)
        {
            reader.TryReadLittleEndian(out blockSize);
            reader.TryReadLittleEndian(out count);
        }

        private static bool ReadTable(ref ReadOnlySequence<byte> sequence, out BlockInfo[] table, out int blockSize,
            out SequencePosition read, out int current, out int position, out List<int> list)
        {
            var reader = new SequenceReader<byte>(sequence);

            ReadTableInfo(ref reader, out blockSize, out var count);

            table = new BlockInfo[count];

            for (current = 0; current < count; current++)
            {
                list = new List<int>(blockSize);
                if (!reader.TryReadLittleEndian(out position))
                {
                    read = reader.Position;
                    return false;
                }

                for (int j = 0; j < blockSize; j++)
                {
                    if (!reader.TryReadLittleEndian(out int wordId))
                    {
                        read = reader.Position;
                        return false;
                    }

                    if (wordId != 0)
                        list.Add(wordId);
                }

                table[current] = new BlockInfo {Position = position, WordIds = list};
            }

            position = -1;
            read = reader.Position;
            list = null;
            return true;
        }

        private static bool ReadTable(ref ReadOnlySequence<byte> sequence, BlockInfo[] table, int blockSize,
            out SequencePosition read, ref int current, ref int position, ref List<int> list)
        {
            var reader = new SequenceReader<byte>(sequence);

            for (; current < table.Length; current++)
            {
                if (position == -1 && !reader.TryReadLittleEndian(out position))
                {
                    read = reader.Position;
                    return false;
                }

                for (int j = list.Count; j < blockSize; j++)
                {
                    if (!reader.TryReadLittleEndian(out int wordId))
                    {
                        read = reader.Position;
                        return false;
                    }

                    if (wordId != 0)
                        list.Add(wordId);
                }

                table[current] = new BlockInfo {Position = position, WordIds = list};
                list = new List<int>();
                position = -1;
            }

            position = -1;
            read = reader.Position;
            return true;
        }

        public static bool TryRead3Bytes(this ref SequenceReader<byte> reader, out int value)
        {
            if (reader.Sequence.Length < 3)
            {
                value = 0;
                return false;
            }
            Span<byte> buffer = stackalloc byte[4];
            for (int i = 0; i < 3; i++)
            {
                reader.TryRead(out buffer[i]);
            }
            value = BitConverter.ToInt32(buffer);
            return true;
        }
    }
}