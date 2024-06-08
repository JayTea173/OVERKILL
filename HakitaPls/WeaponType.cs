using HarmonyLib;
using UnityEngine;

namespace OVERKILL.HakitaPls;

/// <summary>
/// since NewBlood's coders don't seem to use any sort of Inheritance or actually useful interfaces when it comes to most gameplay stuff
/// to make stuff more easily generalizable for modding (or their own future development),
/// I make some ugly bridge here.
/// </summary>

public enum WeaponType
{
    UNKNOWN,
    Revolver,
    SlabRevolver,
    Shotgun,
    ShotgunHammer,
    Nailgun,
    Sawgun,
    Railcannon,
    RocketLauncher,
}

public enum WeaponVariationType
{
    UNKNOWN,
    
    PiercerRevolver,
    MarskmanRevolver,
    SharpshooterRevolver,
    PiercerSlabRevolver,
    MarskmanSlabRevolver,
    SharpshooterSlabRevolver,
    
    CoreEjectShotgun,
    PumpChargeShotgun,
    SawedOnShotgun,
    CoreEjectShotgunHammer,
    PumpChargeShotgunHammer,
    SawedOnShotgunHammer,

    AttractorNailgun,
    OverheatNailgun,
    JumpstartNailgun,
    AttractorSawgun,
    OverheatSawgun,
    JumpstartSawgun,
    
    ElectricRailcannon,
    ScrewdriverRailcannon,
    MaliciousRailcannon,
    
    FreezeframeRocketLauncher,
    SRSRocketLauncher,
    FirestarterRocketLauncher
    
}

[HarmonyPatch(typeof(WeaponIdentifier), "Start")]
public class PatchWeaponIdentifierAttachWeaponType
{
    static void Postfix(WeaponIdentifier __instance)
    {
        if (__instance.gameObject.GetComponent <WeaponTypeComponent>() != null)
            return;

        __instance.gameObject.AddComponent <WeaponTypeComponent>().value = GetWeaponType(__instance);
    }

    private static WeaponVariationType GetWeaponType(WeaponIdentifier wid)
    {
        if (wid.TryGetComponent(out Revolver revolver))
            return WeaponVariationType.PiercerRevolver + revolver.gunVariation + (revolver.altVersion ? 3 : 0);
        if (wid.TryGetComponent(out Shotgun shotgun))
            return WeaponVariationType.CoreEjectShotgun + shotgun.variation;
        if (wid.TryGetComponent(out ShotgunHammer shotgunHammer))
            return WeaponVariationType.CoreEjectShotgunHammer + shotgunHammer.variation;
        if (wid.TryGetComponent(out Nailgun nailgun))
            return WeaponVariationType.AttractorNailgun + nailgun.variation + (nailgun.altVersion ? 3 : 0);
        if (wid.TryGetComponent(out Railcannon railcannon))
            return WeaponVariationType.ElectricRailcannon + railcannon.variation;
        if (wid.TryGetComponent(out RocketLauncher rocketLauncher))
            return WeaponVariationType.FreezeframeRocketLauncher + rocketLauncher.variation;
        
        return WeaponVariationType.UNKNOWN;
    }
}

public class WeaponTypeComponent : MonoBehaviour
{
    public WeaponVariationType value;

    public WeaponType WeaponTypeNoVariation
    {
        get
        {
            return value switch
                   {
                       <= WeaponVariationType.SharpshooterRevolver => WeaponType.Revolver,
                       <= WeaponVariationType.SharpshooterSlabRevolver => WeaponType.SlabRevolver,
                       <= WeaponVariationType.SawedOnShotgun => WeaponType.Shotgun,
                       <= WeaponVariationType.SawedOnShotgunHammer => WeaponType.ShotgunHammer,
                       <= WeaponVariationType.JumpstartNailgun => WeaponType.Nailgun,
                       <= WeaponVariationType.JumpstartSawgun => WeaponType.Sawgun,
                       <= WeaponVariationType.MaliciousRailcannon => WeaponType.Railcannon,
                       <= WeaponVariationType.FirestarterRocketLauncher => WeaponType.RocketLauncher,
                       _ => WeaponType.UNKNOWN
                   };
        }
    }
}
