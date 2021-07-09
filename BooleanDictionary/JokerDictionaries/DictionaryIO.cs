using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MessagePack;
using MessagePack.Resolvers;

namespace BinaryDictionary
{
    public static class DictionaryIo
    {
        public static SetDictionary SetDictionaryFromFiles(string folderPath)
        {
            var files = Directory.GetFiles(folderPath).ToArray();
            var fileNames = files.Select(f => new FileInfo(f))
                .Select(i => i.Name).ToArray();
            var dictionary = new SetDictionary(fileNames);
            FillDictionary(files, dictionary);
            return dictionary;
        }

        public static void FillDictionary(string[] files, IBooleanDictionary dictionary)
        {
            int counter = 0;
            long sizeCounter = 0;
            //Parallel.ForEach(files, (filePath, _, index) =>
            for (int index = 0; index < files.Length; index++)
            {
                var filePath = files[index];
                var fileInfo = new FileInfo(filePath);
                int fileCounter = 0;
                using var reader = new StreamReader(filePath);
                while (!reader.EndOfStream)
                {
                    foreach (var word in reader.ReadLine().Split(" ")
                        .Select(s => s.Trim('.', ';', ',', '—', '-', '?', '!', '*'))
                        .Where(s => Regex.IsMatch(s, "\\w+"))
                        .Select(s => s.ToLowerInvariant()))
                    {
                        fileCounter++;
                        dictionary.AddWord(word, (int) index);
                    }
                }

                Console.WriteLine(
                    $"File {filePath}     Size: {fileInfo.Length / 1000}KB     Word Count: {fileCounter}");
                Interlocked.Add(ref counter, fileCounter);
                Interlocked.Add(ref sizeCounter, fileInfo.Length);
            } //);

            Console.WriteLine($"Words processed: {counter}      Actual dictionary size: {dictionary.Size}");
            Console.WriteLine($"Total file sizes sum: {sizeCounter / 1000}KB");
        }

        public static PermutationDictionary PermutationFromFiles(string folderPath)
        {
            var files = Directory.GetFiles(folderPath).ToArray();
            var fileNames = files.Select(f => new FileInfo(f))
                .Select(i => i.Name).ToArray();
            var dictionary = new PermutationDictionary(fileNames);
            for (int i = 0; i < files.Length; i++)
            {
                var filePath = files[i];
                using var reader = new StreamReader(filePath);
                while (!reader.EndOfStream)
                {
                    foreach (var word in reader.ReadLine()
                        .Split(new[] {" ", "--", ";", ".", ",", "!", "?", "*", ":", "(", ")", "[", "]"},
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›', '„', '‟', '〝',
                            '_'))
                        .Where(s => Regex.IsMatch(s, "\\w+"))
                        .Select(s => s.ToLowerInvariant()))
                    {
                        dictionary.AddWord(word, i);
                    }
                }
            }
            return dictionary;
        }


        public static ThreeGrammDictionary ThreeGrammFromFiles(string folderPath)
        {
            var files = Directory.GetFiles(folderPath).ToArray();
            var fileNames = files.Select(f => new FileInfo(f))
                .Select(i => i.Name).ToArray();
            var dictionary = new ThreeGrammDictionary(fileNames); 
            for (int i = 0; i < files.Length; i++)
            {
                var filePath = files[i];
                using var reader = new StreamReader(filePath);
                while (!reader.EndOfStream)
                {
                    foreach (var word in reader.ReadLine()
                        .Split(new[] {" ", "--", ";", ".", ",", "!", "?", "*", ":", "(", ")", "[", "]"},
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›', '„', '‟', '〝',
                            '_'))
                        .Where(s => Regex.IsMatch(s, "\\w+"))
                        .Select(s => s.ToLowerInvariant()))
                    {
                        dictionary.AddWord(word, i);
                    }
                }
            }
            return dictionary;
        }

        public static void Save<T>(T dictionary, string path)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            MessagePackSerializer.Serialize(fs, dictionary,
                StandardResolverAllowPrivate.Options.WithCompression(MessagePackCompression
                    .Lz4BlockArray));
        }

        public static T FromSaveFile<T>(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            return MessagePackSerializer.Deserialize<T>(fs,
                StandardResolverAllowPrivate.Options.WithCompression(MessagePackCompression
                    .Lz4BlockArray));
        }
    }
}