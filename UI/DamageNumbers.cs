using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BepInEx.Logging;
using GameConsole.pcon;
using OVERKILL.HakitaPls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OVERKILL;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageNumber : MonoBehaviour
{
    private static Canvas canvas;
    private static AnimationCurve sizeAnimationCurve;

    private static Dictionary <EnemyIdentifier, DamageNumber> damageNumbersByTarget =
        new Dictionary <EnemyIdentifier, DamageNumber>(64);

    private Transform target;
    private EnemyIdentifier enemyId;
    private TMP_Text text;
    private float value;

    private float startTime;
    private float positionAnimationStartTime;
    private float lifeTime = 1f;
    public bool isDead = false;



    private void Awake()
    {
        EnsureCanvasExists();

        startTime = Time.time;
        positionAnimationStartTime = startTime;
        
        text = GetComponent <TMP_Text>();
        text.richText = true;
    }

    private static void EnsureCanvasExists()
    {
        if (canvas == null)
        {
            canvas = CheatsController.Instance.cheatsInfo.canvas;
            sizeAnimationCurve = new AnimationCurve();
            sizeAnimationCurve.AddKey(new Keyframe(0f, 0f, 0f, 10f));
            sizeAnimationCurve.AddKey(new Keyframe(0.08f, 3f, 0f, 0f));
            sizeAnimationCurve.AddKey(new Keyframe(0.25f, 1f, -1f, 0f));
            sizeAnimationCurve.AddKey(new Keyframe(0.8f, 1f, 0f, -1f));
            sizeAnimationCurve.AddKey(new Keyframe(1f, 0f, 0f, 0f));
            sizeAnimationCurve.AddKey(new Keyframe(5f, 0f, 0f, 0f));


        }
    }

    public static DamageNumber CreateOrAddToExistingHit(
        EventDelegates.DeliverDamageEventData evntData,
        Transform target,
        EnemyIdentifier enemyId,
        Vector3 hitPoint,
        float value)
    {
        EnsureCanvasExists();
        

        if (damageNumbersByTarget.TryGetValue(enemyId, out var existing))
        {
            if (existing.isDead)
                return existing;
            
            existing.value += value;
            existing.target = target;
            existing.lifeTime = Mathf.Clamp(existing.lifeTime + 0.15f, 1f, 2.5f);
            SetDamageNumberText(evntData, existing);
            existing.startTime = Time.time - existing.lifeTime * 0.032f;

            existing.isDead = enemyId.dead;
            return existing;
        }
        
        //var go = new GameObject("DN", typeof(DamageNumber));
        //var go = Instantiate(HudController.Instance.textElements[1].gameObject, HudController.Instance.textElements[1].transform.parent);
        var go = Instantiate(CheatsController.Instance.cheatsInfo.gameObject, canvas.transform);

        //var dn = go.GetComponent <DamageNumber>();
        var dn = go.AddComponent <DamageNumber>();
        dn.target = target;
        dn.enemyId = enemyId;
        dn.value = value;
        SetDamageNumberText(evntData, dn);
        
        dn.text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        dn.text.verticalAlignment = VerticalAlignmentOptions.Bottom;


        var rt = dn.text.rectTransform;

        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.right;
        rt.sizeDelta = new Vector2(200f, 30f);
        rt.anchoredPosition = Vector2.zero;

        damageNumbersByTarget.Add(enemyId, dn);
        return dn;
    }

    private static void SetDamageNumberText(
        EventDelegates.DeliverDamageEventData evntData,
        DamageNumber existing)
    {
        existing.text.text = existing.value.ToString("0.");

        if (evntData.isHeadshot)
        {
            existing.text.color = Color.yellow;
            existing.text.text += "!";
        }
        else
        {
            existing.text.color = Color.white;
        }
    }

    private void Update()
    {
        float timeLived = Time.time - startTime;
        
        if (timeLived > lifeTime || target == null)
        {
            damageNumbersByTarget.Remove(enemyId);
            Destroy(gameObject);
            return;
        }




        var pos = Camera.main.WorldToScreenPoint(target.transform.position);

        var dir = (target.transform.position -  Camera.main.transform.position).normalized;
        var dot = Vector3.Dot(Camera.main.transform.forward, dir);

        if (dot < 0f) //off don't render when it's on the other side of the screen.
        {
            text.transform.position = Vector3.down * 1000f;

            return;
            
        }
        float timeLived01 = Mathf.Clamp01(timeLived / lifeTime);
        var rt = text.rectTransform;
        //text.transform.position = new Vector3(pos.x, pos.y, text.transform.position.z);



        //text.fontSize = sizeAnimationCurve.Evaluate(timeLived01) * 32f;
        text.transform.localScale = Vector3.one * Mathf.Clamp(sizeAnimationCurve.Evaluate(timeLived) * (0.8f + Mathf.Clamp(value * 0.01f, 0f, 1.2f)) * (enemyId.dead ? 0.5f : 1f), 0f, 10f);

        text.transform.position = new Vector3(pos.x, pos.y - 15f + Mathf.Pow(Mathf.Clamp01(Time.time - positionAnimationStartTime), 0.5f) * 60f, text.transform.position.z);
    }
}

public static class DamageNumbers
{
    private static bool enemyWasDead;
    public static void Initialize()
    {
        Events.OnDealDamage.Pre += OnPreDeliverDamage;
        Events.OnDealDamage.Post += OnDeliverDamage;
    }

    private static void OnPreDeliverDamage(EventDelegates.DeliverDamageEventData evntData, EnemyIdentifier enemy, GameObject target, Vector3 force, Vector3 hitpoint, ref float multiplier, bool tryforexplode, float critmultiplier, GameObject sourceweapon, bool ignoretotaldamagetakenmultiplier, bool fromexplosion)
    {
        enemyWasDead = enemy == null || enemy.dead;
    }



    private static void OnDeliverDamage(EventDelegates.DeliverDamageEventData evntData, EnemyIdentifier enemy, GameObject target, Vector3 force, Vector3 hitpoint, ref float multiplier, bool tryforexplode, float critmultiplier, GameObject sourceweapon, bool ignoretotaldamagetakenmultiplier, bool fromexplosion)
    {
        /*
        if (sourceweapon != null && target != null)
        {
            if (sourceweapon.TryGetComponent <WeaponTypeComponent>(out var wt))
            {
                OK.Log($"Dealt damage with sourceweapon: {wt.value.ToString()}");
            }
            else
            {
                OK.Log($"Dealt damage with unknown weapon type: {sourceweapon.name}");
            }

        }
        */
        
        if (multiplier > 0f && !enemyWasDead)
            DamageNumber.CreateOrAddToExistingHit(evntData, target.transform, enemy, hitpoint, multiplier * 10f);
    }
}
