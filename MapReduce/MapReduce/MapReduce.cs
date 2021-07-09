using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Collections.Pooled;
using MapReduce.Models;
using MapReduce.Services;
using MessagePack;
using File = MapReduce.Models.File;

namespace MapReduce
{
    public class MapReduce
    {
        private const string TempDirectory = @"D:\MapReduce\Temp";
        private const string SaveDirectory = @"D:\MapReduce\Result";
        private const long BatchSize = 150_000_000;

        private int _counter;

        private static readonly (char min, char max)[] Buckets =
        {
            ('a', 'd'), ('e', 'g'), ('h', 'p'), ('q', 'z')
        };

        private readonly Logger _logger = new Logger();

        private readonly ConcurrentDictionary<string, int> _dict =
            new ConcurrentDictionary<string, int>(12, 9_000_000);

        private int _idCounter;
        private readonly object _lock = new object();

        private int GetNewId(string _)
        {
            lock (_lock)
            {
                return ++_idCounter;
            }
        }

        private static readonly MessagePackSerializerOptions CompressionOptions =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private byte GetBucketId(char value)
        {
            for (byte i = 0; i < Buckets.Length; i++)
            {
                if (value >= Buckets[i].min && value <= Buckets[i].max)
                    return i;
            }

            return 0;
        }

        private bool InBucket(char value)
        {
            foreach ((char min, char max) in Buckets)
            {
                if (value >= min && value <= max)
                    return true;
            }

            return false;
        }

        public async Task Execute(string directoryPath)
        {
            var (batches, bigBatches) = GetBatches(directoryPath);

            PrepareFolders(batches.Concat(bigBatches));

            _logger.StartMap();
            await Map(batches, 12);
            await Map(bigBatches, 4, batches.Count);
            _logger.FinishMap();

            Console.WriteLine($"Words mapped in total: {_counter}");
            Console.WriteLine($"Unique words: {_dict.Count}");
            _logger.MappedWords = _counter;
            _logger.UniqueWords = _dict.Count;


            await DictionarySerialiazer.Serialize(Path.Combine(SaveDirectory, @"dictionary.bin"), _dict);
            _dict.Clear();

            _logger.StartReduce();
            await ReduceAsync();
            _logger.FinishReduce();
            _logger.SaveLogs(Path.Combine(SaveDirectory, "logs.txt"));
        }

        private (List<List<File>>, List<List<File>>) GetBatches(string directoryPath)
        {
            var files = Directory.GetFiles(directoryPath, "", SearchOption.AllDirectories);
            var batches = new List<List<File>>();
            var bigBatches = new List<List<File>>();
            int counter = 0;
            long totalCount = 0;
            long currentCount = 0;
            long bigCount = 0;
            var currentList = new List<File>();
            var bigList = new List<File>();

            foreach (var file in files)
            {
                long fileSize = new FileInfo(file).Length;
                totalCount += fileSize;
                if (fileSize >= BatchSize / 3)
                {
                    bigList.Add(new File {DocId = ++counter, FilePath = file});
                    bigCount += fileSize;
                    if (bigCount >= BatchSize)
                    {
                        bigBatches.Add(bigList);
                        bigList = new List<File>();
                        bigCount = 0;
                    }
                }
                else
                {
                    currentList.Add(new File {DocId = ++counter, FilePath = file});
                    currentCount += fileSize;
                    if (currentCount >= BatchSize)
                    {
                        batches.Add(currentList);
                        currentList = new List<File>();
                        currentCount = 0;
                    }
                }
            }

            if (currentList.Count > 0)
                batches.Add(currentList);
            if (bigList.Count > 0)
                bigBatches.Add(bigList);

            Console.WriteLine($"Batch count: {batches.Count + bigBatches.Count}");
            _logger.DocumentsSize = totalCount;
            _logger.BatchesCount = batches.Count + bigBatches.Count;
            return (batches, bigBatches);
        }

        private async Task Map(List<List<File>> filesBatches, int degreeOfParalelism, int addCount = 0)
        {
            var semaphore = new SemaphoreSlim(degreeOfParalelism);

            var tasks = filesBatches.Select(async (batch, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProccessFiles(batch, index + addCount);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        private static void PrepareFolders(IEnumerable<IEnumerable<File>> filesBatches)
        {
            Directory.CreateDirectory(TempDirectory);
            Directory.CreateDirectory(SaveDirectory);

            //var files = Directory.GetFiles(folderPath);
            using (var fs = new FileStream(Path.Combine(SaveDirectory, "docInfo.bin"), FileMode.Create,
                FileAccess.Write))
            {
                MessagePackSerializer.Serialize(fs, filesBatches.SelectMany(f => f));
            }

            foreach (var (min, max) in Buckets)
            {
                var dir = Path.Combine(TempDirectory, $"{min}-{max}");
                Directory.CreateDirectory(dir);
            }
        }

        private readonly Regex _word = new Regex("^\\w+$", RegexOptions.Compiled);
        private readonly string[] _delimeters = {" ", "--", ";", ".", ",", "!", "?", "*", ":", "(", ")", "[", "]"};
        private readonly char[] _trimChars =
            {'—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›', '„', '‟', '〝', '_'};

        private IEnumerable<string> GetWords(string line)
        {
            var split = line.Split(_delimeters, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < split.Length; index++)
            {
                split[index] = split[index].Trim(_trimChars);
                var s = split[index];
                if (_word.IsMatch(s) && s.Length <= 30 && InBucket(char.ToLowerInvariant(s[0])))
                    yield return s.ToLowerInvariant();
            }
        }
            
        private async Task ProccessFiles(List<File> batch, long index)
        {
            PooledList<WordMultiDocsBucket> groups;
            using (var pairs = new PooledList<WordDoc>(4000 * batch.Count, ClearMode.Always))
            {
                foreach (var file in batch)
                {
                    using var reader = new StreamReader(file.FilePath);
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        
                        foreach (var word in GetWords(line))
                        {
                            var id = _dict.GetOrAdd(word, GetNewId);
                            pairs.Add(new WordDoc {Word = id, DocId = file.DocId, Bucket = GetBucketId(word[0])});
                        }
                    }
                }

                pairs.Sort();

                groups = GroupPairs(pairs);
            }

            int size = groups.Count;

            var streams = new PipeWriter[Buckets.Length];
            for (int i = 0; i < Buckets.Length; i++)
            {
                var dir = Path.Combine(TempDirectory, $"{Buckets[i].min}-{Buckets[i].max}");
                var stream = System.IO.File.Create(Path.Combine(dir, $"{index}.bin"));
                streams[i] = PipeWriter.Create(stream);
            }

            try
            {
                foreach (var wordMultiDocsBucket in groups)
                {
                    await Serializer.Serialize(streams[wordMultiDocsBucket.Bucket], wordMultiDocsBucket.WordMultiDocs);
                }
            }
            finally
            {
                foreach (var stream in streams)
                {
                    stream.Complete();
                }

                groups.Dispose();
            }

            Console.WriteLine($"Batch #{index} has been finished");
            Console.WriteLine($"Words in batch: {size}");
            Interlocked.Add(ref _counter, size);
        }

        private PooledList<WordMultiDocsBucket> GroupPairs(PooledList<WordDoc> list)
        {
            using var enumerator = list.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            var result = new PooledList<WordMultiDocsBucket>((int) (list.Capacity * 0.8), ClearMode.Always);
            var currentPair = enumerator.Current;
            var currWord = currentPair.Word;
            var currBucket = currentPair.Bucket;
            int lastId = currentPair.DocId;
            List<int> wordIDs = new List<int>(30) {lastId};
            while (enumerator.MoveNext())
            {
                currentPair = enumerator.Current;
                if (currWord != currentPair.Word)
                {
                    result.Add(new WordMultiDocsBucket
                    {
                        WordMultiDocs = new WordMultiDocs
                        {
                            Word = currWord,
                            DocId = wordIDs
                        },
                        Bucket = currBucket
                    });
                    currWord = currentPair.Word;
                    lastId = currentPair.DocId;
                    currBucket = currentPair.Bucket;
                    wordIDs = new List<int>(30) {lastId};
                }
                else if (lastId != currentPair.DocId)
                {
                    lastId = currentPair.DocId;
                    wordIDs.Add(currentPair.DocId);
                }
            }

            return result;
        }

        private async Task ReduceAsync()
        {
            Console.WriteLine("Reduce phase has been started");
            var directories = Directory.GetDirectories(TempDirectory);
            var buckets = directories.Select(s => Task.Run(async () =>
            {
                var segments = Directory.GetFiles(s);
                var readers = new LinkedList<IAsyncEnumerator<WordMultiDocs>>();
                foreach (var t in segments)
                {
                    readers.AddLast(Deserializer.Deserialize(t).GetAsyncEnumerator());
                }

                await using var writeStream = new FileStream(
                    Path.Combine(SaveDirectory, $"{new DirectoryInfo(s).Name}.bin"),
                    FileMode.Create);
                var pipeWriter = PipeWriter.Create(writeStream);
                await MergeSegmentsAsync(readers, pipeWriter);
                await pipeWriter.CompleteAsync();
            })).ToList();
            await Task.WhenAll(buckets);
        }

        private async Task MergeSegmentsAsync(LinkedList<IAsyncEnumerator<WordMultiDocs>> readers,
            PipeWriter pipeWriter)
        {
            uint counter = 0;

            static async Task<bool> HasNext(IAsyncEnumerator<WordMultiDocs> reader)
            {
                if (!await reader.MoveNextAsync())
                {
                    await reader.DisposeAsync();
                    return false;
                }

                return true;
            }

            static async Task FilterList(LinkedList<IAsyncEnumerator<WordMultiDocs>> list, int word)
            {
                var node = list.First;
                while (node != null)
                {
                    var nextNode = node.Next;
                    if (node.Value.Current.Word == word)
                    {
                        if (!await HasNext(node.Value))
                            list.Remove(node);
                    }

                    node = nextNode;
                }
            }

            {
                var node = readers.First;
                while (node != null)
                {
                    var nextNode = node.Next;
                    if (!await HasNext(node.Value))
                        readers.Remove(node);
                    node = nextNode;
                }
            }

            while (readers.Any())
            {
                if (counter % 50000 == 0)
                    Console.WriteLine($"{counter} words proccessed");
                var current = readers.AsParallel().Select(r => r.Current.Word).Min();
                var docs = readers.AsParallel().Select(d => d.Current)
                    .Where(d => d.Word == current);
                var merged = new WordMultiDocs
                {
                    Word = current,
                    DocId = docs.SelectMany(d => d.DocId).OrderBy(i => i).ToList()
                };
                await Serializer.Serialize(pipeWriter, merged);
                counter++;

                await FilterList(readers, current);
            }
        }
    }
}