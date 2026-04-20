using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Met à jour le HUD du jeu Tetris×Pac-Man :
/// — "SCORE: X"   à gauche du bandeau supérieur
/// — "COUPS: X"   à droite avec icône dorée
/// — Aperçu bloc  "NEXT" (mini-grille 4×2 en haut)
/// </summary>
public class TPMHUD : MonoBehaviour
{
    // ── Références ────────────────────────────────────────────────────────────

    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI movesLabel;

    // Next piece preview
    private readonly List<Image> nextCells = new();
    private RectTransform nextPieceGrid;
    private TPMSettings   settings;

    private static readonly Color EmptyCellColor = new Color(0.12f, 0.12f, 0.18f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        TPMGameManager.OnScoreChanged   += OnScoreChanged;
        TPMGameManager.OnMovesChanged   += OnMovesChanged;
        TPMTetrisSpawner.OnNextPieceChanged += OnNextPieceChanged;
    }

    private void OnDisable()
    {
        TPMGameManager.OnScoreChanged   -= OnScoreChanged;
        TPMGameManager.OnMovesChanged   -= OnMovesChanged;
        TPMTetrisSpawner.OnNextPieceChanged -= OnNextPieceChanged;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé par TPMSceneSetup ou TPMSceneBuilder après construction du canvas.</summary>
    public void Init(TextMeshProUGUI score, TextMeshProUGUI moves, int startingMoves,
                     RectTransform nextGrid, TPMSettings cfg)
    {
        scoreLabel   = score;
        movesLabel   = moves;
        nextPieceGrid = nextGrid;
        settings     = cfg;

        if (scoreLabel != null) scoreLabel.text = "SCORE: 0";
        if (movesLabel != null) movesLabel.text = $"COUPS: {startingMoves}";

        BuildNextCells();
    }

    /// <summary>Surcharge sans aperçu next (rétrocompatibilité).</summary>
    public void Init(TextMeshProUGUI score, TextMeshProUGUI moves, int startingMoves)
    {
        Init(score, moves, startingMoves, null, null);
    }

    // ── Construction grille NEXT ──────────────────────────────────────────────

    private void BuildNextCells()
    {
        if (nextPieceGrid == null) return;

        // Grille 4 colonnes × 2 lignes de petites cellules
        for (int i = 0; i < 8; i++)
        {
            var cellGO  = new GameObject($"NC_{i}");
            cellGO.transform.SetParent(nextPieceGrid, false);
            var cellImg = cellGO.AddComponent<Image>();
            cellImg.color = EmptyCellColor;
            cellImg.sprite = SpriteGenerator.CreateColoredSquare(EmptyCellColor);
            nextCells.Add(cellImg);
        }

        var glg = nextPieceGrid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = nextPieceGrid.gameObject.AddComponent<GridLayoutGroup>();
        glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount  = 4;
        glg.cellSize         = new Vector2(22f, 22f);
        glg.spacing          = new Vector2(3f, 3f);
        glg.startCorner      = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis        = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment   = TextAnchor.MiddleCenter;
    }

    // ── Mises à jour ─────────────────────────────────────────────────────────

    private void OnScoreChanged(int score)
    {
        if (scoreLabel != null) scoreLabel.text = $"SCORE: {score}";
    }

    private void OnMovesChanged(int moves)
    {
        if (movesLabel != null) movesLabel.text = $"COUPS: {moves}";
    }

    private void OnNextPieceChanged(int shapeIndex)
    {
        if (nextCells.Count < 8) return;

        // Réinitialise tout en vide
        foreach (var c in nextCells)
        {
            c.color  = EmptyCellColor;
            c.sprite = SpriteGenerator.CreateColoredSquare(EmptyCellColor);
        }

        // Remplit les cellules occupées (offset +1,+0 pour centrer sur grille 4×2)
        Color pieceColor = TPMTetrisPiece.Colors[shapeIndex];
        foreach (var offset in TPMTetrisPiece.Shapes[shapeIndex])
        {
            // On mappe offset.x ∈ [-1..2] vers colonne 0..3, offset.y ∈ [0..1] vers ligne 0..1
            int col = offset.x + 1;
            int row = 1 - offset.y; // y=0 → ligne 1 (bas), y=1 → ligne 0 (haut)
            int idx = row * 4 + col;
            if (idx >= 0 && idx < nextCells.Count)
            {
                nextCells[idx].color  = pieceColor;
                nextCells[idx].sprite = SpriteGenerator.CreateColoredSquare(pieceColor);
            }
        }
    }
}
