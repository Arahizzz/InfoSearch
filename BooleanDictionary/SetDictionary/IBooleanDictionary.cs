using System.Collections.Generic;

namespace BinaryDictionary
{
    public interface IBooleanDictionary
    {
        public void AddWord(string word, int document);

        public string GetDocument(int index);

        public IEnumerable<string> GetContainingDocuments(string word);
        
        public int Size { get; }

        public IEnumerable<string> And(params string[] words);
        
        public IEnumerable<string> Or(params string[] words);
    }
}