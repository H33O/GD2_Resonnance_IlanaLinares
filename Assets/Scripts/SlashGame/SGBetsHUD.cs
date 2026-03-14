using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the bottom bet bar (3 square slots with current/max values)
/// and the top progress bar that fills as bets are completed.
/// A bet is completed when the player's score reaches the slot's target.
/// </summary>
public class SGBetsHUD : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const int   BetCount          = 3;
    private static readonly int[] BetTargets = { 10, 25, 50 };

    private static readonly Color ColorLocked    = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ColorActive    = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color ColorCompleted = new Color(0.20f, 0.80f, 0.20f, 1f);
    private static readonly Color ColorTextNormal    = Color.white;
    private static readonly Color ColorTextCompleted = Color.black;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Bottom Bet Slots")]
    public Image[]           betBackgrounds  = new Image[BetCount];
    public TextMeshProUGUI[] betLabels       = new TextMeshProUGUI[BetCount];

    [Header("Top Progress Bar")]
    public Image progressBarFill;

    // ── State ─────────────────────────────────────────────────────────────────

    private int   currentScore;
    private int   completedBets;
    private float displayFill;
    private float targetFill;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        SGGameManager.OnScoreChanged += HandleScoreChanged;
        SGGameManager.OnGameOver     += HandleGameOver;
    }

    private void OnDisable()
    {
        SGGameManager.OnScoreChanged -= HandleScoreChanged;
        SGGameManager.OnGameOver     -= HandleGameOver;
    }

    private void Start()
    {
        currentScore  = 0;
        completedBets = 0;

        // Ensure the fill image is configured correctly at runtime
        if (progressBarFill != null)
        {
            progressBarFill.type       = UnityEngine.UI.Image.Type.Filled;
            progressBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            progressBarFill.fillAmount = 0f;
        }

        RefreshAllSlots();
        UpdateProgressBar(0f, snap: true);
    }

    private void Update()
    {
        // Smooth progress bar fill
        if (progressBarFill != null)
        {
            displayFill = Mathf.MoveTowards(displayFill, targetFill, 2.5f * Time.deltaTime);
            progressBarFill.fillAmount = displayFill;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleScoreChanged(int score)
    {
        currentScore = score;

        // Check for newly completed bets
        for (int i = completedBets; i < BetCount; i++)
        {
            if (score >= BetTargets[i])
            {
                completedBets = i + 1;
                StartCoroutine(BetCompleteFlashRoutine(i));
            }
        }

        RefreshAllSlots();
        RecalculateProgressBar();
    }

    private void HandleGameOver()
    {
        // Freeze the display as-is on game over
    }

    // ── Slot refresh ──────────────────────────────────────────────────────────

    /// <summary>Redraws all three bet slots to match current state.</summary>
    private void RefreshAllSlots()
    {
        for (int i = 0; i < BetCount; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int i)
    {
        bool completed = i < completedBets;
        bool active    = !completed && (i == completedBets);

        // Background color
        if (betBackgrounds != null && i < betBackgrounds.Length && betBackgrounds[i] != null)
        {
            betBackgrounds[i].color = completed
                ? ColorCompleted
                : active ? ColorActive : ColorLocked;
        }

        // Label text  "score / target"
        if (betLabels != null && i < betLabels.Length && betLabels[i] != null)
        {
            int displayed = Mathf.Min(currentScore, BetTargets[i]);
            betLabels[i].text  = $"{displayed}/{BetTargets[i]}";
            betLabels[i].color = completed ? ColorTextCompleted : ColorTextNormal;
        }
    }

    // ── Progress bar ──────────────────────────────────────────────────────────

    /// <summary>
    /// Progress bar fills from 0 to 1 within the range of the current active bet.
    /// Once all bets are done it stays full.
    /// </summary>
    private void RecalculateProgressBar()
    {
        if (completedBets >= BetCount)
        {
            UpdateProgressBar(1f);
            return;
        }

        int activeBetTarget = BetTargets[completedBets];
        int previousTarget  = completedBets > 0 ? BetTargets[completedBets - 1] : 0;

        float range    = activeBetTarget - previousTarget;
        float progress = currentScore - previousTarget;
        float normalized = range > 0f ? Mathf.Clamp01(progress / range) : 0f;

        // Account for already-completed bets in the overall bar
        float overallNormalized = (completedBets + normalized) / BetCount;
        UpdateProgressBar(overallNormalized);
    }

    private void UpdateProgressBar(float normalized, bool snap = false)
    {
        targetFill = Mathf.Clamp01(normalized);
        if (snap)
        {
            displayFill = targetFill;
            if (progressBarFill != null)
                progressBarFill.fillAmount = displayFill;
        }
    }

    // ── Bet complete flash ────────────────────────────────────────────────────

    private IEnumerator BetCompleteFlashRoutine(int slotIndex)
    {
        if (betBackgrounds == null || slotIndex >= betBackgrounds.Length) yield break;
        Image bg = betBackgrounds[slotIndex];
        if (bg == null) yield break;

        float elapsed  = 0f;
        float duration = 0.40f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t     = elapsed / duration;
            // Overshoot scale pop
            float scale = t < 0.5f
                ? Mathf.Lerp(1f, 1.25f, t / 0.5f)
                : Mathf.Lerp(1.25f, 1f, (t - 0.5f) / 0.5f);
            bg.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        bg.transform.localScale = Vector3.one;
    }
}
