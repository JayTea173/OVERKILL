using System;
using System.Collections.Generic;
using System.Reflection;
using GameConsole.pcon;
using HarmonyLib;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class ProjectilesCanHitCoinsUpgrade : IUpgrade
{
    public int OptionsSortPriority => 0;
    public bool IsObtainable => !PlayerUpgradeStats.Instance.upgrades.ContainsKey(this.GetHashCode());

    public double AppearChanceWeighting => RarityChances.Overkill * 1.2f * AppearChanceWeightingOptionMultiplier;

    public double AppearChanceWeightingOptionMultiplier {get; set;} = 1d;

    public string Name => "OVERRICOCHET";

    public string Description => "Allows all projectiles to hit your coins, including the hostile ones.";

    public Rarity Rarity
    {
        get => Rarity.Overkill;
        set
        {
        }
    }

    public Rarity MaxRarity => Rarity.Overkill;

    public void Apply()
    {
        ProjectileStartPatcher.hasEffect = true;
    }

    public void Absolve()
    {
        ProjectileStartPatcher.hasEffect = false;
    }

    public bool Equals(IUpgrade other)
    {
        return other != null && this.Name.Equals(other.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public class CapturedInCustomMagnet : MonoBehaviour
{
    public CoinMagnet magnet;

    private void OnDestroy()
    {
        if (magnet != null)
        {
            magnet.RemoveCoin(GetComponent <Rigidbody>());
        }
    }
}
public class CoinMagnet : MonoBehaviour
{
    public class CoinWithRigidbody : IEquatable <CoinWithRigidbody>
    {
        public Coin coin;
        public Rigidbody rb;

        public CoinWithRigidbody()
        {
        }

        public CoinWithRigidbody(Rigidbody rb)
        {
            this.rb = rb;
        }

        public bool Equals(CoinWithRigidbody other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Equals(rb, other.rb);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != this.GetType())
                return false;

            return Equals((CoinWithRigidbody)obj);
        }

        public override int GetHashCode()
        {
            return (rb != null ? rb.GetHashCode() : 0);
        }
    }
    
    public Magnet magnet;
    public float magnetRadius;

    private HashSet <CoinWithRigidbody> coins = new HashSet <CoinWithRigidbody>();
    private HashSet <Rigidbody> rigidbodies = new HashSet <Rigidbody>();

    public bool RemoveCoin(Rigidbody rb)
    {
        coins.Remove(new CoinWithRigidbody(rb));
        
        return Remove(rb);
    }
    
    public bool Remove(Rigidbody rb)
    {
        return rigidbodies.Remove(rb);
    }

    void Capture(Coin coin)
    {
        //var affectedRbsField = typeof(Magnet).GetField("affectedRbs", BindingFlags.Instance | BindingFlags.NonPublic);
        //var affectedRbs = (List <Rigidbody>)affectedRbsField.GetValue(magnet);

        var mag = coin.GetOrAddComponent <CapturedInCustomMagnet>();
        mag.magnet = this;
        var rb = coin.GetComponent <Rigidbody>();
        coins.Add(new CoinWithRigidbody()
        {
            coin = coin,
            rb = rb
        });

        rigidbodies.Add(rb);
    }
    
    void Capture(Rigidbody rb)
    {
        //var affectedRbsField = typeof(Magnet).GetField("affectedRbs", BindingFlags.Instance | BindingFlags.NonPublic);
        //var affectedRbs = (List <Rigidbody>)affectedRbsField.GetValue(magnet);

        var mag = rb.GetOrAddComponent <CapturedInCustomMagnet>();
        mag.magnet = this;
        rigidbodies.Add(rb);
    }

    void FixedUpdate()
    {
        List <Rigidbody> deadCoins = new List <Rigidbody>();
        foreach (var coinWithRb in coins)
        {
            if (coinWithRb.rb != null && coinWithRb.rb.GetComponent <AudioSource>() == null)
            {
                if (rigidbodies.Remove(coinWithRb.rb))
                {
                    deadCoins.Add(coinWithRb.rb);
                }
            }
        }

        foreach (var deadcoinrb in deadCoins)
        {
            RemoveCoin(deadcoinrb);
        }
        
        foreach (var rb in rigidbodies)
        {
            if (rb == null)
                continue;

            var deltaPos = (transform.position + Vector3.up * 5f) - rb.transform.position;
            var dist = deltaPos.magnitude;
            var deltaPosNorm = deltaPos / dist;
            rb.AddForce(deltaPos * ((magnetRadius - dist) / magnetRadius * Time.fixedDeltaTime * 2500f * rb.mass));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.layer == 10 || other.gameObject.layer == 14) && other.attachedRigidbody != null)
        {
            if (other.gameObject.CompareTag("Coin"))
            {
                var coin = other.attachedRigidbody.GetComponent <Coin>();

                if (coin != null)
                    Capture(coin);
            }
            else
            {
                if (other.attachedRigidbody.TryGetComponent(out Projectile proj))
                {
                    proj.friendly = true;
                }
                Capture(other.attachedRigidbody);
            }
        }
    }
}

[HarmonyPatch(typeof(Magnet), "Start")]
public class PatchMagnetSetup
{
    public static bool hasEffect = false;
    public static void Postfix(Magnet __instance)
    {
        if (!hasEffect)
            return;
        
        var coinMagnet = __instance.GetOrAddComponent <CoinMagnet>();
        coinMagnet.magnet = __instance;
        var sphereColliderField = typeof(Magnet).GetField("col", BindingFlags.Instance | BindingFlags.NonPublic);
        coinMagnet.magnetRadius = ((SphereCollider)sphereColliderField.GetValue(__instance)).radius;
    }
}


[HarmonyPatch(typeof(Coin), "OnCollisionEnter")]
public class PatchMagnetCoinPreventDeath
{
    public static bool Prefix(Coin __instance, Collision collision)
    {
        if (collision.gameObject.layer != 8 && collision.gameObject.layer != 24)
            return true;


        if (__instance.GetComponent <CapturedInCustomMagnet>() == null)
            return true;
        
        return false;

    }
}

