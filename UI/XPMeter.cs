using System;
using HarmonyLib;
using OVERKILL.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OVERKILL.UI;

public class XPMeter : MonoBehaviour
{
    public static XPMeter Instance {get; private set;}
    
    private Slider slider;
    private TMP_Text text;
    public TMP_Text bonusText;
    private int lastLevel;
    
    private void Awake()
    {
        Instance = this;
        var sliders = this.GetComponentsInChildren <Slider>();

        for (var index = 0; index < sliders.Length; index++)
        {
            var sl = sliders[index];
            OK.Log(sl.gameObject.GetGameObjectScenePath());
            //Destroy(sl.gameObject);
            sl.minValue = 0f;
            sl.maxValue = 1f;
            sl.value = 0f;

            sl.transform.localPosition += Vector3.up * 125f;
            sl.image.rectTransform.sizeDelta = new Vector2(sl.image.rectTransform.sizeDelta.x, sl.image.rectTransform.sizeDelta.y * .5f);

        }

        slider = sliders[2];
        text = this.GetComponent<TMP_Text>();
        //flash = bar.transform.GetChild(0).GetComponent<Image>();


        text = Instantiate(CheatsController.Instance.cheatsInfo.gameObject, transform).GetComponent<TMP_Text>();
        text.fontSize *= 0.75f;
        var rt = text.rectTransform;
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(8f, 82f);

        transform.position += Vector3.up * Options.config.XpBarOffset;

        slider.image.color = Color.cyan;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        
        //slider.image.rectTransform.position += Vector3.up * slider.image.rectTransform.rect.height * 0.2f;
        //slider.image.rectTransform.sizeDelta = new Vector2(slider.image.rectTransform.sizeDelta.x, slider.image.rectTransform.sizeDelta.y * .5f);

        //slider.minValue = 0f;
        //slider.maxValue = 1f;
        //flash = bar.transform.GetChild(0).GetComponent<Image>();


        bonusText = Instantiate(text, transform).GetComponent<TMP_Text>();
        rt = bonusText.rectTransform;

        bonusText.raycastTarget = false;
        bonusText.horizontalAlignment = HorizontalAlignmentOptions.Right;
        //bonusText.verticalAlignment = HorizontalAlignmentOptions.;
        //rt.anchoredPosition = Vector3.right;
        rt.position += Vector3.right * 0.1f;
        bonusText.fontSize *= 0.8f;
        bonusText.text = string.Empty;
    }

    public void UpdateOffset(float delta)
    {
        var rt = transform as RectTransform;

        rt.anchoredPosition += Vector2.up * delta * 1f;
        
    }

    private void Update()
    {
        

        var currLevel = PlayerUpgradeStats.Instance.okLevel;

        if (currLevel - 1 != lastLevel)
            OnLevelup();
        
        lastLevel = currLevel - 1;
        var lastXP = StyleLevelupThresholds.GetXPAtLevel(lastLevel);
        var currXP = StyleLevelupThresholds.GetXPAtLevel(currLevel);
        slider.value = Mathf.Lerp(slider.value, (float)(PlayerUpgradeStats.Instance.stylePoints - lastXP) / (float)(currXP - lastXP), Time.deltaTime * 12f);
        //text.text = $"XP {PlayerUpgradeStats.stylePoints} / {lastXP}";
        
        
        //text.text = $"Level {currLevel}   {slider.value:0.%}";
        text.text = string.Format("Level {0}   <size=75%>{1, 6} / {2}</size>", currLevel, (long)PlayerUpgradeStats.Instance.stylePoints - lastXP, currXP - lastXP);
        
        /*
        if (flash.color.a > 0.0f)
        {
            if (flash.color.a - Time.deltaTime > 0.0f)
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, flash.color.a - Time.deltaTime);
            else
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 0f);
        }
        */
        
    }

    private void OnLevelup()
    {
        //flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1f);
    }
}

[HarmonyPatch(typeof(HealthBar), "Start")]
public class InstantiateXPMeterPatch
{
    public static void Postfix(HealthBar __instance)
    {
        
        var inst = Object.Instantiate(__instance, __instance.transform.parent);
        Object.Destroy(inst.gameObject.GetComponent <HealthBar>());
        foreach (var text in inst.gameObject.GetComponentsInChildren<TMP_Text>())
            Object.Destroy(text.gameObject);
        
        inst.gameObject.AddComponent <XPMeter>();

    }
}

