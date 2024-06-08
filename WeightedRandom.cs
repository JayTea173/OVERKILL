using System;
using System.Collections.Generic;
using System.Linq;

namespace OVERKILL;

public interface IRandomWeight
{
    public double Weight {get;}
}

public struct FixedRandomWeight : IRandomWeight
{
    public double Weight {get;}

    public FixedRandomWeight(double weight = 1d)
    {
        Weight = weight;
    }
}

public class WeightedRandom<T>
{
    private List <WeightedRandom <T>.Entry> _entries;
    public double weightSum;
    public Random rnd;
    
    public struct Entry
    {
        public T value;
        public IRandomWeight weight;
    }

    public WeightedRandom(Random rnd)
    {
        this.rnd = rnd;
        _entries = new List <Entry>();
    }

    public void Clear()
    {
        weightSum = 0;
        _entries.Clear();
    }

    public static int numFuckedSounds = 0;
    
    public void AddEntry(T value, IRandomWeight weight)
    {
        _entries.Add(new Entry() {value = value, weight = weight});
        weightSum += weight.Weight;
    }

    public bool Any() => _entries.Any();

    public T Get()
    {
       
        double numericValue = rnd.NextDouble() * weightSum;

        foreach (var entry in _entries)
        {
            numericValue -= entry.weight.Weight;

            if (numericValue <= 0)
                return entry.value;
        }

        throw new Exception($"WTF, got rnd {numericValue} left, {_entries.Count} entries");
    }
}
