using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZoneSearch
{
    public class ZoneDictionary
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ZoneVector>> _dictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, ZoneVector>>();

        private readonly string[] _docs;

        public ZoneDictionary(float[] zones, string[] docs)
        {
            _docs = docs;
            ZoneVector.ZoneWeights = zones;
        }

        public void AddWord(string word, int doc, int zone)
        {
            _dictionary.AddOrUpdate(word, _ =>
            {
                var dict = new ConcurrentDictionary<int, ZoneVector>();
                dict[doc] = new ZoneVector(zone);
                return dict;
            }, (_, dict) =>
            {
                dict.AddOrUpdate(doc, _ => new ZoneVector(zone), (_, vec) =>
                {
                    vec[zone] = true;
                    return vec;
                });
                return dict;
            });
        }

        public IEnumerable<string> Query(string word)
        {
            if (_dictionary.TryGetValue(word, out var res))
            {
                return res.OrderByDescending(kvp => kvp.Value.Weight)
                    .Select(kvp => $"{_docs[kvp.Key]}  {kvp.Value.Weight}");
            }
            return Enumerable.Empty<string>();
        }
    }

    internal class ZoneVector : IComparable<ZoneVector>
    {
        internal static float[] ZoneWeights;

        public bool[] Zones { get; set; }

        private Lazy<float> _weight;

        public ZoneVector()
        {
            Zones = new bool[ZoneWeights.Length];
            _weight = new Lazy<float>(() => CalculateWeigth());
        }

        public ZoneVector(int zone)
        {
            Zones = new bool[ZoneWeights.Length];
            Zones[zone] = true;
            _weight = new Lazy<float>(() => CalculateWeigth());
        }

        public bool this[int index]
        {
            get => Zones[index];
            set => Zones[index] = value;
        }

        private float CalculateWeigth()
        {
            float weight = 0;
            for (int i = 0; i < Zones.Length; i++)
            {
                if (Zones[i])
                    weight += ZoneWeights[i];
            }

            return weight;
        }

        public float Weight => _weight.Value;

        public int CompareTo(ZoneVector other) => _weight.Value.CompareTo(other._weight.Value);
    }
}