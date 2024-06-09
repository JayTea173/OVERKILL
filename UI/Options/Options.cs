using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using OVERKILL.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OVERKILL.UI;

public class OptionsValues
{
    public bool KeepUpgrades = false;
    public double XpRequiredMultiplier = 1d;
    public float XpBarOffset = 0f;
}

[HarmonyPatch(typeof (OptionsMenuToManager))]
public class Options
{
    private static Transform optionsMenu;
    private static Transform settings, settingsContent;
    private static GameObject sliderPrefab, sectionPrefab, togglePrefab, buttonPrefab;

    public static OptionsValues config = new OptionsValues();
    private static Button okButton;

    public static void DumpHierarchy(Transform t)
    {
        StringBuilder sb = new StringBuilder();
        
        DumpHierarchyRecursive(t, sb);
        OK.Log($"Hierarchy of {t.gameObject.name}\n{sb.ToString()}");
    }

    public static void DumpHierarchyDelayed(Transform t, float delay)
    {
        NewMovement.Instance.StartCoroutine(DumpHierarchyDelayedCoroutine(t, delay));
    }
    
    public static IEnumerator DumpHierarchyDelayedCoroutine(Transform t, float delay)
    {
        yield return new WaitForSeconds(delay);
        DumpHierarchy(t);
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
    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.CloseOptions))]
    public static void OnCloseOptions(OptionsManager __instance)
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.OpenOptions))]
    public static void OptionsMenuToManager_Open_Prefix(OptionsManager __instance)
    {
        OK.Log("OPEN OPTIONS!!");
        __instance.StartCoroutine(OptionsMenuToManager_Open_Prefix_Coroutine(__instance));

    }

    public static IEnumerator OptionsMenuToManager_Open_Prefix_Coroutine(OptionsManager __instance)
    {
        //I'm silly and don't know how to do compatibility for this
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.25f);

            var layout = optionsMenu.GetComponentInChildren <VerticalLayoutGroup>();
            var buttons = layout.transform.GetComponentsInChildren <Button>();


            foreach (var button in buttons)
            {
                if (button.GetComponentInParent <VerticalLayoutGroup>() == layout && button != okButton)
                {
                    button.onClick.RemoveListener(OnClickNotOverkillTab);
                    button.onClick.AddListener(OnClickNotOverkillTab);

                    //button.onClick.DirtyPersistentCalls();
                    //button.onClick.RebuildPersistentCallsIfNeeded();
                }
                else
                {
                    OK.Log($"Button without layout parent: {button.gameObject.name}");
                }
            }
        }
    }



    private static IEnumerator GenerateUICoroutine(OptionsMenuToManager optionsMenuToManager)
    {

        var layout = optionsMenu.GetComponentInChildren <VerticalLayoutGroup>();

        

        var audioButton = layout.transform.Find("Audio");
        var okButtonGo = UnityEngine.Object.Instantiate(audioButton, layout.transform);
        
        OK.Log($"SET SIBLING TO {audioButton.GetSiblingIndex()}");
        yield return new WaitForEndOfFrame();
        okButtonGo.transform.SetSiblingIndex(audioButton.GetSiblingIndex());

        okButtonGo.gameObject.name = "OVERKILL";

        var okText = okButtonGo.GetComponentInChildren <TMP_Text>();
        okText.text = "OVERKILL";
        okText.color = RarityColor.Get(Rarity.Overkill);
        
        
        
        okButton = okButtonGo.GetComponent <Button>();


        
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
            
            
            Slider xpMultiplierSlider = CreateSlider(settingsContent, "Req. Experience", 20, 500, (float)(config.XpRequiredMultiplier * 100d));

            xpMultiplierSlider.onValueChanged.AddListener(
                (v) =>
                {
                    config.XpRequiredMultiplier = v / 100f;
                });

            CreateSection(settingsContent, "Debug");
            
            var triggerNextWave = CreateButton(settingsContent, "trigger next wave");

            triggerNextWave.onClick.AddListener(
                () =>
                {
                    EndlessGrid.Instance.Invoke("NextWave", 1f);
                });

            var xpBarOffsetSlider = CreateSlider(settingsContent, "XP bar position", -100f, 100f, config.XpBarOffset);
            xpBarOffsetSlider.onValueChanged.AddListener(
                (v) =>
                {
                    //fuck em floating point precision
                    XPMeter.Instance.UpdateOffset(v - config.XpBarOffset);
                    config.XpBarOffset = v;
                    
                });

            CreateSection(settingsContent, "Upgrade Appear Multipliers (%)");

            foreach (var upgrade in RandomUpgrade.All.OrderByDescending(u => u.OptionsSortPriority))
            {
                var slider = CreateSlider(settingsContent, upgrade.Name, 0f, 800f, (float)(upgrade.AppearChanceWeightingOptionMultiplier * 100d));
                

                var upgradeCaptured = upgrade;
                slider.onValueChanged.AddListener(
                    (v) =>
                    {
                        upgradeCaptured.AppearChanceWeightingOptionMultiplier = v / 100f;
                    });
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
        settings.gameObject.SetActive(false);
    }
    
    private static Transform CreateSection(Transform parent, string text)
    {
        GameObject section = Object.Instantiate(sectionPrefab, parent);
        section.name = text;
        section.GetComponent<TextMeshProUGUI>().text = text;
        return section.transform;
    }
    
    public static Toggle CreateToggle(Transform parent, string text)
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
    
    public static Button CreateButton(Transform parent, string text)
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
    
    public static Slider CreateSlider(Transform parent, string text, float minValue, float maxValue, float initialValue = 0f)
    {
        var sXp = UnityEngine.Object.Instantiate(sliderPrefab, settingsContent);
        sXp.gameObject.name = text;
        var sXpText = sXp.GetComponentInChildren <TMP_Text>();

        sXpText.text = text;
        var sXps = sXp.GetComponentInChildren <Slider>();
        sXps.onValueChanged.m_PersistentCalls.Clear();
        sXps.onValueChanged.DirtyPersistentCalls();
        sXps.onValueChanged.RebuildPersistentCallsIfNeeded();

        sXps.minValue = minValue;
        sXps.maxValue = maxValue;
        sXps.SetValueWithoutNotify(initialValue);

        return sXps;
    }
}
