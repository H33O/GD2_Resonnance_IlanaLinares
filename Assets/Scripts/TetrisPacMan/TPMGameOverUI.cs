using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Affiche l'écran de fin de partie (victoire ou défaite) du mini-jeu Tetris×Pac-Man.
/// </summary>
public class TPMGameOverUI : MonoBehaviour
{
    // ── Références ────────────────────────────────────────────────────────────

    private Canvas     overlayCanvas;
    private Image      background;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI scoreLabel;
    private Button     restartButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        TPMGameManager.OnVictory += ShowVictory;
        TPMGameManager.OnDefeat  += ShowDefeat;
    }

    private void OnDisable()
    {
        TPMGameManager.OnVictory -= ShowVictory;
        TPMGameManager.OnDefeat  -= ShowDefeat;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Reçoit les références UI construites par <see cref="TPMSceneSetup"/>.
    /// </summary>
    public void Init(Canvas canvas, Image bg, TextMeshProUGUI title,
                     TextMeshProUGUI score, Button restart)
    {
        overlayCanvas  = canvas;
        background     = bg;
        titleLabel     = title;
        scoreLabel     = score;
        restartButton  = restart;

        overlayCanvas.gameObject.SetActive(false);

        restartButton.onClick.AddListener(() =>
            TPMGameManager.Instance?.Restart());
    }

    // ── Affichage ─────────────────────────────────────────────────────────────

    private void ShowVictory()
    {
        Show("SORTIE ATTEINTE !", new Color(0.10f, 0.70f, 0.25f, 0.88f));
    }

    private void ShowDefeat()
    {
        Show("ÉLIMINÉ !", new Color(0.70f, 0.08f, 0.05f, 0.88f));
    }

    private void Show(string title, Color bgColor)
    {
        overlayCanvas.gameObject.SetActive(true);
        if (background  != null) background.color  = bgColor;
        if (titleLabel  != null) titleLabel.text   = title;
        if (scoreLabel  != null)
            scoreLabel.text = $"SCORE  {TPMGameManager.Instance?.Score ?? 0:D6}";

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (background == null) yield break;

        Color   target = background.color;
        float   elapsed = 0f;
        float   duration = 0.50f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;
            Color c  = target;
            c.a      = Mathf.Lerp(0f, target.a, t);
            if (background   != null) background.color   = c;
            if (titleLabel   != null) { Color tc = titleLabel.color;   tc.a = t; titleLabel.color   = tc; }
            if (scoreLabel   != null) { Color sc = scoreLabel.color;   sc.a = t; scoreLabel.color   = sc; }
            if (restartButton != null) { var img = restartButton.GetComponent<Image>(); if (img != null) { Color ic = img.color; ic.a = t; img.color = ic; } }
            yield return null;
        }
    }
}
