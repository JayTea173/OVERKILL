using Newtonsoft.Json;
using OVERKILL.UI.Upgrades;

namespace OVERKILL.Upgrades;

public abstract class LeveledUpgrade : IUpgrade
{
    public int level = 0;
    [JsonIgnore]
    public virtual int MaxLevel => 1;
    [JsonIgnore]
    public virtual int OptionsSortPriority => 0;
    

    public virtual bool IsObtainable
    {
        get
        {
            if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(GetHashCode(), out var existing))
            {
                if (existing is LeveledUpgrade existingLvl)
                {
                    return existingLvl.level < existingLvl.MaxLevel;
                }

                return false;
            }

            return true;
        }
    }

    [JsonIgnore]
    public virtual double AppearChanceWeighting => 1d * AppearChanceWeightingOptionMultiplier;

    public double AppearChanceWeightingOptionMultiplier {get; set;} = 1d;
    
    public abstract string Name {get;}

    [JsonIgnore]
    public abstract string Description {get;}

    [JsonIgnore]
    public virtual Rarity Rarity {get; set;}

    [JsonIgnore]
    public virtual Rarity MaxRarity => Rarity.Overkill;

    public abstract void Apply();

    public abstract void Absolve();

    protected bool Equals(LeveledUpgrade other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    public bool Equals(IUpgrade other)
    {
        return other != null && GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != this.GetType())
            return false;

        return Equals((LeveledUpgrade)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}
