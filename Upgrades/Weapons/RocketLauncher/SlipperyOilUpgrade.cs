using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades.RocketLauncher;

public class SlipperyOilUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Uncommon * 0.8f * AppearChanceWeightingOptionMultiplier;

    public override int MaxLevel => 5;

    public override string Name => "SLIPPERY GAS";

    public override string Description => $"Increases the amount of gas your firestarter holds by {-1d + (1d / (1d - (slipChancePerMeter[Rarity]*level))):0.%}. Zombie-type enemies randomly slip while moving through gasoline. Higher level increases chance of slippage :)";
    
    public override Rarity MaxRarity => Rarity.Overkill;
    
    public DoubleRarityValue slipChancePerMeter;

    public override void Apply()
    {
        SlipperyAgentComponent.slipChance += (float)(slipChancePerMeter[Rarity] * level);
        PatchNapalmUsage.multiplier -= slipChancePerMeter[Rarity] * level;
    }

    public override void Absolve()
    {
        SlipperyAgentComponent.slipChance -= (float)(slipChancePerMeter[Rarity] * level);
        PatchNapalmUsage.multiplier += slipChancePerMeter[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        slipChancePerMeter = new DoubleRarityValue(0);
        slipChancePerMeter[Rarity.Uncommon] = 0.03;
        slipChancePerMeter[Rarity.Rare] = 0.05;
        slipChancePerMeter[Rarity.Epic] = 0.07;
        slipChancePerMeter[Rarity.Overkill] = 0.10;

        var r = Random.value * RarityChances.Uncommon;

        Rarity = r switch
                 {
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}

public class SlipperyAgentComponent : MonoBehaviour
{
    public EnemyIdentifier eid;
    public NavMeshAgent nma;
    private Vector3Int? lastCheckedGasolineVoxel;
    public bool standingInGas;

    public delegate void SlipDelegate(float speed);

    public SlipDelegate onSlip;

    public static float slipChance = 0f;

    private void Awake()
    {
        eid = GetComponent <EnemyIdentifier>();
        nma = GetComponent <NavMeshAgent>();
    }

    private void FixedUpdate()
    {
        if (slipChance <= 0f || eid.dead || nma == null)
            return;
        
        //Vector3Int voxelPosition = StainVoxelManager.WorldToVoxelPosition(this.transform.position + Vector3.down * 1.833333f);
        Vector3Int voxelPosition = StainVoxelManager.WorldToVoxelPosition(this.transform.position + Vector3.down * 1.8f);

        if (!this.lastCheckedGasolineVoxel.HasValue || this.lastCheckedGasolineVoxel.Value != voxelPosition)
        {
            this.lastCheckedGasolineVoxel = new Vector3Int?(voxelPosition);

            standingInGas = MonoSingleton <StainVoxelManager>.Instance.HasProxiesAt(
                voxelPosition,
                shape: VoxelCheckingShape.VerticalBox,
                searchMode: ProxySearchMode.AnyFloor);
        }
        
        var vel = nma.velocity;
        vel.y = 0;
        
        if (vel.sqrMagnitude > 0f && standingInGas)
        {
            var r = Random.value;
            float slipChancePerMeter = 0.6f;

            var v = vel.magnitude;
            if (r < Time.fixedDeltaTime * v * slipChancePerMeter / eid.health * 0.5f) 
            {
                //OK.Log($"SLIP!! {gameObject.name} speed: {nma.velocity}, in gas: {standingInGas}");
                onSlip?.Invoke(v);

                StyleHUD.Instance.AddPoints((int)(10f + eid.health*10f*2f), "gasoline.slip", null, eid);
                eid.DeliverDamage(eid.gameObject, transform.forward * 800f * v, transform.position, eid.health, false);
                PlayDeathSound(eid.gameObject.transform.position);
            }
        }
    }

    private async void PlayDeathSound(Vector3 p)
    {
        var clip = await CustomSound.LoadAsync("ack.wav");
        clip.PlayClipAtPoint(null, p, 128, 1f, 3f, Random.Range(0.9f, 1.1f));
    }
}


[HarmonyPatch(typeof(global::RocketLauncher), nameof(global::RocketLauncher.ShootNapalm))]
public class PatchNapalmUsage
{
    public static double multiplier = 1d;
    public static void Postfix(global::RocketLauncher __instance)
    {
        MonoSingleton<WeaponCharges>.Instance.rocketNapalmFuel += (float)(0.015d * (1d - multiplier));

    }
}

[HarmonyPatch(typeof(Zombie), "Awake")]
public class PatchNapalmSlipZombie
{
    public static void Postfix(Zombie __instance)
    {
        var slippery = __instance.GetOrAddComponent <SlipperyAgentComponent>();
    }
}

[HarmonyPatch(typeof(SwordsMachine), "Awake")]
public class PatchNapalmSlipSwordMachine
{
    public static void Postfix(SwordsMachine __instance)
    {
        var slippery = __instance.GetOrAddComponent <SlipperyAgentComponent>();

    }
}


[HarmonyPatch(typeof(Ferryman), "Start")]
public class PatchNapalmSlipSwordFerryman
{
    public static void Postfix(Ferryman __instance)
    {
        var slippery = __instance.GetOrAddComponent <SlipperyAgentComponent>();

    }
}
