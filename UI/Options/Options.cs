using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using OVERKILL.UI.Upgrades;
using OVERKILL.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OVERKILL.UI.Options;

public class OptionsValues
{
    public bool KeepUpgrades = false;
    public double XpRequiredMultiplier = 1d;
}

[HarmonyPatch(typeof (OptionsMenuToManager))]
public class Options
{
    private static Transform optionsMenu;
    private static Transform settings, settingsContent;
    private static GameObject sliderPrefab, sectionPrefab, togglePrefab, buttonPrefab;

    public static OptionsValues config = new OptionsValues();


    private static void DumpHierarchy(Transform t)
    {
        StringBuilder sb = new StringBuilder();
        
        DumpHierarchyRecursive(t, sb);
        OK.Log($"Hierarchy of {t.gameObject.name}\n{sb.ToString()}");
    }
    
    private static void DumpHierarchyRecursive(Transform t, StringBuilder sb, int depth = 0)
    {
        if (depth > 0)
            sb.Append(new string('\t', depth));
        
        sb.Append(t.gameObject.name);

        var texts = t.GetComponents <TMP_Text>();

        if (texts.Length > 0)
        {
            sb.Append($" {string.Join(", ", texts.Select(text => $"<TEXT:{text.text}>"))}");
        }

        var comps = t.GetComponents <Component>();
        if (comps.Length > 0)
            sb.Append($" [{string.Join(", ", comps.Select(comp => comp.GetType().Name))}]");

        sb.Append('\n');
        
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            DumpHierarchyRecursive(c, sb, depth + 1);
        }
        
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionsMenuToManager), nameof(OptionsMenuToManager.CloseOptions))]
    public static void OnCloseOptions()
    {
        var filePath = Path.Combine(Application.persistentDataPath, "OVERKILL.json");
        File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof (OptionsMenuToManager), "Start")]
    public static bool OptionsMenuToManager_Start_Prefix(OptionsMenuToManager __instance)
    {
        var filePath = Path.Combine(Application.persistentDataPath, "OVERKILL.json");

        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            config = JsonConvert.DeserializeObject <OptionsValues>(json);
        }

        optionsMenu = __instance.optionsMenu.transform;
        //OptionsMenuToManagerPatch.Prepare();


        //var okOptions = UnityEngine.Object.Instantiate(optionsMenu.Find("Audio Options"));


        __instance.StartCoroutine(GenerateUICoroutine(__instance));
        
        
        
        /*
        CreateSection(audioSettingsContent, "-- AYAYAYA --").SetSiblingIndex(0);
        CreateSection(audioSettingsContent, "-- OVERKILL --");
        */
        //OptionsMenuToManagerPatch._note = OptionsMenuToManagerPatch.CreateSection(OptionsMenuToManagerPatch._audioSettingsContent, OptionsMenuToManagerPatch.GetNote());
        //OptionsMenuToManagerPatch._note.GetComponent<TextMeshProUGUI>().fontSize = 16f;
        return true;
    }



    private static IEnumerator GenerateUICoroutine(OptionsMenuToManager optionsMenuToManager)
    {

        var layout = optionsMenu.GetComponentInChildren <VerticalLayoutGroup>();

        var buttons = layout.transform.GetComponentsInChildren <Button>();

        foreach (var button in buttons)
        {
            if (button.GetComponentInParent <VerticalLayoutGroup>() == layout)
            {
                button.onClick.AddListener(OnClickNotOverkillTab);
                button.onClick.DirtyPersistentCalls();
                button.onClick.RebuildPersistentCallsIfNeeded();
            }
        }

        var audioButton = layout.transform.Find("Audio");
        var okButtonGo = UnityEngine.Object.Instantiate(audioButton, layout.transform);
        
        OK.Log($"SET SIBLING TO {audioButton.GetSiblingIndex()}");
        yield return new WaitForEndOfFrame();
        okButtonGo.transform.SetSiblingIndex(audioButton.GetSiblingIndex());

        okButtonGo.gameObject.name = "OVERKILL";

        var okText = okButtonGo.GetComponentInChildren <TMP_Text>();
        okText.text = "OVERKILL";
        okText.color = RarityColor.Get(Rarity.Overkill);
        
        
        
        var okButton = okButtonGo.GetComponent <Button>();


        
        okButton.onClick.RemoveAllListeners();
        //PrintClickEvent(okButton.onClick);
        
        okButton.onClick.m_PersistentCalls.m_Calls[0].arguments.boolArgument = false;
        okButton.onClick.DirtyPersistentCalls();
        okButton.onClick.RebuildPersistentCallsIfNeeded();
        
        okButton.onClick.AddListener(OnClickOverkillTab);

        //DumpHierarchy(optionsMenu);

        var gameplayOptions = optionsMenu.Find("Gameplay Options");

        if (gameplayOptions == null)
        {
            OK.Log("NO AUDIO OPTIONS!");
        }
        else
        {
            sliderPrefab = optionsMenu.Find("Audio Options").Find("Image/Master Volume").gameObject;
            sectionPrefab = optionsMenu.Find("HUD Options").Find("Scroll Rect (1)").Find("Contents").Find("-- HUD Elements -- ").gameObject;
            togglePrefab = gameplayOptions.GetComponentInChildren <Toggle>().transform.parent.gameObject;
            buttonPrefab = togglePrefab.transform.parent.Find("Advanced Options").gameObject.GetComponentInChildren<Button>().gameObject;
            settings = UnityEngine.Object.Instantiate(gameplayOptions.gameObject, gameplayOptions.parent).transform;
            settingsContent = settings.GetComponentInChildren<LayoutGroup>().transform;
            settingsContent.parent.gameObject.name = "OVERKILL Options";
            
            //destroy all children >:)
            for (int i = 0; i < settingsContent.childCount; i++)
            {
                UnityEngine.Object.Destroy(settingsContent.GetChild(i).gameObject);
            }

            
            yield return new WaitForEndOfFrame();

            
            CreateSection(settingsContent, "General");

            var resetUpgradesToggle = CreateToggle(settingsContent, "Keep Upgrades on respawn/level change");
            resetUpgradesToggle.onValueChanged.AddListener(
                (value) =>
                {
                    config.KeepUpgrades = value;
                });
            resetUpgradesToggle.SetIsOnWithoutNotify(config.KeepUpgrades);
            var resetUpgradesButton = CreateButton(settingsContent, "Reset NAOW!");

            resetUpgradesButton.onClick.AddListener(
                () =>
                {
                    var old = config.KeepUpgrades;
                    config.KeepUpgrades = false;
                    PlayerUpgradeStats.Instance.Reset();
                    config.KeepUpgrades = old;
                });
            
            
            var sXp = UnityEngine.Object.Instantiate(sliderPrefab, settingsContent);
            sXp.gameObject.name = "ExperienceMultiplier";
            var sXpText = sXp.GetComponentInChildren <TMP_Text>();
                
                
            sXpText.text = "XP Required Multiplier (%)";
            var sXps = sXp.GetComponentInChildren <Slider>();
            sXps.onValueChanged.m_PersistentCalls.Clear();
            sXps.onValueChanged.DirtyPersistentCalls();
            sXps.onValueChanged.RebuildPersistentCallsIfNeeded();

            sXps.minValue = 20;
            sXps.maxValue = 500;
            
            sXps.onValueChanged.AddListener(
                (v) =>
                {
                    config.XpRequiredMultiplier = v / 100f;
                });
            
            sXps.SetValueWithoutNotify((float)(config.XpRequiredMultiplier * 100d));
            
            CreateSection(settingsContent, "Debug");
            
            var triggerNextWave = CreateButton(settingsContent, "trigger next wave");

            triggerNextWave.onClick.AddListener(
                () =>
                {
                    EndlessGrid.Instance.Invoke("NextWave", 1f);
                });

            CreateSection(settingsContent, "Upgrade Appear Multipliers (%)");

            foreach (var upgrade in RandomUpgrade.All.OrderByDescending(u => u.OptionsSortPriority))
            {
                var slider1 = UnityEngine.Object.Instantiate(sliderPrefab, settingsContent);
                slider1.gameObject.name = upgrade.Name;
                var slider1Text = slider1.GetComponentInChildren <TMP_Text>();
                
                
                slider1Text.text = upgrade.Name;
                var slider1s = slider1.GetComponentInChildren <Slider>();
                slider1s.onValueChanged.m_PersistentCalls.Clear();
                slider1s.onValueChanged.DirtyPersistentCalls();
                slider1s.onValueChanged.RebuildPersistentCallsIfNeeded();

                var upgradeCaptured = upgrade;
                slider1s.onValueChanged.AddListener(
                    (v) =>
                    {
                        upgradeCaptured.AppearChanceWeightingOptionMultiplier = v / 100f;
                    });
                slider1s.maxValue = 500f;
                slider1s.SetValueWithoutNotify((float)(upgrade.AppearChanceWeightingOptionMultiplier * 100d));
                //PrintClickEvent(slider1s.onValueChanged);
                
                var resetButton = slider1.GetComponentInChildren <Button>();
                var backSelect = resetButton.GetComponent <BackSelectOverride>();
            }

            //DumpHierarchy(settingsContent.parent);
        }
        
        gameplayOptions.gameObject.SetActive(false);


        yield break;
    }
    
    private static void PrintEvent(PersistentCallGroup calls)
    {
        OK.Log("Persistent calls: " + 
               string.Join(", ", calls.m_Calls.Select(
                               call =>
                               {
                                   string callerName = call.target.name;

                                   if (call.target is Component c)
                                   {
                                       callerName = $"{c.gameObject.name}.{c.GetType().Name}";
                                   }

                                   return
                                       $"{callerName}.{call.methodName}({(call.arguments.boolArgument)}))";
                               })));


        
        

    }

    private static void PrintClickEvent(Button.ButtonClickedEvent evnt) => PrintEvent(evnt.m_PersistentCalls);

    private static void PrintClickEvent(Slider.SliderEvent evnt) => PrintEvent(evnt.m_PersistentCalls);

    private static void OnClickOverkillTab()
    {
        settings.gameObject.SetActive(true);
    }
    
    private static void OnClickNotOverkillTab()
    {
        OK.Log("ON CLICK NOT OVERKILL!", LogLevel.Error);
        settings.gameObject.SetActive(false);
    }
    
    private static Transform CreateSection(Transform parent, string text)
    {
        GameObject section = Object.Instantiate(sectionPrefab, parent);
        section.name = text;
        section.GetComponent<TextMeshProUGUI>().text = text;
        return section.transform;
    }
    
    private static Toggle CreateToggle(Transform parent, string text)
    {
        GameObject go = Object.Instantiate(togglePrefab, parent);
        go.name = text;
        go.GetComponentInChildren<TextMeshProUGUI>().text = text;

        var toggle = go.GetComponentInChildren<Toggle>();

        toggle.onValueChanged.m_PersistentCalls.Clear();
        toggle.onValueChanged.DirtyPersistentCalls();
        toggle.onValueChanged.RebuildPersistentCallsIfNeeded();
        
        return toggle;
    }
    
    private static Button CreateButton(Transform parent, string text)
    {
        GameObject go = Object.Instantiate(buttonPrefab, parent);
        go.name = text;
        go.GetComponentInChildren<TextMeshProUGUI>().text = text;

        var button = go.GetComponentInChildren<Button>();

        button.onClick.m_PersistentCalls.Clear();
        button.onClick.DirtyPersistentCalls();
        button.onClick.RebuildPersistentCallsIfNeeded();
        button.onClick.RemoveAllListeners();
        
        return button;
    }
}
