using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur de la boule blanche dans l'overworld.
/// Déplacement top-down sur X et Y, confiné dans une zone rectangulaire.
/// Clic gauche (ou tap droit mobile) : place un cube blanc à la position du clic.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class OWBallController : MonoBehaviour
{
    // ── Mouvement ─────────────────────────────────────────────────────────────

    [Header("Mouvement")]
    [SerializeField] private float moveSpeed = 5f;

    // ── Zone de confinement ───────────────────────────────────────────────────

    [Header("Zone de confinement")]
    [SerializeField] private bool    useBounds   = true;
    [SerializeField] private Vector2 boundsMin   = new Vector2(-8f, -5f);
    [SerializeField] private Vector2 boundsMax   = new Vector2( 8f,  5f);
    [SerializeField] private Color   boundsColor = new Color(0.3f, 0.8f, 1f, 0.5f);

    // ── Cube placé par le joueur ──────────────────────────────────────────────

    [Header("Cube placé par le joueur")]
    [SerializeField] private float  cubeSize  = 0.8f;
    [SerializeField] private Color  cubeColor = Color.white;
    [SerializeField] private Sprite cubeSprite;          // assignable dans l'Inspector

    // ── Privés ────────────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Camera      mainCam;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 8f;
    }

    // ── Boucle principale ─────────────────────────────────────────────────────

    private void Update()
    {
        HandlePlaceCube();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        if (useBounds) ClampToBounds();
    }

    // ── Mouvement ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        var dir = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    dir.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  dir.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  dir.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dir.x += 1f;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    if (touch.position.ReadValue().x < Screen.width * 0.5f)
                        dir += touch.delta.ReadValue().normalized;
                }
            }
        }

        if (dir.sqrMagnitude > 1f) dir.Normalize();
        rb.linearVelocity = dir * moveSpeed;
    }

    // ── Confinement ───────────────────────────────────────────────────────────

    private void ClampToBounds()
    {
        var   col = GetComponent<CircleCollider2D>();
        float r   = col != null ? col.radius * transform.localScale.x : 0f;

        var pos   = rb.position;
        pos.x     = Mathf.Clamp(pos.x, boundsMin.x + r, boundsMax.x - r);
        pos.y     = Mathf.Clamp(pos.y, boundsMin.y + r, boundsMax.y - r);
        rb.position = pos;

        var vel = rb.linearVelocity;
        if (pos.x <= boundsMin.x + r || pos.x >= boundsMax.x - r) vel.x = 0f;
        if (pos.y <= boundsMin.y + r || pos.y >= boundsMax.y - r) vel.y = 0f;
        rb.linearVelocity = vel;
    }

    // ── Placement de cube ─────────────────────────────────────────────────────

    private void HandlePlaceCube()
    {
        bool    place    = false;
        Vector2 worldPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            place    = true;
            worldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        if (!place && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began &&
                    touch.position.ReadValue().x > Screen.width * 0.5f)
                {
                    place    = true;
                    worldPos = mainCam.ScreenToWorldPoint(
                        new Vector3(touch.position.ReadValue().x, touch.position.ReadValue().y, 0f));
                    break;
                }
            }
        }

        if (place) SpawnCube(worldPos);
    }

    private void SpawnCube(Vector2 position)
    {
        var go              = new GameObject("PlayerCube");
        go.transform.position   = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = Vector3.one * cubeSize;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = cubeSprite != null ? cubeSprite : MakeFallbackSprite();
        sr.color        = cubeColor;
        sr.sortingOrder = 2;

        var col       = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    // ── Fallback sprite runtime ───────────────────────────────────────────────

    private static Sprite MakeFallbackSprite()
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var pix = new Color[16];
        for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
        tex.SetPixels(pix); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!useBounds) return;

        var center = new Vector3((boundsMin.x + boundsMax.x) * 0.5f,
                                 (boundsMin.y + boundsMax.y) * 0.5f, 0f);
        var size   = new Vector3(boundsMax.x - boundsMin.x,
                                 boundsMax.y - boundsMin.y, 0f);

        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(boundsColor.r, boundsColor.g, boundsColor.b, 0.06f);
        Gizmos.DrawCube(center, size);
    }
}
