using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapReduce.Models;
using MapReduce.Services;
using MessagePack;
using File = MapReduce.Models.File;

namespace Reader
{
    public class Dictionary
    {
        private ConcurrentDictionary<string, int> _wordIds;

        private List<WordMultiDocs> _dictionary;

        private readonly ConcurrentDictionary<int, string> _files;

        private Dictionary(string files)
        {

            using (var filefs = new FileStream(files, FileMode.Open))
            {
                var stream = MessagePackSerializer.Deserialize<IEnumerable<File>>(filefs);
                _files = new ConcurrentDictionary<int, string>();
                foreach (var file in stream)
                {
                    _files.TryAdd(file.DocId, file.FilePath);
                }
            }
        }

        public static async Task<Dictionary> GetDictionary(string wordIds, string files, string dict)
        {
            Dictionary dictionary = new Dictionary(files);
            dictionary._wordIds = await DictionaryDeserializer.Deserialize(wordIds);
            await dictionary.FillDictionary(dict);
            return dictionary;
        }

        private async Task FillDictionary(string path)
        {
            List<WordMultiDocs> list = new List<WordMultiDocs>(2_000_000);
            await foreach (var doc in Deserializer.Deserialize(path))
            {
                list.Add(doc);
            }

            _dictionary = list;
        }

        public IEnumerable<string> Query(string word)
        {
            if (_wordIds.TryGetValue(word, out var id))
            {
                var index = _dictionary.BinarySearch(new WordMultiDocs{Word = id});
                if (index >= 0)
                    return _dictionary[index].DocId.Select(docId => _files[docId]);
            }
            return Enumerable.Empty<string>();
        }
    }
}