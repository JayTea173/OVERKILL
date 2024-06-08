using System;
using System.Globalization;
using Newtonsoft.Json;
using OVERKILL.JSON;

namespace OVERKILL.Upgrades;

public sealed class DoubleRarityValue : RarityValue <double>
{
    public DoubleRarityValue(double baseValue) : base(baseValue)
    {
    }

    public DoubleRarityValue(params double[] values) : base(values)
    {
        
    }
    
    public DoubleRarityValue() : base(){}

    public override double Multiply(double v1, double v2) => v1 * v2;
    public override double Add(double v1, double v2) => v1 + v2;
}

public sealed class LongRarityValue : RarityValue <long>
{
    public LongRarityValue(long baseValue) : base(baseValue)
    {
    }
    
    public LongRarityValue(long[] values) : base(values)
    {
    }
    
    public LongRarityValue() : base(){}

    public override long Multiply(long v1, double v2) => (long)(v1 * v2);
    public override long Add(long v1, double v2) => (long)(v1 + v2);
}

public abstract class RarityValue<T> where T : IConvertible
{
    [JsonConverter(typeof(EnumIndexedArrayConverter))]
    public EnumIndexedArray <T, Rarity> ValuesByRarity {get; private set;}

    public RarityValue(T baseValue)
    {
        ValuesByRarity = baseValue;
    }
    
    public RarityValue(T[] values)
    {
        ValuesByRarity = new EnumIndexedArray <T, Rarity>(values);
    }

    public RarityValue()
    {
        ValuesByRarity = new EnumIndexedArray <T, Rarity>();
    }


    public abstract T Multiply(T v1, double v2);
    public abstract T Add(T v1, double v2);

    public void ApplyLinearRelativeScaling(double scalingValue)
    {
        for (int i = 1; i < ValuesByRarity.Length; i++)
            ValuesByRarity[i] = Multiply(ValuesByRarity[0], i * scalingValue);
    }
    
    public void ApplyExpRelativeScaling(double scalingValue)
    {
        for (int i = 1; i < ValuesByRarity.Length; i++)
            ValuesByRarity[i] = Multiply(ValuesByRarity[i-1], scalingValue);
    }
    
    public void ApplyLinearAbsoluteScaling(double scalingValue)
    {
        for (int i = 1; i < ValuesByRarity.Length; i++)
            ValuesByRarity[i] = Add(ValuesByRarity[0], i * scalingValue);
    }

    public T this[Rarity r]
    {
        get => ValuesByRarity[r];
        set => ValuesByRarity[r] = value;
    }
}
