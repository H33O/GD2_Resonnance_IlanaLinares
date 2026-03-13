using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player input (tap/click) to trigger parries.
/// Owns the parry sword arc visual and notifies active slashes.
/// Guardian auto-parry logic runs here as well.
/// </summary>
public class SGPlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    [Header("References")]
    public SGSlashSpawner   slashSpawner;
    public SGFeedbackManager feedback;
    public SGSquadData      squadData;
    public Camera           gameCamera;

    // ── Internal state ────────────────────────────────────────────────────────

    private bool        parryWindowOpen;
    private Coroutine   parryWindowCoroutine;
    private SpriteRenderer playerSR;

    // Sword arc — LineRenderer drawn as a sweeping arc on parry
    private LineRenderer parryArcLR;
    private Coroutine    arcCoroutine;

    private const int    ArcSegments    = 24;
    private const float  PlayerScale    = 0.28f;  // world-unit radius of player circle

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildPlayerVisual();
        BuildParryArc();
    }

    private void Update()
    {
        HandleInput();
        TickGuardianAutoParry();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        var gm = SGGameManager.Instance;
        if (gm == null) return;
        if (gm.State == SGGameManager.GameState.Dead ||
            gm.State == SGGameManager.GameState.GameOver) return;

        bool tapped = false;

        // Touch
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

        // Mouse fallback (editor / PC)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;

        if (tapped)
            DoParry();
    }

    // ── Parry logic ───────────────────────────────────────────────────────────

    private void DoParry()
    {
        OpenParryWindow();

        // Try to parry every nearby slash
        if (slashSpawner == null) return;
        IReadOnlyList<SGSlash> slashes = slashSpawner.ActiveSlashes;

        float parryRadius = GetParryRadius();
        bool  hit         = false;

        for (int i = slashes.Count - 1; i >= 0; i--)
        {
            SGSlash s = slashes[i];
            if (s == null) continue;

            float dist = Vector3.Distance(s.transform.position, Vector3.zero);
            if (dist > parryRadius) continue;

            s.TryParry(Vector3.zero);
            hit = true;
        }

        // Trigger parry arc visual regardless (always responsive)
        ShowParryArc(hit);

        if (hit)
            feedback?.TriggerParry(Vector3.zero);
    }

    private void OpenParryWindow()
    {
        if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
        parryWindowCoroutine = StartCoroutine(ParryWindowRoutine());
    }

    private IEnumerator ParryWindowRoutine()
    {
        parryWindowOpen = true;
        float duration  = GetParryWindowDuration();
        yield return new WaitForSecondsRealtime(duration);
        parryWindowOpen = false;
    }

    private float GetParryRadius()
    {
        float r = settings != null ? settings.parryRadius : 1.2f;
        return r;
    }

    private float GetParryWindowDuration()
    {
        float w = settings != null ? settings.parryWindowDuration : 0.18f;
        if (squadData != null) w += squadData.SpeedParryBonus;
        return w;
    }

    // ── Guardian auto-parry ───────────────────────────────────────────────────

    private float guardianCooldown;
    private const float GuardianCooldownDuration = 4.0f;

    private void TickGuardianAutoParry()
    {
        if (squadData == null || !squadData.IsUnlocked(SGCharacterType.Guardian)) return;
        if (slashSpawner == null) return;
        if (guardianCooldown > 0f) { guardianCooldown -= Time.deltaTime; return; }

        IReadOnlyList<SGSlash> slashes = slashSpawner.ActiveSlashes;
        float chance = squadData.GuardianAutoParryChance;

        for (int i = slashes.Count - 1; i >= 0; i--)
        {
            SGSlash s = slashes[i];
            if (s == null) continue;

            // Only intercept when very close
            float dist = Vector3.Distance(s.transform.position, Vector3.zero);
            if (dist > settings.parryRadius * 1.5f) continue;

            if (Random.value < chance)
            {
                s.TryParry(Vector3.zero);
                feedback?.TriggerGuardianShield(Vector3.zero);
                SGGameManager.Instance?.NotifyAutoParry(Vector3.zero);
                guardianCooldown = GuardianCooldownDuration;
                break;
            }
        }
    }

    // ── Player visual ─────────────────────────────────────────────────────────

    private void BuildPlayerVisual()
    {
        // Main black circle
        playerSR          = gameObject.AddComponent<SpriteRenderer>();
        playerSR.sprite   = SpriteGenerator.CreateCircle(128);
        playerSR.color    = Color.black;
        playerSR.sortingOrder = 5;
        transform.localScale  = Vector3.one * PlayerScale * 2f;

        // White ring outline
        var ringGO   = new GameObject("PlayerRing");
        ringGO.transform.SetParent(transform, false);
        ringGO.transform.localScale = Vector3.one;
        var ringSR   = ringGO.AddComponent<SpriteRenderer>();
        ringSR.sprite       = SpriteGenerator.CreateRing(128, 0.06f);
        ringSR.color        = Color.white;
        ringSR.sortingOrder = 6;

        // Glow
        var glowGO = new GameObject("PlayerGlow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale = Vector3.one * 1.4f;
        var glowSR = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(128);
        glowSR.color        = new Color(1f, 1f, 1f, 0.07f);
        glowSR.sortingOrder = 4;

        gameObject.AddComponent<SGPlayerGlowPulse>().glowSR = glowSR;
    }

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

    // ── Sword arc visual ──────────────────────────────────────────────────────

    private void ShowParryArc(bool successfulHit)
    {
        if (arcCoroutine != null) StopCoroutine(arcCoroutine);
        arcCoroutine = StartCoroutine(ParryArcRoutine(successfulHit));
    }

    private IEnumerator ParryArcRoutine(bool hit)
    {
        float arcRadius = GetParryRadius() * 0.85f;
        float arcAngle  = hit ? 340f : 180f;
        float startAngle = Random.Range(0f, 360f);

        // Draw positions
        for (int i = 0; i <= ArcSegments; i++)
        {
            float a   = (startAngle + i / (float)ArcSegments * arcAngle) * Mathf.Deg2Rad;
            parryArcLR.SetPosition(i, new Vector3(Mathf.Cos(a) * arcRadius,
                                                   Mathf.Sin(a) * arcRadius, 0f));
        }

        // Animate fade in → out
        float elapsed  = 0f;
        float duration = hit ? 0.30f : 0.18f;
        float peakAlpha = hit ? 0.90f : 0.45f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;

            // Smooth fade: fast rise, slow fall
            float a = t < 0.2f
                ? Mathf.Lerp(0f, peakAlpha, t / 0.2f)
                : Mathf.Lerp(peakAlpha, 0f, (t - 0.2f) / 0.8f);

            Color c = new Color(1f, 1f, 1f, a);
            parryArcLR.startColor = c;
            parryArcLR.endColor   = c;

            // Expand radius slightly on hit for juice
            if (hit)
            {
                float r = arcRadius * (1f + t * 0.3f);
                for (int i = 0; i <= ArcSegments; i++)
                {
                    float ang = (startAngle + i / (float)ArcSegments * arcAngle) * Mathf.Deg2Rad;
                    parryArcLR.SetPosition(i, new Vector3(Mathf.Cos(ang) * r,
                                                           Mathf.Sin(ang) * r, 0f));
                }
            }

            yield return null;
        }

        parryArcLR.startColor = Color.clear;
        parryArcLR.endColor   = Color.clear;
    }
}

/// <summary>Tiny MonoBehaviour that pulses the player glow alpha each frame.</summary>
public class SGPlayerGlowPulse : MonoBehaviour
{
    public SpriteRenderer glowSR;

    private void Update()
    {
        if (glowSR == null) return;
        Color c = glowSR.color;
        c.a          = 0.07f + 0.05f * Mathf.Sin(Time.time * 3.0f);
        glowSR.color = c;
    }
}
