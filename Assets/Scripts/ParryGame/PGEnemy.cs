using System.Collections;
using UnityEngine;

/// <summary>
/// An enemy that charges toward the player from depth (high Z) to player Z.
/// When it reaches <see cref="PGSettings.parryTriggerZ"/> the player can parry it.
/// If not parried it continues to Z=0 and triggers a hit.
///
/// Visual warning: a ⚠ icon appears in world-space when the enemy enters the
/// danger zone (warningZ), giving the player advance notice.
/// </summary>
public class PGEnemy : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Z at which a missed enemy damages the player.</summary>
    private const float HitZ           = 0.5f;
    private const float DestroyBeyondZ = -2.0f;

    /// <summary>Z at which the warning icon appears (well before parry window).</summary>
    private const float WarningZ       = 5.5f;

    // ── State ─────────────────────────────────────────────────────────────────

    private float speed;
    private bool  parried;
    private bool  hitDealt;
    private bool  warningShown;

    private float spawnZ;
    private float targetZ;

    // Warning icon (⚠ Billboard sprite above the enemy)
    private GameObject  _warningGO;
    private SpriteRenderer _warningSR;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Call right after Instantiate to initialise the enemy.</summary>
    public void Init(float spawnZ, float playerZ, float moveSpeed)
    {
        this.spawnZ  = spawnZ;
        this.targetZ = playerZ;
        this.speed   = moveSpeed;

        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         spawnZ);
        RefreshScale();
        BuildWarningIcon();
    }

    private void Update()
    {
        if (parried) return;

        var gm = PGGameManager.Instance;
        if (gm != null && gm.State != PGGameManager.GameState.Playing) return;

        // Move toward player (decreasing Z)
        float currentSpeed = gm != null ? gm.CurrentEnemySpeed : speed;
        transform.position += Vector3.back * currentSpeed * Time.deltaTime;

        RefreshScale();
        UpdateWarning();

        // Damage player when reaching hit zone
        if (!hitDealt && transform.position.z <= HitZ)
        {
            hitDealt = true;

            bool blocked = PGAbilitySystem.Instance != null
                           && PGAbilitySystem.Instance.TryAbsorbWithShield();
            if (!blocked)
                gm?.NotifyHit();

            Destroy(gameObject);
        }

        if (transform.position.z <= DestroyBeyondZ)
            Destroy(gameObject);
    }

    // ── Parry ─────────────────────────────────────────────────────────────────

    /// <summary>Returns true if the enemy is close enough to be parried.</summary>
    public bool IsParryable(float parryTriggerZ)
    {
        return !parried && transform.position.z <= parryTriggerZ;
    }

    /// <summary>Triggers the parry — plays a quick visual and destroys the enemy.</summary>
    public void TriggerParry()
    {
        if (parried) return;
        parried = true;
        HideWarning();
        PGGameManager.Instance?.NotifyParry();
        StartCoroutine(ParryFlash());
    }

    private IEnumerator ParryFlash()
    {
        // Large scale burst + white flash
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float ratio = t / 0.15f;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 2.2f, ratio);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), ratio);

            var rend = GetComponent<Renderer>();
            if (rend != null && rend != sr as Renderer)
                rend.material.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), ratio);

            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Warning icon ──────────────────────────────────────────────────────────

    private void BuildWarningIcon()
    {
        _warningGO = new GameObject("Warning");
        _warningGO.transform.SetParent(transform, false);
        // Offset above the enemy
        _warningGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        _warningGO.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);

        _warningSR            = _warningGO.AddComponent<SpriteRenderer>();
        _warningSR.sprite     = SpriteGenerator.CreateWarningTriangle();
        _warningSR.color      = new Color(1f, 0.85f, 0f, 0f); // start invisible
        _warningSR.sortingOrder = 10;

        _warningGO.SetActive(false);
    }

    private void UpdateWarning()
    {
        if (warningShown || _warningGO == null) return;
        if (transform.position.z > WarningZ) return;

        warningShown = true;
        _warningGO.SetActive(true);
        StartCoroutine(PulseWarning());
    }

    private IEnumerator PulseWarning()
    {
        float elapsed = 0f;
        while (_warningGO != null && !parried && !hitDealt)
        {
            elapsed += Time.deltaTime;
            // Fast blink — alternates between full yellow and dim
            float blink = Mathf.Abs(Mathf.Sin(elapsed * 8f));
            if (_warningSR != null)
                _warningSR.color = new Color(1f, 0.85f, 0f, Mathf.Lerp(0.3f, 1f, blink));
            // Scale pulse
            float s = Mathf.Lerp(0.4f, 0.55f, blink);
            _warningGO.transform.localScale = new Vector3(s, s, s);
            yield return null;
        }
        HideWarning();
    }

    private void HideWarning()
    {
        if (_warningGO != null) _warningGO.SetActive(false);
    }

    // ── Depth scale ───────────────────────────────────────────────────────────

    private void RefreshScale()
    {
        float range = Mathf.Max(0.01f, spawnZ - targetZ);
        float t = Mathf.Clamp01(1f - (transform.position.z - targetZ) / range);

        float s = Mathf.Lerp(0.05f, 1.0f, t);
        transform.localScale = new Vector3(s, s, s);

        // Fade alpha based on distance
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a     = Mathf.Lerp(0.15f, 1f, t);
            sr.color = c;
            return;
        }

        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = Mathf.Lerp(0.15f, 1f, t);
            rend.material.color = c;
        }
    }
}
