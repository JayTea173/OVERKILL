using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GameConsole;
using HarmonyLib;
using OVERKILL;
using OVERKILL.Patches;
using OVERKILL.UI.Upgrades;
using OVERKILL.Upgrades;
using OVERKILL.Upgrades.Cybergrind;
using UnityEngine;
using Console = System.Console;

namespace OVERKILL
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class OK : BaseUnityPlugin
    {
        private static OK instance;
        public static void Log(object data, LogLevel level = LogLevel.Info)
        {
            instance.Logger.Log(level, data);
        }
        
        public static void LogTraced(object data, LogLevel level = LogLevel.Info)
        {
            instance.Logger.Log(level, data.ToString() + "\n" + new StackTrace().ToString());
        }
        private void Awake()
        {
            instance = this;
            
            // Plugin startup logic
            Logger.LogInfo($"LOADED UP MY COOL PLUGIN Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony("com.jaydev.overkill");
            harmony.PatchAll();
            
            DamageNumbers.Initialize();
            PlayerUpgradeStats.Initialize();
            RandomUpgrade.Initialize();
            PatchCybergrindEnemySpawning.Initialize();
            PlayerDeathHandler.Instance.enabled = true;
            
            //make it a custom game to not put modded game on leaderboards
            if (StatsManager.Instance != null)
                StatsManager.Instance.majorUsed = true;

        }


    }
}
