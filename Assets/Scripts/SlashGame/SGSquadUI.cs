using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom squad bar — 4 colored circle slots.
/// Locked slots show as dark silhouettes with a score threshold label.
/// Unlocking a character triggers a pop + glow animation.
/// The upgrade popup appears when enough XP accumulates.
/// </summary>
public class SGSquadUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Squad Data")]
    public SGSquadData squadData;

    [Header("Slot Icons (Speed / Lucky / Guardian / Fury)")]
    public Image[] slotIcons = new Image[4];

    [Header("Slot Labels (name or score threshold)")]
    public TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[4];

    [Header("Slot Glow rings (one Image per slot)")]
    public Image[] slotGlows = new Image[4];

    [Header("Level-up Popup")]
    public GameObject      levelUpPanel;
    public TextMeshProUGUI levelUpText;
    public Button          levelUpConfirmButton;

    // ── Internal ──────────────────────────────────────────────────────────────

    private int                pendingUpgradeIndex = -1;
    private readonly Coroutine[] glowCoroutines    = new Coroutine[4];

    // Locked slot appearance
    private static readonly Color LockedColor   = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color LockedText    = new Color(0.35f, 0.35f, 0.35f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (levelUpConfirmButton != null)
            levelUpConfirmButton.onClick.AddListener(ConfirmUpgrade);
    }

    private void OnEnable()
    {
        SGGameManager.OnCharacterUnlocked += HandleCharacterUnlocked;
        SGGameManager.OnLevelUpReady      += HandleLevelUpReady;
        SGGameManager.OnFuryStarted       += HandleFuryStarted;
        SGGameManager.OnFuryEnded         += HandleFuryEnded;
        SGGameManager.OnGameStarted       += Refresh;
    }

    private void OnDisable()
    {
        SGGameManager.OnCharacterUnlocked -= HandleCharacterUnlocked;
        SGGameManager.OnLevelUpReady      -= HandleLevelUpReady;
        SGGameManager.OnFuryStarted       -= HandleFuryStarted;
        SGGameManager.OnFuryEnded         -= HandleFuryEnded;
        SGGameManager.OnGameStarted       -= Refresh;
    }

    private void Start() => Refresh();

    // ── Refresh all slots ─────────────────────────────────────────────────────

    /// <summary>Redraws every slot to match current squad state.</summary>
    public void Refresh()
    {
        if (squadData == null) return;

        for (int i = 0; i < 4; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int i)
    {
        if (squadData == null) return;
        bool   unlocked  = squadData.IsUnlocked((SGCharacterType)i);
        int    level      = squadData.GetLevel((SGCharacterType)i);
        Color  charColor  = SGCharacterDefs.Colors[i];
        string name       = SGCharacterDefs.Names[i];
        int    threshold  = SGCharacterDefs.UnlockScores[i];

        // ── Icon circle ───────────────────────────────────────────────────────
        if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            slotIcons[i].color = unlocked ? charColor : LockedColor;

        // ── Glow ring (visible only when unlocked) ────────────────────────────
        if (slotGlows != null && i < slotGlows.Length && slotGlows[i] != null)
        {
            Color g    = charColor;
            g.a        = unlocked ? 0.25f : 0f;
            slotGlows[i].color = g;
        }

        // ── Label ─────────────────────────────────────────────────────────────
        if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
        {
            if (unlocked)
            {
                slotLabels[i].text  = level > 0 ? $"{name}\nLV{level}" : name;
                slotLabels[i].color = Color.white;
            }
            else
            {
                slotLabels[i].text  = $"{threshold}pts";
                slotLabels[i].color = LockedText;
            }
        }
    }

    // ── Unlock animation ──────────────────────────────────────────────────────

    private void HandleCharacterUnlocked(int index)
    {
        RefreshSlot(index);
        if (glowCoroutines[index] != null) StopCoroutine(glowCoroutines[index]);
        glowCoroutines[index] = StartCoroutine(UnlockPopRoutine(index));
    }

    private IEnumerator UnlockPopRoutine(int i)
    {
        if (slotIcons == null || i >= slotIcons.Length || slotIcons[i] == null) yield break;

        Color charColor = SGCharacterDefs.Colors[i];
        Transform iconTr = slotIcons[i].transform;

        // ── Scale pop ─────────────────────────────────────────────────────────
        float elapsed = 0f, dur = 0.35f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / dur;
            // Overshoot spring: 0 → 1.4 → 1
            float s  = t < 0.5f
                ? Mathf.Lerp(1f, 1.45f, t / 0.5f)
                : Mathf.Lerp(1.45f, 1f, (t - 0.5f) / 0.5f);
            iconTr.localScale = Vector3.one * s;
            yield return null;
        }
        iconTr.localScale = Vector3.one;

        // ── Sustained glow pulse for 1.5s to attract attention ────────────────
        if (slotGlows != null && i < slotGlows.Length && slotGlows[i] != null)
        {
            elapsed = 0f;
            float glowDur = 1.5f;
            while (elapsed < glowDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / glowDur;
                float a  = Mathf.Lerp(0.6f, 0f, t) * (0.7f + 0.3f * Mathf.Sin(elapsed * 18f));
                Color g  = charColor;
                g.a      = a;
                slotGlows[i].color = g;
                yield return null;
            }
            // Return to base glow
            Color base_ = charColor; base_.a = 0.25f;
            slotGlows[i].color = base_;
        }
    }

    // ── Level-up popup ────────────────────────────────────────────────────────

    private void HandleLevelUpReady(int characterIndex)
    {
        if (levelUpPanel == null) return;
        pendingUpgradeIndex = characterIndex;

        string charName = characterIndex < SGCharacterDefs.Names.Length
            ? SGCharacterDefs.Names[characterIndex] : "CHARACTER";
        string desc = characterIndex < SGCharacterDefs.Descriptions.Length
            ? SGCharacterDefs.Descriptions[characterIndex] : "";

        if (levelUpText != null)
            levelUpText.text = $"AMÉLIORER\n{charName}\n<size=70%>{desc}</size>";

        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ConfirmUpgrade()
    {
        Time.timeScale = 1f;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (squadData == null || pendingUpgradeIndex < 0) return;

        squadData.Upgrade(pendingUpgradeIndex);
        pendingUpgradeIndex = -1;
        Refresh();
    }

    // ── Fury pulse on Fury slot ───────────────────────────────────────────────

    private Coroutine furyCoroutine;

    private void HandleFuryStarted()
    {
        int idx = (int)SGCharacterType.Fury;
        if (slotIcons == null || idx >= slotIcons.Length || slotIcons[idx] == null) return;

        if (furyCoroutine != null) StopCoroutine(furyCoroutine);
        furyCoroutine = StartCoroutine(FuryPulseRoutine(idx));
    }

    private void HandleFuryEnded()
    {
        if (furyCoroutine != null) { StopCoroutine(furyCoroutine); furyCoroutine = null; }
        Refresh();
    }

    private IEnumerator FuryPulseRoutine(int i)
    {
        Color baseColor = SGCharacterDefs.Colors[i];
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 7f;
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t));
            if (slotIcons[i] != null)
                slotIcons[i].color = Color.Lerp(Color.white, baseColor, pulse);
            yield return null;
        }
    }
}
