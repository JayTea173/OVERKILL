using System.Collections.Generic;
using System.Linq;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class WeaponVariantDamageUpgrade : WeaponUpgrade, IRandomizable
{

    public override int MaxLevel => 5;

    public override double AppearChanceWeighting => 2.2d * AppearChanceWeightingOptionMultiplier;

    public WeaponVariationType type;
    public DoubleRarityValue multiplier;
    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == type;
    }

    public override string Name => $"Damage Up: {type.ToString()}";

    public override string Description =>
        $"Increase damage dealt by your {type.ToString()} by {(multiplier.ValuesByRarity[Rarity] - 1d)*level:0.%}";
    

    public override Rarity MaxRarity => Rarity.Overkill;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[type] += (multiplier[Rarity] - 1d) * level;
        //OK.Log($"Multiplier of {type} is now {PlayerUpgradeStats.weaponDamageMultplier.weaponVariationTypeMultiplier[type]}");
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[type] -= (multiplier[Rarity] - 1d) * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        
        
        var allWeaponTypesEquipped = GunControl.Instance.allWeapons.Select(w => w.GetComponent <WeaponTypeComponent>()).
                                                Where(wt => wt != null).Select(wt => wt.value);

        var allWeaponsTypesEquippedHashset = new HashSet <WeaponVariationType>(allWeaponTypesEquipped);

        do
        {
            type = (WeaponVariationType)(Random.Range(0, (int)WeaponVariationType.FirestarterRocketLauncher) + 1);
        }
        while (!allWeaponsTypesEquippedHashset.Contains(type));
        
        multiplier = new DoubleRarityValue(1.02d);
        multiplier[Rarity.Uncommon] = 1.04d;
        multiplier[Rarity.Rare] = 1.08d;
        multiplier[Rarity.Epic] = 1.12d;
        multiplier[Rarity.Overkill] = 1.2d;

        var r = Random.value;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
    
    protected bool Equals(WeaponVariantDamageUpgrade other)
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

        return Equals((WeaponVariantDamageUpgrade)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ (int)type;
        }
    }
}
