using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapReduce.Models;

namespace MapReduce.Services
{
    public static class DictionarySerialiazer
    {
        private static readonly ReadOnlyMemory<byte> Delimeter = new byte[] {255, 255};
        private static readonly Encoding Encode = Encoding.Unicode;

        public static async Task Serialize(string path, IDictionary<string, int> dictionary, int blockSize = 3)
        {
            await using var fs = new FileStream(path, FileMode.Create);
            int tableSize = 8 + (int) Math.Ceiling((double) dictionary.Count / blockSize) * 4 * (blockSize + 1);
            fs.Seek(tableSize, SeekOrigin.Begin);
            var pipe = PipeWriter.Create(fs, new StreamPipeWriterOptions(leaveOpen: true));
            int position = 0;
            var buffer = new List<KeyValuePair<string, int>>();
            var table = new List<BlockInfo>();
            foreach (var kvp in dictionary.OrderBy(kvp => kvp.Key))
            {
                buffer.Add(kvp);
                if (buffer.Count == blockSize)
                {
                    await NewBlock(table, buffer, pipe, ref position, blockSize);
                }
            }

            if (buffer.Any())
                await NewBlock(table, buffer, pipe, ref position, blockSize);

            await pipe.CompleteAsync();

            fs.Seek(0, SeekOrigin.Begin);

            pipe = PipeWriter.Create(fs);
            await WriteTable(table, pipe, tableSize, blockSize);
            await pipe.CompleteAsync();
        }

        private static ValueTask<FlushResult> WriteTable(List<BlockInfo> info, PipeWriter pipe, int tableSize,
            int blockSize)
        {
            var span = pipe.GetSpan(tableSize);

            BitConverter.GetBytes(blockSize).CopyTo(span);
            BitConverter.GetBytes(info.Count).CopyTo(span.Slice(4));
            int counter = 8;
            foreach (var row in info)
            {
                BitConverter.GetBytes(row.Position).CopyTo(span.Slice(counter));
                counter += 4;

                foreach (var id in row.WordIds)
                {
                    BitConverter.GetBytes(id).CopyTo(span.Slice(counter));
                    counter += 4;
                }

                if (row.WordIds.Count != blockSize)
                {
                    for (int i = 0; i < tableSize - counter; i += 4)
                    {
                        BitConverter.GetBytes(0).CopyTo(span.Slice(counter));
                    }
                }
            }

            pipe.Advance(tableSize);
            return pipe.FlushAsync();
        }

        private static ValueTask<FlushResult> NewBlock(List<BlockInfo> table, List<KeyValuePair<string, int>> buffer,
            PipeWriter pipe, ref int position, int blockSize)
        {
            table.Add(new BlockInfo
            {
                Position = position,
                WordIds = buffer.Select(pair => pair.Value).ToList()
            });
            int count = WriteBlock(pipe, buffer, blockSize);
            position += count;
            pipe.Advance(count);
            buffer.Clear();
            return pipe.FlushAsync();
        }

        private static int WriteBlock(PipeWriter pipe,
            List<KeyValuePair<string, int>> buffer, int blockSize)
        {
            var strings = new List<string>(buffer.Select(pair => pair.Key));
            var span = pipe.GetSpan(strings.Aggregate(0, (acc, s) => acc + s.Length) + blockSize);
            int prefix = GetLongestCommonPrefix(strings);
            int counter = 0;
            int bytes;
            var encoder = Encode.GetEncoder();
            {
                var first = strings.First().AsSpan();
                BitConverter.GetBytes((byte) first.Length).AsSpan().Slice(0, 1).CopyTo(span.Slice(counter));
                counter++;
                encoder.Convert(first.Slice(0, prefix), span.Slice(counter),
                    true, out _, out bytes, out _);
                counter += bytes;
                Delimeter.Span.CopyTo(span.Slice(counter));
                counter += Delimeter.Length;
                encoder.Convert(first.Slice(prefix, first.Length - prefix), span.Slice(counter),
                    true, out _, out bytes, out _);
                counter += bytes;
            }
            for (int i = 1; i < strings.Count; i++)
            {
                var s = strings[i].AsSpan();
                BitConverter.GetBytes(s.Length - prefix).AsSpan().Slice(0, 1).CopyTo(span.Slice(counter));
                counter++;
                encoder.Convert(s.Slice(prefix, s.Length - prefix), span.Slice(counter),
                    true, out _, out bytes, out _);
                counter += bytes;
            }

            return counter;
        }

        public static int GetLongestCommonPrefix(List<string> s)
        {
            int k = s[0].Length;
            for (int i = 1; i < s.Count; i++)
            {
                k = Math.Min(k, s[i].Length);
                for (int j = 0; j < k; j++)
                    if (s[i][j] != s[0][j])
                    {
                        k = j;
                        break;
                    }
            }

            return k;
        }
    }
}