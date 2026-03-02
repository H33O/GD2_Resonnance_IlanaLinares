using UnityEngine;

/// <summary>
/// Canon en bas de l'écran.
/// Mobile : doigt posé = vise, doigt levé = tire UNE bulle.
/// Éditeur  : souris maintenue = vise, clic relâché = tire.
/// Une seule bulle en vol à la fois.
/// </summary>
public class BubbleShooter : MonoBehaviour
{
    private BubbleColor current, next;
    private SpriteRenderer currentSR, nextSR;
    private Transform currentBubbleTransform;
    private Vector3 baseCurrentScale;
    private LineRenderer aimLine;
    private Camera cam;

    private Vector2 lastInputPosition;
    private BubbleProjectile activeProjectile; // null = prêt à tirer

    private static readonly float MinAngleDeg = 8f;

    private void Start()
    {
        cam = Camera.main;
        lastInputPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        transform.position = new Vector3(0f, -cam.orthographicSize + 2.5f, 0f);

        SetupAimLine();
        CreateBubbleIndicators();
        DrawNextBubbles();
    }

    private void SetupAimLine()
    {
        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.positionCount  = 2;
        aimLine.startWidth     = 0.06f;
        aimLine.endWidth       = 0.02f;
        aimLine.startColor     = new Color(1f, 1f, 1f, 0.8f);
        aimLine.endColor       = new Color(1f, 1f, 1f, 0f);
        aimLine.useWorldSpace  = true;
        aimLine.sortingOrder   = 5;
        aimLine.enabled        = false;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        aimLine.material = mat;
    }

    private void CreateBubbleIndicators()
    {
        float d = BubbleGrid.Instance.Diameter;

        var curGO = MakeCircle("Current", transform.position, d * 0.75f, 3);
        curGO.transform.SetParent(transform);
        currentSR              = curGO.GetComponent<SpriteRenderer>();
        currentBubbleTransform = curGO.transform;
        baseCurrentScale       = curGO.transform.localScale;

        var nxtGO = MakeCircle("Next", transform.position + new Vector3(-1.2f, 0f, 0f), d * 0.5f, 3);
        nxtGO.transform.SetParent(transform);
        nextSR = nxtGO.GetComponent<SpriteRenderer>();

        current = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
        next    = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
    }

    private GameObject MakeCircle(string name, Vector3 pos, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.Circle();
        sr.sortingOrder = order;
        return go;
    }

    private void DrawNextBubbles()
    {
        ApplyColorToIndicator(currentSR, current);
        ApplyColorToIndicator(nextSR, next);
    }

    private void ApplyColorToIndicator(SpriteRenderer sr, BubbleColor color)
    {
        Sprite colorSprite = BubbleGrid.Instance?.GetSprite(color);
        if (colorSprite != null) { sr.sprite = colorSprite; sr.color = Color.white; }
        else                     { sr.sprite = SpriteGenerator.Circle(); sr.color = color.ToUnityColor(); }
    }

    private void Update()
    {
        if (BubbleGameManager.Instance != null && !BubbleGameManager.Instance.IsGameActive) return;

        // Pulse bulle courante
        if (currentBubbleTransform != null)
        {
            float pulse = 1f + 0.07f * Mathf.Sin(Time.time * 3.2f);
            currentBubbleTransform.localScale = baseCurrentScale * pulse;
        }

        bool fingerDown = false;
        bool shoot      = false;

        if (Input.touchCount > 0)
        {
            // ── Mobile : Input.GetTouch — universel et fiable ─────────────────
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began     ||
                t.phase == TouchPhase.Moved      ||
                t.phase == TouchPhase.Stationary)
            {
                lastInputPosition = t.position;
                fingerDown        = true;
            }

            // Tir uniquement au lever du doigt (Ended)
            if (t.phase == TouchPhase.Ended)
                shoot = true;
        }
        else
        {
            // ── Éditeur : souris ──────────────────────────────────────────────
            lastInputPosition = Input.mousePosition;
            fingerDown        = Input.GetMouseButton(0);

            // Cohérent avec le mobile : tir au relâcher
            if (Input.GetMouseButtonUp(0))
                shoot = true;
        }

        // Ligne de visée : visible uniquement quand le doigt est posé
        // et qu'aucun projectile n'est en vol
        bool canAim = fingerDown && activeProjectile == null;
        aimLine.enabled = canAim;
        if (canAim) UpdateAimLine();

        // Une bulle à la fois : on ne tire pas si un projectile est déjà en vol
        if (shoot && activeProjectile == null)
            Shoot();
    }

    private Vector2 AimDir()
    {
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(lastInputPosition.x, lastInputPosition.y, 10f));
        Vector2 dir = ((Vector2)world - (Vector2)transform.position).normalized;
        float minY  = Mathf.Sin(MinAngleDeg * Mathf.Deg2Rad);
        if (dir.y < minY) dir = new Vector2(dir.x, minY).normalized;
        return dir;
    }

    private void UpdateAimLine()
    {
        Vector2 dir = AimDir();
        aimLine.SetPosition(0, transform.position);
        aimLine.SetPosition(1, (Vector2)transform.position + dir * 12f);
    }

    private void Shoot()
    {
        if (BubbleGameManager.Instance != null && !BubbleGameManager.Instance.TryShoot()) return;

        var go = new GameObject("Projectile");
        go.transform.position = transform.position;
        go.AddComponent<SpriteRenderer>();

        var proj = go.AddComponent<BubbleProjectile>();
        proj.Init(current, AimDir(), BubbleGrid.Instance.Diameter);

        // Suivi du projectile actif → libéré par son callback OnLanded
        activeProjectile = proj;
        proj.OnLanded = () => activeProjectile = null;

        current = next;
        next    = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
        DrawNextBubbles();
    }
}

