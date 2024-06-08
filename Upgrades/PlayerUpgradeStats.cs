using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Logging;
using GameConsole.pcon;
using Newtonsoft.Json;
using OVERKILL.HakitaPls;
using OVERKILL.JSON;
using OVERKILL.Patches;
using OVERKILL.UI;
using OVERKILL.UI.Options;
using OVERKILL.UI.Upgrades;
using OVERKILL.Upgrades.Cybergrind;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class PlayerUpgradeStats
{
    public static PlayerUpgradeStats Instance {get; private set;} = new PlayerUpgradeStats();
    
    public struct WeaponMultiplier
    {
        public double globalMultiplier = 1d;
        [JsonConverter(typeof(EnumIndexedArrayConverter))]
        public EnumIndexedArray <double, WeaponType> weaponTypeMultiplier = 1d;
        [JsonConverter(typeof(EnumIndexedArrayConverter))]
        public EnumIndexedArray <double, WeaponVariationType> weaponVariationTypeMultiplier = 1d;

        public double GetMultiplier(WeaponTypeComponent wt)
        {
            if (wt == null)
                return globalMultiplier;
            
            return globalMultiplier *
                   weaponVariationTypeMultiplier[wt.value] *
                   weaponTypeMultiplier[wt.WeaponTypeNoVariation];
        }
    }
    
    public WeaponMultiplier weaponDamageMultplier = new WeaponMultiplier();
    public WeaponMultiplier weaponHeadshotDamageMultiplier = new WeaponMultiplier();
    public long HPBonusFlat = 0;
    public double PctMaxHPGainOnKill = 0d;
    public double TotalHPGainedOnKill = 0d;
    public float chargeSpeed = 1f;
    
    [JsonProperty]
    private double stylePointsMultiplier = 1d;

    private bool upgradesAreApplied = false;

    public double StylePointsMultiplier
    {
        get => stylePointsMultiplier;
        set
        {
            stylePointsMultiplier = value;
            if (XPMeter.Instance != null)
                XPMeter.Instance.bonusText.text = $"+{stylePointsMultiplier - 1:0.%}";
        }
    }
    public long stylePoints;
    public int okLevel = 1;

    public Dictionary <int, IUpgrade> upgrades = new Dictionary <int, IUpgrade>();

    public static void Initialize()
    {
        Events.OnDealDamage.Pre += Instance.OnDeliverDamage;
        Events.OnDeath.Post += Instance.OnEnemyDeath;

        TryLoadFromFile(Path.Combine(Application.persistentDataPath, "OVERKILL-save.json"));
    }

    private void OnDeliverDamage(EventDelegates.DeliverDamageEventData evntData, EnemyIdentifier enemy, GameObject target, Vector3 force, Vector3 hitpoint, ref float multiplier, bool tryforexplode, float critmultiplier, GameObject sourceweapon, bool ignoretotaldamagetakenmultiplier, bool fromexplosion)
    {
        if (sourceweapon == null)
            return;

        var wt = sourceweapon.GetComponent <WeaponTypeComponent>();
        var mul = weaponDamageMultplier.GetMultiplier(wt);

        if (evntData.isHeadshot)
        {
            mul *= weaponHeadshotDamageMultiplier.GetMultiplier(wt);
        }
        
        multiplier = (float)(multiplier * mul);
    }
    
    private void OnEnemyDeath(EnemyIdentifier enemy, bool fromexplosion)
    {
        if (EnemyMaxHP.TryGet(enemy, out var maxHealth))
        {
            var gain = maxHealth * PlayerUpgradeStats.Instance.PctMaxHPGainOnKill;

            if (gain > 0d)
            {
                if (TotalHPGainedOnKill > 50d)
                {
                    gain *= Math.Max(0.01d, Math.Pow(50d / TotalHPGainedOnKill, 2.5d));
                }

                TotalHPGainedOnKill += gain;
                PatchMaxHP.currMax += gain;
            }

            EnemyMaxHP.Unregister(enemy);
        }

        //UpgradeScreen.Instance.enabled = true;
    }

    public void Reset()
    {
        if (upgradesAreApplied)
        {
            foreach (var a in upgrades)
                a.Value.Absolve();

            upgradesAreApplied = false;
        }

        if (!Options.config.KeepUpgrades)
        {
            upgrades.Clear();

            weaponDamageMultplier = new WeaponMultiplier();
            weaponHeadshotDamageMultiplier = new WeaponMultiplier();
            HPBonusFlat = 0;
            PctMaxHPGainOnKill = 0d;
            TotalHPGainedOnKill = 0;
            chargeSpeed = 1f;
            StylePointsMultiplier = 1d;
            stylePoints = 0;

            //OK.Log($"Level at {stylePoints} xp: {okLevel}, other way round: {StyleLevelupThresholds.GetXPAtLevel(okLevel)}, before that: {StyleLevelupThresholds.GetXPAtLevel(okLevel-1)}");
            PatchCybergrindEnemySpawning.customSpawns.customSpawns.Clear();
            PatchCybergrindEnemySpawning.waveThresholdSpawnBonus = 0;
            UpgradeScreen.advantage = 0;
            UpgradeScreen.numExtraChoices = 0;
            TotalHPGainedOnKill = 0;
            HPBonusFlat = 0;
            PatchMaxHP.currMax = 100;


            if (XPMeter.Instance != null)
                XPMeter.Instance.bonusText.text = string.Empty;

        }
        else
        {
            SaveToFile(Path.Combine(Application.persistentDataPath, "OVERKILL-save.json"));
            
            foreach (var a in upgrades)
                a.Value.Apply();

            upgradesAreApplied = true;

        }
        
        okLevel = StyleLevelupThresholds.GetLevelAtXP(stylePoints);

    }

    public void SaveToFile(string file)
    {
        OK.Log($"Writing save to {file}");
        JsonSerializerSettings settings = new JsonSerializerSettings();
        
        settings.TypeNameHandling = TypeNameHandling.Objects;
        settings.Formatting = Formatting.Indented;
        settings.Culture = CultureInfo.InvariantCulture;
        
        File.WriteAllText(file, JsonConvert.SerializeObject(this, Formatting.Indented, settings));
    }
    
    public static bool TryLoadFromFile(string file)
    {
        OK.Log($"LOADING from {file}");
        
        if (!File.Exists(file))
            return false;

        try
        {
            var json = File.ReadAllText(file);
            
            JsonSerializerSettings settings = new JsonSerializerSettings();
        
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Formatting = Formatting.Indented;
            settings.Culture = CultureInfo.InvariantCulture;

            Instance = JsonConvert.DeserializeObject <PlayerUpgradeStats>(json, settings);
        }
        catch (Exception ex)
        {
            OK.Log(ex.ToString(), LogLevel.Error);

            return false;
        }

        return true;
    }
}
