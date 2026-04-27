using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player input for the Parry Game.
/// A tap/click opens a parry window; any enemy within range is parried.
/// Displays a sword-arc visual feedback on parry.
/// </summary>
public class PGPlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    [Header("References")]
    public PGEnemySpawner enemySpawner;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const float ParryArcRadius    = 0.7f;
    private const float ParryArcDuration  = 0.28f;
    private const int   ArcSegments       = 20;

    // ── Internal state ────────────────────────────────────────────────────────

    private bool      parryWindowOpen;
    private Coroutine parryWindowCoroutine;
    private LineRenderer parryArcLR;
    private Coroutine    arcCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildParryArc();
    }

    private void Update()
    {
        HandleInput();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        var gm = PGGameManager.Instance;
        if (gm == null) return;
        if (gm.State != PGGameManager.GameState.Playing) return;

        bool tapped = false;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    tapped = true;
                    break;
                }
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;

        if (tapped)
            DoParry();
    }

    // ── Parry ─────────────────────────────────────────────────────────────────

    private void DoParry()
    {
        OpenParryWindow();

        if (enemySpawner == null) return;

        float triggerZ = settings != null ? settings.parryTriggerZ : 1.2f;
        bool  hit      = false;

        var enemies = enemySpawner.ActiveEnemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (!e.IsParryable(triggerZ)) continue;

            e.TriggerParry();
            hit = true;
        }

        ShowParryArc(hit);
    }

    private void OpenParryWindow()
    {
        if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
        parryWindowCoroutine = StartCoroutine(ParryWindowRoutine());
    }

    private IEnumerator ParryWindowRoutine()
    {
        parryWindowOpen = true;
        float duration  = settings != null ? settings.parryWindowDuration : 0.22f;
        yield return new WaitForSecondsRealtime(duration);
        parryWindowOpen = false;
    }

    // ── Sword arc visual ──────────────────────────────────────────────────────

    private void BuildParryArc()
    {
        var go   = new GameObject("ParryArc");
        go.transform.SetParent(transform.parent, false);
        parryArcLR              = go.AddComponent<LineRenderer>();
        parryArcLR.positionCount = ArcSegments + 1;
        parryArcLR.useWorldSpace = true;
        parryArcLR.loop          = false;
        parryArcLR.startWidth    = 0.06f;
        parryArcLR.endWidth      = 0.06f;
        parryArcLR.startColor    = Color.clear;
        parryArcLR.endColor      = Color.clear;
        parryArcLR.sortingOrder  = 15;
        parryArcLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        parryArcLR.receiveShadows    = false;
        parryArcLR.numCapVertices    = 4;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        parryArcLR.sharedMaterial = mat;
    }

    private void ShowParryArc(bool hit)
    {
        if (arcCoroutine != null) StopCoroutine(arcCoroutine);
        arcCoroutine = StartCoroutine(ParryArcRoutine(hit));
    }

    private IEnumerator ParryArcRoutine(bool hit)
    {
        float arcAngle  = hit ? 340f : 180f;
        float startAngle = Random.Range(0f, 360f);

        // Origin at player screen-center (the weapon holder position)
        Vector3 origin = transform.position;

        for (int i = 0; i <= ArcSegments; i++)
        {
            float a = (startAngle + i / (float)ArcSegments * arcAngle) * Mathf.Deg2Rad;
            parryArcLR.SetPosition(i, origin + new Vector3(Mathf.Cos(a) * ParryArcRadius,
                                                            Mathf.Sin(a) * ParryArcRadius, 0f));
        }

        float elapsed   = 0f;
        float duration  = hit ? 0.30f : 0.18f;
        float peakAlpha = hit ? 0.90f : 0.45f;
        Color hitColor  = new Color(1f, 0.9f, 0.2f, 1f);
        Color missColor = new Color(1f, 1f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float alpha = t < 0.2f
                ? Mathf.Lerp(0f, peakAlpha, t / 0.2f)
                : Mathf.Lerp(peakAlpha, 0f, (t - 0.2f) / 0.8f);

            Color baseColor  = hit ? hitColor : missColor;
            Color c          = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            parryArcLR.startColor = c;
            parryArcLR.endColor   = c;

            if (hit)
            {
                float r = ParryArcRadius * (1f + t * 0.3f);
                for (int i = 0; i <= ArcSegments; i++)
                {
                    float ang = (startAngle + i / (float)ArcSegments * arcAngle) * Mathf.Deg2Rad;
                    parryArcLR.SetPosition(i, origin + new Vector3(Mathf.Cos(ang) * r,
                                                                    Mathf.Sin(ang) * r, 0f));
                }
            }

            yield return null;
        }

        parryArcLR.startColor = Color.clear;
        parryArcLR.endColor   = Color.clear;
    }
}
