using System.Collections.Generic;
using System.Linq;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class HeadshotDamageUpgrade : WeaponUpgrade, IRandomizable
{
    public override int MaxLevel => 5;

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return true;
    }

    public override string Name => "BOOM HEADSHOT!";

    public override string Description => $"Increase your {type} headshot damage by {multiplier[Rarity]*level:0.%}.";
    
    public override Rarity MaxRarity => Rarity.Overkill;
    public DoubleRarityValue multiplier;
    public WeaponType type;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.weaponHeadshotDamageMultiplier.weaponTypeMultiplier[type] += multiplier[Rarity]*level;
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.weaponHeadshotDamageMultiplier.weaponTypeMultiplier[type] -= multiplier[Rarity]*level;
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
        
        multiplier = new DoubleRarityValue(0.0);
        multiplier[Rarity.Uncommon] = 0.0;
        multiplier[Rarity.Rare] = 0.08;
        multiplier[Rarity.Epic] = 0.16;
        multiplier[Rarity.Overkill] = 0.4;

        var r = Random.value * RarityChances.Rare;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill / 2f => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
    
    protected bool Equals(HeadshotDamageUpgrade other)
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

        return Equals((HeadshotDamageUpgrade)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ (int)type;
        }
    }
}
