using System.Collections;
using UnityEngine;

/// <summary>
/// Monstre du niveau 1 : suit le joueur via BFS, ne traverse pas les blocs,
/// se déplace case par case à intervalle régulier.
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
    private bool       isMoving;
    private bool       started;

    // Visuels
    private SpriteRenderer bodySR;
    private SpriteRenderer glowSR;

    private static readonly Color MonsterCore  = new Color(1.00f, 0.22f, 0.12f, 1.00f);
    private static readonly Color MonsterGlow  = new Color(1.00f, 0.10f, 0.05f, 0.25f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildVisuals();
    }

    private void Start()
    {
        if (TPMGrid.Instance == null) return;

        // Départ du monstre : coin bas-droit (diagonalement opposé au joueur en bas-gauche,
        // et loin de la sortie qui est en haut-droit)
        cellPos = new Vector2Int(settings.gridWidth - 2, 1);
        transform.position = TPMGrid.Instance.CellToWorld(cellPos.x, cellPos.y);

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(settings.monsterStartDelay);
        started = true;
        StartCoroutine(StepRoutine());
    }

    private IEnumerator StepRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(settings.monsterStepDelay);

            if (TPMGameManager.Instance?.State != TPMGameManager.GameState.Playing) yield break;
            if (player == null) continue;

            Vector2Int playerCell = player.CellPosition;
            Vector2Int? nextStep  = TPMGrid.Instance.FindNextStep(cellPos, playerCell);

            if (nextStep.HasValue)
                yield return StartCoroutine(SlideToCell(nextStep.Value));
        }
    }

    private IEnumerator SlideToCell(Vector2Int target)
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 endPos   = TPMGrid.Instance.CellToWorld(target.x, target.y);

        float duration = settings.monsterStepDelay * 0.6f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        cellPos = target;
        isMoving = false;

        CheckCatch();
    }

    private void CheckCatch()
    {
        if (player == null) return;
        if (cellPos == player.CellPosition)
            TPMGameManager.Instance?.NotifyCaught();
    }

    private void Update()
    {
        AnimateGlow();
    }

    // ── Visuels ───────────────────────────────────────────────────────────────

    private void BuildVisuals()
    {
        // Corps rouge menaçant
        bodySR           = gameObject.AddComponent<SpriteRenderer>();
        bodySR.sprite    = SpriteGenerator.CreatePolygon(6, 64); // hexagone
        bodySR.color     = MonsterCore;
        bodySR.sortingOrder = 10;
        transform.localScale = Vector3.one * 0.72f;

        // Halo de danger
        var glowGO       = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale = Vector3.one * 1.7f;
        glowSR           = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite    = SpriteGenerator.CreateCircle(64);
        glowSR.color     = MonsterGlow;
        glowSR.sortingOrder = 9;

        // Rotation permanente (élan menaçant)
        StartCoroutine(RotateBody());
    }

    private IEnumerator RotateBody()
    {
        while (true)
        {
            transform.Rotate(0f, 0f, 40f * Time.deltaTime);
            yield return null;
        }
    }

    private void AnimateGlow()
    {
        if (glowSR == null) return;
        float a      = 0.20f + 0.15f * Mathf.Sin(Time.time * 6f);
        glowSR.color = new Color(MonsterGlow.r, MonsterGlow.g, MonsterGlow.b, a);
    }
}
