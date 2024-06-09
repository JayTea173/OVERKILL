using System;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameConsole.pcon;
using OVERKILL.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace OVERKILL.UI.Upgrades;

public class UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TMP_Text nameText, descriptionText;
    private VerticalLayoutGroup content;
    private Button chooseButton;
    public UpgradeChoice Choice {get; private set;}
    private RawImage bg;
    private RawImage contentRawImage;

    public static UpgradeCard Create(Transform parent, System.Random rnd, UpgradeChoice choice)
    {
        var t = HudController.Instance.gunCanvas.transform;
        
        
#pragma warning disable CS0618
        var gunPanel = t.FindChild("GunPanel").gameObject;
#pragma warning restore CS0618

        var cardGameObject = new GameObject("Card");

        if (parent == null)
            throw new NullReferenceException("PARENT OF CARD CANT BE NULL");

        cardGameObject.transform.SetParent(parent);
        cardGameObject.layer = gunPanel.layer;
        
        var card = cardGameObject.AddComponent <UpgradeCard>();
        card.Choice = choice;
        
        
        if (card.Choice.upgrade is LeveledUpgrade lvl && card.Choice.upgradeLevel)
            lvl.level++;
        if (card.Choice.upgradeRarity)
            card.Choice.upgrade.Rarity++;
        
        card.bg = card.gameObject.AddComponent <RawImage>();
        card.bg.texture = null;
        //card.bg.color = new Color(0f, 0f, 0f, 0.9f);
        card.bg.color = Color.clear;
        var rt = card.bg.rectTransform;
        rt.pivot = new Vector2(0.0f, 0.0f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        card.content = new GameObject("content").AddComponent<VerticalLayoutGroup>();
        card.content.spacing = 32f;
        card.content.transform.SetParent(card.transform);
        card.contentRawImage = card.content.gameObject.AddComponent <RawImage>();
        var contentRT = card.contentRawImage.rectTransform;
        
        card.contentRawImage.color = new Color(0f, 0f, 0f, 0.96f);
        card.contentRawImage.raycastTarget = true;
        contentRT.pivot = new Vector2(0.0f, 0.0f);
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(-48f, -16f);
        
        
        
        

        //var layoutEl = go.AddComponent <LayoutElement>();
        //layoutEl.preferredWidth = 300f;
        card.content.childForceExpandWidth = true;
        card.content.childForceExpandHeight = false;
        card.content.childControlHeight = false;
        card.content.childControlWidth = true;
        
        //var (card.content.transform as RectTransform)
        //inst.content.spacing = 32f;
        


        //bg.rectTransform.sizeDelta = new Vector2(300f, bg.rectTransform.sizeDelta.y);
        
        card.nameText = Instantiate(CheatsController.Instance.cheatsInfo.gameObject, card.content.transform).GetComponent<TMP_Text>();
        card.descriptionText = Instantiate(CheatsController.Instance.cheatsInfo.gameObject, card.content.transform).GetComponent<TMP_Text>();
        card.nameText.fontSize *= 1.25f;
        card.nameText.rectTransform.sizeDelta = new Vector2(card.nameText.rectTransform.sizeDelta.x, 58f);
        card.descriptionText.rectTransform.sizeDelta = new Vector2(card.descriptionText.rectTransform.sizeDelta.x, 600f);
        card.descriptionText.fontSize *= 0.8f;

        card.nameText.text = card.Choice.upgrade.RTFName();

        if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(card.Choice.GetHashCode(), out var existing))
        {
            if (card.Choice.upgrade is LeveledUpgrade lvlUpgrade && choice.upgradeLevel)
            {
                card.nameText.text +=
                    $"\n<size=75%><b>Level {lvlUpgrade.level - 1} => {lvlUpgrade.level}</b></size>";
            }
            
            if (choice.upgradeRarity)
                card.nameText.text +=
                    $"\n<size=75%><b>{(existing.Rarity-1).ToString().ColoredRTF(RarityColor.Get(existing.Rarity-1))} => {card.Choice.upgrade.Rarity.ToString().ColoredRTF(RarityColor.Get(card.Choice.upgrade.Rarity))}</b></size>";
        }
        else // new card
        {
            if (card.Choice.upgrade is LeveledUpgrade lvlUpgrade && choice.upgradeLevel)
            {
                if (lvlUpgrade.MaxLevel > 1)
                    card.nameText.text += $"\n<size=75%>Max Level: {lvlUpgrade.MaxLevel}</size>";
            }
        }

        card.descriptionText.text = card.Choice.upgrade.Description;

        card.nameText.margin = new Vector4(8f, 8f, 8f, 8f);
        card.descriptionText.margin = new Vector4(8f, 8f, 8f, 8f);

        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();
        //card.chooseButton = TMP_DefaultControls.CreateButton(resources).GetComponent<Button>();
        card.chooseButton = Options.CreateButton(card.content.transform, "GIMME!");
        card.chooseButton.transform.position += Vector3.up * 64f;
        //card.chooseButton.transform.SetParent(card.content.transform);
        card.chooseButton.transform.SetAsLastSibling();
        
        card.chooseButton.onClick.AddListener(card.OnClick);
        //card.chooseButton.GetComponentInChildren <TMP_Text>().text = "GIMME!";
        
        if (card.Choice.upgrade is LeveledUpgrade lvl0 && card.Choice.upgradeLevel)
            lvl0.level--;
        if (card.Choice.upgradeRarity)
            card.Choice.upgrade.Rarity--;

        return card;
    }

    private void OnClick()
    {

        if (Event.current.button != 0)
            return;
        
        try
        {
            if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(Choice.GetHashCode(), out var existing))
            {
                existing.Absolve();

                if (Choice.upgradeRarity)
                    existing.Rarity++;

                if (existing is LeveledUpgrade lvl && Choice.upgradeLevel)
                    lvl.level++;
                
                existing.Apply();
                PlayerUpgradeStats.Instance.upgradesAreApplied = true;

            }
            else if (!PlayerUpgradeStats.Instance.upgrades.ContainsKey(Choice.GetHashCode()))
            {
                //upgrade will be newly added
                if (Choice.upgrade is LeveledUpgrade lvl0)
                    lvl0.level++;
                
                PlayerUpgradeStats.Instance.upgrades.Add(Choice.GetHashCode(), Choice.upgrade);
                Choice.upgrade.Apply();
                PlayerUpgradeStats.Instance.upgradesAreApplied = true;

            } else
                OK.Log($"Adding {Choice.upgrade.Name} upgrade failed! Already added and not leveled.", LogLevel.Error);
            
            
        }
        catch (Exception ex)
        {
            OK.Log(ex, LogLevel.Error);
        }

        UpgradeScreen.Instance.Hide();
    }

    private static void PrintHierarchyRecursive(int depth, int childIndex, Transform t, StringBuilder sb)
    {
        sb.AppendLine(new string('\t', depth) + t.gameObject.GetGameObjectScenePath());

        for (int i = 0; i < t.childCount; i++)
        {
            PrintHierarchyRecursive(depth + 1, i, t.GetChild(i), sb);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        contentRawImage.color = new Color(0.0f, 0.2f, 0.2f, 0.96f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        contentRawImage.color = new Color(0f, 0f, 0f, 0.96f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DateTime.UtcNow - UpgradeScreen.Instance.timeShown > TimeSpan.FromSeconds(0.66d))
            OnClick();
    }
}
