using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades.Attractor;

public class AttractCoinUpgrade : WeaponUpgrade, IRandomizable
{
    //public override double AppearChanceWeighting => 0.002f;
    public override double AppearChanceWeighting => RarityChances.Epic;
    
    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return false;
    }

    public override string Name => "OVERREAL";

    public override string Description =>
        $"Your attractor rods now influences coins. And all the other things, too.";

    public override Rarity Rarity => Rarity.Epic;
    public override Rarity MaxRarity => Rarity.Epic;

    public override void Apply()
    {

        PatchMagnetSetup.hasEffect = true;
    }

    public override void Absolve()
    {
        PatchMagnetSetup.hasEffect = false;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
    }
}