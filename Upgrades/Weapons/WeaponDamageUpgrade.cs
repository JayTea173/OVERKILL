using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class WeaponDamageUpgrade : WeaponUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => 1d;
    public override int MaxLevel => 10;

    public WeaponType type;
    [JsonIgnore]
    public DoubleRarityValue multiplier => new DoubleRarityValue(1.02d, 1.04d, 1.06d, 1.08d, 1.08d);
    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.WeaponTypeNoVariation == type;
    }

    public override string Name => $"Damage Up: {type.ToString()}";

    public override string Description =>
        $"Increase damage dealt by your {type.ToString()} by {(multiplier.ValuesByRarity[Rarity] - 1d) * level:0.%}";
    

    public override Rarity MaxRarity => Rarity.Epic;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponTypeMultiplier[type] += (multiplier[Rarity] - 1d) * level;
        //OK.Log($"Multiplier of {type} is now {PlayerUpgradeStats.weaponDamageMultplier.weaponTypeMultiplier[type]}");
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponTypeMultiplier[type] -= (multiplier[Rarity] - 1d) * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
       

        var allWeaponTypesEquipped = GunControl.Instance.allWeapons.Select(w => w.GetComponent <WeaponTypeComponent>()).
                                                Where(wt => wt != null).Select(wt => wt.WeaponTypeNoVariation);

        var allWeaponsTypesEquippedHashset = new HashSet <WeaponType>(allWeaponTypesEquipped);

        do
        {
            type = (WeaponType)(Random.Range(0, (int)WeaponType.RocketLauncher) + 1);
        }
        while (!allWeaponsTypesEquippedHashset.Contains(type));
        
        var r = Random.value;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Epic
                 };
    }
    
    protected bool Equals(WeaponDamageUpgrade other)
    {
        return base.Equals(other) && type == other.type;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != this.GetType())
            return false;

        return Equals((WeaponDamageUpgrade)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ (int)type;
        }
    }
}
