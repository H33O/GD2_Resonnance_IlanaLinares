using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Top HUD: score display with pop animation, combo counter, and XP bar.
/// Attach to the "HUD" Canvas GameObject.
/// </summary>
public class SGHUD : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Score")]
    public TextMeshProUGUI scoreText;

    [Header("Combo")]
    public TextMeshProUGUI comboText;
    public float           comboPeakScale  = 1.4f;
    public float           comboPopDuration = 0.18f;

    [Header("XP Bar")]
    public Image xpBarFill;

    [Header("Fury")]
    public TextMeshProUGUI furyLabel;

    // ── Internal ──────────────────────────────────────────────────────────────

    private float   displayXp;
    private Coroutine comboPopCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        SGGameManager.OnScoreChanged  += HandleScoreChanged;
        SGGameManager.OnComboChanged  += HandleComboChanged;
        SGGameManager.OnXpChanged     += HandleXpChanged;
        SGGameManager.OnFuryStarted   += HandleFuryStarted;
        SGGameManager.OnFuryEnded     += HandleFuryEnded;
        SGGameManager.OnGameOver      += HandleGameOver;
    }

    private void OnDisable()
    {
        SGGameManager.OnScoreChanged  -= HandleScoreChanged;
        SGGameManager.OnComboChanged  -= HandleComboChanged;
        SGGameManager.OnXpChanged     -= HandleXpChanged;
        SGGameManager.OnFuryStarted   -= HandleFuryStarted;
        SGGameManager.OnFuryEnded     -= HandleFuryEnded;
        SGGameManager.OnGameOver      -= HandleGameOver;
    }

    private void Start()
    {
        if (scoreText != null) scoreText.text = "0";
        if (comboText != null) { comboText.text = ""; comboText.gameObject.SetActive(false); }
        if (furyLabel != null) furyLabel.gameObject.SetActive(false);
        if (xpBarFill != null) xpBarFill.fillAmount = 0f;
    }

    private void Update()
    {
        // Smooth XP bar
        if (xpBarFill != null)
        {
            float target = SGGameManager.Instance?.XpNormalized ?? 0f;
            displayXp    = Mathf.MoveTowards(displayXp, target, 1.5f * Time.deltaTime);
            xpBarFill.fillAmount = displayXp;
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandleScoreChanged(int score)
    {
        if (scoreText == null) return;
        scoreText.text = score.ToString();
        StartCoroutine(ScorePopRoutine());
    }

    private void HandleComboChanged(int combo)
    {
        if (comboText == null) return;
        if (combo <= 1)
        {
            comboText.gameObject.SetActive(false);
            return;
        }

        comboText.gameObject.SetActive(true);
        comboText.text = $"x{combo}";

        if (comboPopCoroutine != null) StopCoroutine(comboPopCoroutine);
        comboPopCoroutine = StartCoroutine(ComboPopRoutine());
    }

    private void HandleXpChanged(float normalized)
    {
        // XP bar is smoothed in Update — nothing to do here
    }

    private void HandleFuryStarted()
    {
        if (furyLabel != null)
        {
            furyLabel.gameObject.SetActive(true);
            StartCoroutine(FuryPulseRoutine());
        }
    }

    private void HandleFuryEnded()
    {
        if (furyLabel != null) furyLabel.gameObject.SetActive(false);
    }

    private void HandleGameOver()
    {
        if (comboText != null) comboText.gameObject.SetActive(false);
    }

    // ── Score pop ─────────────────────────────────────────────────────────────

    private IEnumerator ScorePopRoutine()
    {
        if (scoreText == null) yield break;
        Transform t    = scoreText.transform;
        float elapsed  = 0f;
        float half     = 0.10f;

        while (elapsed < half)
        {
            elapsed    += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed    += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, elapsed / half);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── Combo pop ─────────────────────────────────────────────────────────────

    private IEnumerator ComboPopRoutine()
    {
        if (comboText == null) yield break;
        Transform t    = comboText.transform;
        float elapsed  = 0f;
        float half     = comboPopDuration * 0.5f;

        while (elapsed < half)
        {
            elapsed    += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, comboPeakScale, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed    += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(comboPeakScale, 1f, elapsed / half);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── Fury pulse ────────────────────────────────────────────────────────────

    private IEnumerator FuryPulseRoutine()
    {
        if (furyLabel == null) yield break;
        float t = 0f;
        while (furyLabel.gameObject.activeSelf)
        {
            t += Time.deltaTime * 5f;
            float a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t));
            Color c = furyLabel.color;
            c.a             = a;
            furyLabel.color = c;
            yield return null;
        }
    }
}
