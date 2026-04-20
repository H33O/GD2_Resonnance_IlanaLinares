using System.Collections;
using UnityEngine;

/// <summary>
/// Ennemi — enfermé dans une cage au démarrage.
/// Libéré uniquement quand le joueur pose son premier bloc.
/// Suit le joueur via BFS case par case.
/// </summary>
public class TPMMonster : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    [Header("References")]
    public TPMPlayerController player;

    // ── État ──────────────────────────────────────────────────────────────────

    private Vector2Int cellPos;
    private bool       released;
    private bool       isMoving;

    // Visuels
    private SpriteRenderer bodySR;
    private SpriteRenderer glowSR;

    private static readonly Color CoreColor = new Color(1.00f, 0.18f, 0.10f, 1.00f);
    private static readonly Color GlowColor = new Color(1.00f, 0.08f, 0.05f, 0.28f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildVisuals();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<TPMPlayerController>();

        if (TPMGrid.Instance != null)
        {
            cellPos = TPMGrid.Instance.MonsterStart;
            transform.position = TPMGrid.Instance.CellToWorld(cellPos.x, cellPos.y);
        }

        // Écoute le premier bloc posé
        TPMGameManager.OnFirstBlockPlaced += OnReleased;
    }

    private void OnDestroy()
    {
        TPMGameManager.OnFirstBlockPlaced -= OnReleased;
    }

    private void OnReleased()
    {
        if (released) return;
        released = true;
        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        yield return new WaitForSeconds(settings.monsterReleaseDelay);
        StartCoroutine(StepRoutine());
    }

    // ── IA : BFS case par case ────────────────────────────────────────────────

    private IEnumerator StepRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(settings.monsterStepDelay);

            if (TPMGameManager.Instance?.State != TPMGameManager.GameState.Playing) yield break;
            if (player == null || isMoving) continue;

            Vector2Int? next = TPMGrid.Instance.FindNextStep(cellPos, player.CellPosition);
            if (next.HasValue)
                yield return StartCoroutine(SlideToCell(next.Value));
        }
    }

    private IEnumerator SlideToCell(Vector2Int target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        Vector3 end   = TPMGrid.Instance.CellToWorld(target.x, target.y);
        float   dur   = settings.monsterStepDelay * 0.55f;
        float   t     = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }

        transform.position = end;
        cellPos = target;
        isMoving = false;

        if (player != null && cellPos == player.CellPosition)
            TPMGameManager.Instance?.NotifyCaught();
    }

    private void Update()
    {
        AnimateGlow();
    }

    // ── Visuels ───────────────────────────────────────────────────────────────

    private void BuildVisuals()
    {
        // Corps rouge fantôme (hexagone)
        bodySR              = gameObject.AddComponent<SpriteRenderer>();
        bodySR.sprite       = SpriteGenerator.CreatePolygon(6, 64);
        bodySR.color        = CoreColor;
        bodySR.sortingOrder = 12;
        transform.localScale = Vector3.one * 0.70f;

        var glowGO    = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale = Vector3.one * 1.7f;
        glowSR        = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = SpriteGenerator.CreateCircle(64);
        glowSR.color  = GlowColor;
        glowSR.sortingOrder = 11;

        StartCoroutine(SpinBody());
    }

    private IEnumerator SpinBody()
    {
        while (true) { transform.Rotate(0f, 0f, 38f * Time.deltaTime); yield return null; }
    }

    private void AnimateGlow()
    {
        if (glowSR == null) return;
        float a = released
            ? 0.20f + 0.18f * Mathf.Sin(Time.time * 7f)
            : 0.08f + 0.06f * Mathf.Sin(Time.time * 2f);
        glowSR.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, a);
    }
}
