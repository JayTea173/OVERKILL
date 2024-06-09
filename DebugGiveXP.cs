using HarmonyLib;
using UnityEngine;

namespace OVERKILL;

[HarmonyPatch(typeof(NewMovement), "Update")]
public class DebugGiveXP
{
    public static void Postfix()
    {
        if (Time.frameCount % 5 != 0)
            return;
        
        if (Input.GetKey(KeyCode.O) && Input.GetKey(KeyCode.V) && Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.R))
            StyleHUD.Instance.AddPoints(100, "O.V.E.R");
    }
}
