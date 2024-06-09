using System;
using HarmonyLib;
using UnityEngine;

namespace OVERKILL;

using static EventDelegates;

public struct Event<TDelegate>
{
    public TDelegate Pre;
    public TDelegate LatePre;
    public TDelegate Post;
}

public static class Events
{
    public static Event<DeliverDamage> OnDealDamage;
    public static Event<Death> OnEnemyDeath;
    public static Event<WithNone> OnPlayerRespawn;
    public static Event <WithSender <global::RocketLauncher>> OnFreezeStart, OnFreezeEnd;
}


public sealed class EventDelegates
{
    public class DeliverDamageEventData
    {
        public bool isHeadshot = false;
    }
    
    public delegate void WithSender<T>(T sender);

    public delegate void WithNone();

    public delegate void DeliverDamage(
        DeliverDamageEventData evntData,
        EnemyIdentifier enemy,
        GameObject target,
        Vector3 force,
        Vector3 hitPoint,
        ref float multiplier,
        bool tryForExplode,
        float critMultiplier = 0.0f,
        GameObject sourceWeapon = null,
        bool ignoreTotalDamageTakenMultiplier = false,
        bool fromExplosion = false);

    public delegate void Death(EnemyIdentifier enemy, bool fromExplosion);
}
