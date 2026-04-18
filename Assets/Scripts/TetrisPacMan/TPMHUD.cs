using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construit et met à jour le HUD du jeu Tetris×Pac-Man :
/// score, coups restants, légende des contrôles.
/// </summary>
public class TPMHUD : MonoBehaviour
{
    // ── Références ────────────────────────────────────────────────────────────

    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI movesLabel;
    private Image           movesBarFill;

    private int startingMoves;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        TPMGameManager.OnScoreChanged += OnScoreChanged;
        TPMGameManager.OnMovesChanged += OnMovesChanged;
    }

    private void OnDisable()
    {
        TPMGameManager.OnScoreChanged -= OnScoreChanged;
        TPMGameManager.OnMovesChanged -= OnMovesChanged;
    }

    // ── Initialisation (appelée par TPMSceneSetup) ────────────────────────────

    /// <summary>
    /// Reçoit les références UI construites par <see cref="TPMSceneSetup"/>.
    /// </summary>
    public void Init(TextMeshProUGUI score, TextMeshProUGUI moves, Image movesFill, int maxMoves)
    {
        scoreLabel   = score;
        movesLabel   = moves;
        movesBarFill = movesFill;
        startingMoves = maxMoves;
    }

    // ── Mises à jour ─────────────────────────────────────────────────────────

    private void OnScoreChanged(int score)
    {
        if (scoreLabel != null)
            scoreLabel.text = $"SCORE  {score:D6}";
    }

    private void OnMovesChanged(int moves)
    {
        if (movesLabel != null)
            movesLabel.text = $"COUPS  {moves}";

        if (movesBarFill != null && startingMoves > 0)
        {
            movesBarFill.fillAmount = Mathf.Clamp01((float)moves / startingMoves);

            // Colorie la barre : vert → orange → rouge
            float t = movesBarFill.fillAmount;
            movesBarFill.color = t > 0.5f
                ? Color.Lerp(new Color(1f, 0.65f, 0f), new Color(0.15f, 0.85f, 0.25f), (t - 0.5f) * 2f)
                : Color.Lerp(new Color(1f, 0.15f, 0.05f), new Color(1f, 0.65f, 0f), t * 2f);
        }
    }
}
