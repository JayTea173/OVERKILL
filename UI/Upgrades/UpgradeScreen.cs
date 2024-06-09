using System;
using System.Collections.Generic;
using System.Linq;
using OVERKILL.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace OVERKILL.UI.Upgrades;

public class UpgradeScreen : MonoBehaviour
{
    public DateTime timeShown;
    private int timesToBeShown = 0;

    public Rarity minUpgradeRarity = Rarity.Common;

    public static int numExtraChoices;
    public static int advantage;

    private static UpgradeScreen instance;

    private Canvas canvas;
    private RectTransform upgradesParent;
    private readonly List <UpgradeCard> cards = new();

    public static UpgradeScreen Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("UpgradeScreen");
                go.transform.SetParent(CheatsController.Instance.cheatsInfo.canvas.transform);
                instance = go.AddComponent <UpgradeScreen>();
                instance.gameObject.SetActive(false);
            }

            return instance;
        }
        private set => instance = value;
    }

    public bool Shown => gameObject != null && gameObject.activeSelf;

    #region Unity Event Functions

    private void Awake()
    {
        instance = this;
        canvas = CheatsController.Instance.cheatsInfo.canvas;
        var rt0 = gameObject.GetOrAddComponent <RectTransform>();
        rt0.pivot = new Vector2(.5f, .5f);
        rt0.anchorMin = Vector2.zero;
        rt0.anchorMax = Vector2.one;
        rt0.sizeDelta = Vector2.zero;
        rt0.anchoredPosition = Vector2.one;

        //rt0.transform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        var go = new GameObject("UpgradesLayoutGroup");

        var img = go.GetOrAddComponent <RawImage>();
        upgradesParent = img.rectTransform;
        img.color = Color.clear;
        RectTransform rt = img.rectTransform;
        rt.SetParent(transform);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(160f, 0f);
        rt.anchoredPosition = Vector2.one;

        //rt.anchoredPosition = Vector2.zero;
        //rt.transform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        var layout = rt.gameObject.AddComponent <HorizontalLayoutGroup>();

        var res = new TMP_DefaultControls.Resources();
        
        //GameObject skipButton = TMP_DefaultControls.CreateButton(res);
        GameObject skipButtonGo = Options.CreateButton(transform, "SKIP").gameObject;
        skipButtonGo.GetComponent <Button>().onClick.AddListener(OnSkip);
        //skipButton.transform.SetParent(transform);
        var rtSkip = skipButtonGo.transform as RectTransform;

        rtSkip.pivot = new Vector2(.5f, 1f);
        rtSkip.anchorMin = new Vector2(0.5f, 0f);
        rtSkip.anchorMax = new Vector2(0.5f, 0f);
        rtSkip.sizeDelta = new Vector2(200f, 40f);
        rtSkip.anchoredPosition = Vector2.down * 64f;
        //var textReroll = skipButton.GetComponentInChildren <TMP_Text>();
        //textReroll.text = "SKIP";
        //skipButton.GetComponent <Button>().onClick.AddListener(OnSkip);
    }

    #endregion

    #region Public

    public IEnumerable <IUpgrade> GetRarityUpgrades(HashSet <UpgradeChoice> existingChoices)
    {
        return PlayerUpgradeStats.Instance.upgrades.Where(u => u.Value.Rarity < u.Value.MaxRarity && !existingChoices.Contains(new UpgradeChoice()
        {
            upgrade = u.Value
        })).Select(kv => kv.Value);
    }
    
    public IEnumerable <IUpgrade> GetLevelUpgrades(HashSet <UpgradeChoice> existingChoices)
    {
        return PlayerUpgradeStats.Instance.upgrades.Where(u => u.Value is LeveledUpgrade lvl && lvl.level < lvl.MaxLevel && !existingChoices.Contains(new UpgradeChoice()
        {
            upgrade = u.Value
        })).Select(kv => kv.Value);
    }

    public void Hide()
    {
        foreach (UpgradeCard card in cards)
            Destroy(card.gameObject);

        cards.Clear();
        gameObject.SetActive(false);

        OptionsManager.Instance.UnPause();
        OptionsManager.Instance.UnFreeze();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        if (timesToBeShown <= 0)
            return;
        else
            Show();
    }

    public void ShowTimes(int times)
    {
        timesToBeShown += times;

        if (!gameObject.activeSelf)
            Show();
    }

    public void Show()
    {
        timesToBeShown--;
        gameObject.SetActive(true);

        RandomUpgrade.UpdateAvailable();

        var rnd = new Random();

        HashSet <UpgradeChoice> upgradeChoices = new();
        List <UpgradeChoice> upgrades = new();
        UpgradeChoice upgrade = null;

        var maxTries = 800;

        for (var i = 0; i < 3 + numExtraChoices; i++)
        {
            IUpgrade[] rarityUpgrades = GetRarityUpgrades(upgradeChoices).ToArray();
            IUpgrade[] levelUpgrades = GetLevelUpgrades(upgradeChoices).ToArray();
            
            var rndV = rnd.NextDouble();

            var tries = 0;

            //card choice

            do
            {
                if (levelUpgrades.Length > 0 && rndV < 0.25d)
                {
                    //rarity upgrade choice
                    upgrade = new UpgradeChoice()
                    {
                        upgrade = levelUpgrades[rnd.Next(levelUpgrades.Length)], upgradeLevel = true
                    };
                } else if (rarityUpgrades.Length > 0 && rndV < 0.05d)
                {
                    //rarity upgrade choice
                    upgrade = new UpgradeChoice()
                    {
                        upgrade = rarityUpgrades[rnd.Next(rarityUpgrades.Length)], upgradeRarity = true
                    };
                }
                else
                {
                    upgrades.Clear();

                    for (var j = 0; j <= Mathf.Abs(advantage); j++)
                        upgrades.Add(new UpgradeChoice()
                        {
                            upgrade = RandomUpgrade.Get(rnd),
                            upgradeLevel = true
                        });

                    if (advantage >= 0)
                    {
                        foreach (var u in upgrades)
                        {
                            if (upgrade == null || upgrade.upgrade.Rarity < u.upgrade.Rarity)
                                upgrade = u;
                        }
                    }
                    else
                    {
                        foreach (var u in upgrades)
                        {
                            if (upgrade == null || upgrade.upgrade.Rarity > u.upgrade.Rarity)
                                upgrade = u;
                        }
                    }
                }
                
                tries++;

                if (tries > maxTries)
                    break;

            }
            while (upgradeChoices.Contains(upgrade) || upgrade != null && upgrade.upgrade.Rarity < minUpgradeRarity);

            if (tries > maxTries || upgrade == null)
                continue;

            var card = UpgradeCard.Create(upgradesParent, rnd, upgrade);
            cards.Add(card);
            upgradeChoices.Add(upgrade);
            upgrade = null;
        }

        OptionsManager.Instance.Pause();

        //OptionsManager.Instance.pauseMenu.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        timeShown = DateTime.UtcNow;
    }

    #endregion

    #region Private

    private void OnSkip()
    {
        Hide();
    }

    #endregion
}

public class UpgradeChoice
{
    public IUpgrade upgrade;
    public bool upgradeLevel;
    public bool upgradeRarity;
        

    protected bool Equals(UpgradeChoice other)
    {
        return Equals(upgrade, other.upgrade);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != this.GetType())
            return false;

        return Equals((UpgradeChoice)obj);
    }
        
    public override int GetHashCode()
    {
        return (upgrade != null ? upgrade.GetHashCode() : 0);
    }
}
