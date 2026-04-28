using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralise tous les feedbacks visuels du Parry Game :
/// - Explosion + burst rouge quand un ennemi est parrié (slash touche)
/// - Flash rouge sur les bords quand un ennemi passe (hurt)
/// - Vignette rouge qui pulse quand l'ennemi est proche (danger)
/// - Pop de combo (label flottant) à chaque parry réussi
/// - Flash doré quand une amélioration est activée
///
/// Se crée via <see cref="Spawn"/> depuis <see cref="PGSceneSetup"/>.
/// </summary>
public class PGFeedback : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PGFeedback Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    private const float DangerRange    = 2.5f;  // distance monde déclenchant le flash rouge
    private const float HurtFlashDur   = 0.35f;
    private const float ComboFadeDur   = 0.65f;
    private const float AbilityFlashDur = 0.40f;

    private static readonly Color ColDanger  = new Color(1f, 0.05f, 0.05f, 0f);
    private static readonly Color ColHurt    = new Color(1f, 0.05f, 0.05f, 0.60f);
    private static readonly Color ColAbility = new Color(1f, 0.85f, 0.10f, 0.50f);
    private static readonly Color ColCombo   = new Color(1f, 0.90f, 0.20f, 1f);

    // ── Références ────────────────────────────────────────────────────────────

    private Image   _dangerPanel;    // recouvre tout l'écran, rouge, alpha 0 au repos
    private Image   _hurtPanel;      // même chose — flash bref lors d'un hurt
    private Image   _abilityPanel;   // flash doré lors d'une amélioration
    private Canvas  _canvas;

    // ── Création ──────────────────────────────────────────────────────────────

    public static PGFeedback Spawn(RectTransform canvasRT)
    {
        var go  = new GameObject("PGFeedback");
        var fb  = go.AddComponent<PGFeedback>();
        fb.Init(canvasRT);
        return fb;
    }

    private void Init(RectTransform canvasRT)
    {
        Instance = this;
        _canvas  = canvasRT.GetComponent<Canvas>();

        _dangerPanel  = MakeFullPanel(canvasRT, "DangerPanel",  ColDanger);
        _hurtPanel    = MakeFullPanel(canvasRT, "HurtPanel",    Color.clear);
        _abilityPanel = MakeFullPanel(canvasRT, "AbilityPanel", Color.clear);

        // Abonnements
        PGGameManager.OnParrySuccess += OnParrySuccess;
        PGGameManager.OnParryFail    += OnHurt;
        PGGameManager.OnComboChanged += OnComboChanged;
        PGAbilitySystem.OnAbilityUsed += OnAbilityUsed;
    }

    private static Image MakeFullPanel(RectTransform canvasRT, string name, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(canvasRT, false);
        var img = go.AddComponent<Image>();
        img.color         = col;
        img.raycastTarget = false;
        var rt       = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PGGameManager.OnParrySuccess  -= OnParrySuccess;
        PGGameManager.OnParryFail     -= OnHurt;
        PGGameManager.OnComboChanged  -= OnComboChanged;
        PGAbilitySystem.OnAbilityUsed -= OnAbilityUsed;
    }

    private void Update()
    {
        UpdateDangerVignette();
    }

    // ── Danger vignette (pulse rouge selon proximité) ─────────────────────────

    private void UpdateDangerVignette()
    {
        if (_dangerPanel == null) return;

        float closest = float.MaxValue;
        foreach (var e in Object.FindObjectsByType<PGEnemy>(FindObjectsSortMode.None))
        {
            float d = e.transform.position.z;   // z décroît vers 0 en s'approchant
            if (d < closest) closest = d;
        }

        if (closest >= DangerRange)
        {
            _dangerPanel.color = Color.clear;
            return;
        }

        float danger  = 1f - Mathf.Clamp01(closest / DangerRange);
        float pulse   = Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(2f, 7f, danger)));
        float alpha   = danger * 0.45f * pulse;
        _dangerPanel.color = new Color(ColDanger.r, ColDanger.g, ColDanger.b, alpha);
    }

    // ── Parry réussi — explosion sur l'ennemi ─────────────────────────────────

    private void OnParrySuccess()
    {
        // L'explosion est déclenchée directement depuis PGEnemy.TriggerParry()
        // via PGExplosionFX.Spawn(). Ici on peut ajouter un flash écran léger.
        StartCoroutine(FlashPanel(_hurtPanel,
            new Color(1f, 1f, 1f, 0.12f), 0.18f));
    }

    // ── Hurt — flash rouge écran ───────────────────────────────────────────────

    private void OnHurt()
    {
        StopCoroutine(nameof(FlashHurtRoutine));
        StartCoroutine(FlashHurtRoutine());
    }

    private IEnumerator FlashHurtRoutine()
    {
        yield return FlashPanel(_hurtPanel, ColHurt, HurtFlashDur);
    }

    // ── Combo — label flottant ────────────────────────────────────────────────

    private void OnComboChanged(int combo)
    {
        if (combo < 2) return;
        StartCoroutine(SpawnComboLabel(combo));
    }

    private IEnumerator SpawnComboLabel(int combo)
    {
        if (_canvas == null) yield break;

        var go  = new GameObject("ComboLabel");
        go.transform.SetParent(_canvas.transform, false);

        var txt              = go.AddComponent<Text>();
        txt.text             = $"x{combo} COMBO!";
        txt.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize         = combo >= 5 ? 72 : 54;
        txt.fontStyle        = FontStyle.Bold;
        txt.color            = ColCombo;
        txt.alignment        = TextAnchor.MiddleCenter;
        txt.raycastTarget    = false;

        var rt           = txt.rectTransform;
        rt.anchorMin     = new Vector2(0.5f, 0.55f);
        rt.anchorMax     = new Vector2(0.5f, 0.55f);
        rt.pivot         = new Vector2(0.5f, 0.5f);
        rt.sizeDelta     = new Vector2(600f, 120f);
        rt.anchoredPosition = Vector2.zero;

        float t = 0f;
        while (t < ComboFadeDur && go != null)
        {
            t += Time.unscaledDeltaTime;
            float r = t / ComboFadeDur;
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 180f, r));
            float alpha = r < 0.3f
                ? Mathf.Lerp(0f, 1f, r / 0.3f)
                : Mathf.Lerp(1f, 0f, (r - 0.3f) / 0.7f);
            txt.color = new Color(ColCombo.r, ColCombo.g, ColCombo.b, alpha);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Amélioration — flash doré ─────────────────────────────────────────────

    private void OnAbilityUsed(PGAbilitySystem.AbilityType _)
    {
        StartCoroutine(FlashPanel(_abilityPanel, ColAbility, AbilityFlashDur));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerator FlashPanel(Image panel, Color peakColor, float duration)
    {
        if (panel == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float r     = t / duration;
            float alpha = r < 0.15f
                ? Mathf.Lerp(0f, peakColor.a, r / 0.15f)
                : Mathf.Lerp(peakColor.a, 0f, (r - 0.15f) / 0.85f);
            panel.color = new Color(peakColor.r, peakColor.g, peakColor.b, alpha);
            yield return null;
        }
        panel.color = Color.clear;
    }
}
