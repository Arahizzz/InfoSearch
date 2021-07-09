using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using BinaryDictionary;
using Fb2.Document;
using Fb2.Document.Models;

namespace FB2Reader
{
    public static class Reader
    {
        public static async Task<SetDictionary> DictionaryFromFiles(string path)
        {
            var files = Directory.GetFiles(path);
            var dict = new SetDictionary(files.Select(s => new FileInfo(s).Name).ToArray());
            for (var index = 0; index < files.Length; index++)
            {
                var file = files[index];
                await using var fs = new FileStream(file, FileMode.Open);
                var reader = new Fb2Document();
                await reader.LoadAsync(fs);


                var nodes = reader.Bodies
                    .SelectMany(b => b.Content)
                    .Select(c => c.ToXml())
                    .SelectMany(x => x.DescendantNodes().OfType<XText>());

                foreach (var word in nodes.SelectMany(n =>
                        n.Value.Split(new[] {" ", "--", ";", ".", ",", "!", "?", "*", ":", "(", ")", "[", "]"},
                            StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›',
                        '„', '‟', '〝', '_'))
                    .Where(s => Regex.IsMatch(s, "^\\w+$", RegexOptions.Compiled))
                    .Where(s => s.Length <= 30)
                    .Select(s => s.ToLowerInvariant()))
                {
                    dict.AddWord(word, index);
                }

                var titleInfo = reader.Title.Content.ToList();
                var authors = titleInfo.OfType<Author>();
                var genres = titleInfo.OfType<BookGenre>();

                foreach (var author in authors.SelectMany(a => a.Content)
                    .SelectMany(c => c.ToXml().Value.Split(new[] {" ", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim('—', '’', '-', '“', '”', '"', '\'', '°', '«', '‹', '»', '›',
                        '„', '‟', '〝', '_'))
                    .Select(s => s.ToLowerInvariant()))
                {
                    dict.AddWord(author, index);
                }
                
                foreach (var genre in genres.Select(a => a.Content)
                    .SelectMany(s => s.Split(new[]{" ", "_"}, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.ToLowerInvariant()))
                {
                    dict.AddWord(genre, index);
                }
            }

            return dict;
        }
    }
}