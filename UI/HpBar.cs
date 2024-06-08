using System.Reflection;
using HarmonyLib;
using OVERKILL.Patches;
using OVERKILL.Upgrades;

namespace OVERKILL.UI;

[HarmonyPatch(typeof(HealthBar), "Update")]
public class PatchHealthBar 
{
    private static FieldInfo hpRefl = typeof(HealthBar).GetField("hp", BindingFlags.Instance | BindingFlags.NonPublic);
    static void Postfix(HealthBar __instance)
    {
        //OK.Log($"HPBar is: {__instance.gameObject.GetGameObjectScenePath()}");
        //OK.Log($"HP bar layer: {__instance.gameObject.layer}, root: {__instance.transform.root.gameObject.name}");
        var hpValue = (float)hpRefl.GetValue(__instance);
        __instance.hpText.richText = true;

        var max = PatchMaxHP.currMax + PlayerUpgradeStats.Instance.HPBonusFlat;
        __instance.hpText.text = hpValue.ToString("0.") + "<size=50%>/" + max.ToString("0.") + "</size>";

        for (var sliderId = 0; sliderId < __instance.hpSliders.Length; sliderId++)
        {
            var slider = __instance.hpSliders[sliderId];
            slider.minValue = (float)(max * sliderId);
            slider.maxValue = (float)(slider.minValue + max);
        }
    }  
}