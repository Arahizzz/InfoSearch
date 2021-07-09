using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Clasterisation
{
    public class VectorSpace
    {
        private readonly ConcurrentDictionary<int, float[]> _vectors;

        private readonly ConcurrentDictionary<int, float[]> _champions;

        private readonly ConcurrentDictionary<int, List<int>> _clusters;

        public VectorSpace(IndexedCollection collection)
        {
            var championCount = (int) Math.Sqrt(collection._docs.Length);
            Console.WriteLine("Making vectors");
            _vectors = new ConcurrentDictionary<int, float[]>(12, collection._docs.Length);
            _champions = new ConcurrentDictionary<int, float[]>(12, championCount);
            _clusters = new ConcurrentDictionary<int, List<int>>();
            Parallel.For(0, collection._docs.Length, i =>
            {
                _vectors[i] = collection.GetVector(i);
            });
            GetChampions(championCount);
            Console.WriteLine("Constructing Clusters");
            MakeClusters();
            DisplayStats();
        }

        private void DisplayStats()
        {
            Console.WriteLine($"Total files: {_vectors.Count + _champions.Count}");
            Console.WriteLine($"Champions count: {_champions.Count}");
            foreach (var champion in _champions.Keys)
            {
                Console.Write($"{champion} ");
            }
            Console.WriteLine();
            Console.WriteLine($"Number of clusters {_clusters.Count}");
            foreach (var (cluster, list) in _clusters)
            {
                Console.WriteLine($"Cluster {cluster} Count: {list.Count}");
            }
        }

        private void GetChampions(int championCount)
        {
            var random = new Random();
            var max = _vectors.Count;
            while (_champions.Count != championCount)
            {
                var id = random.Next(max);
                if (!_champions.ContainsKey(id))
                {
                    _vectors.Remove(id, out var value);
                    _champions[id] = value;
                }
            }
        }

        private void MakeClusters()
        {
            foreach (var (doc, vector) in _vectors)
            {
                var similarities = _champions.Select(kvp => new Similarity
                {
                    Champion = kvp.Key,
                    Level = CosinineSimilarity(vector, kvp.Value)
                }).ToList();
                var best = similarities.Max();
                _clusters.AddOrUpdate(best.Champion, _ => new List<int> {doc}, (_, list) =>
                {
                    list.Add(doc);
                    return list;
                });
            }
        }

        public static float CosinineSimilarity(float[] first, float[] second)
        {
            return DotProduct(first, second) / (Norm(first) * Norm(second));
        }

        private static float DotProduct(float[] first, float[] second)
        {
            int vectorSize = Vector<float>.Count;
            Vector<float> acc = Vector<float>.Zero;
            int i;
            for (i = 0; i < first.Length - vectorSize; i += vectorSize)
            {
                var f = new Vector<float>(first, i);
                var s = new Vector<float>(second, i);
                acc += f * s;
            }

            float result = Vector.Dot(acc, Vector<float>.One);
            for (; i < first.Length; i++)
            {
                result += first[i] * second[i];
            }

            return result;
        }

        private static float Norm(float[] vector) => (float) Math.Sqrt(DotProduct(vector, vector));
        
        private struct Similarity : IComparable<Similarity>
        {
            public int Champion { get; set; }

            public float Level { get; set; }
            
            public int CompareTo(Similarity other) => Level.CompareTo(other.Level);
        }
    }
}