using UnityEngine;

/// <summary>
/// Canon en bas de l'écran. Vise avec la souris, tire au clic gauche ou Espace.
/// Affiche la ligne de visée et la prochaine bulle.
/// </summary>
public class BubbleShooter : MonoBehaviour
{
    private BubbleColor current, next;
    private SpriteRenderer currentSR, nextSR;
    private LineRenderer aimLine;
    private Camera cam;

    private static readonly float MinAngleDeg = 8f; // angle min par rapport à l'horizontal

    private void Start()
    {
        cam = Camera.main;
        transform.position = new Vector3(0f, -cam.orthographicSize + 1.5f, 0f);

        SetupAimLine();
        CreateBubbleIndicators();
        DrawNextBubbles();
    }

    private void SetupAimLine()
    {
        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.positionCount = 2;
        aimLine.startWidth = 0.06f;
        aimLine.endWidth = 0.02f;
        aimLine.startColor = new Color(1f, 1f, 1f, 0.7f);
        aimLine.endColor   = new Color(1f, 1f, 1f, 0f);
        aimLine.useWorldSpace = true;
        aimLine.sortingOrder = 5;
    }

    private void CreateBubbleIndicators()
    {
        float d = BubbleGrid.Instance.Diameter;

        // Bulle courante (au centre du canon)
        var curGO = MakeCircle("Current", transform.position, d * 0.9f, 3);
        curGO.transform.SetParent(transform);
        currentSR = curGO.GetComponent<SpriteRenderer>();

        // Prochaine bulle (à gauche, plus petite)
        var nxtGO = MakeCircle("Next", transform.position + new Vector3(-2f, 0f, 0f), d * 0.6f, 3);
        nxtGO.transform.SetParent(transform);
        nextSR = nxtGO.GetComponent<SpriteRenderer>();

        // Génère les premières couleurs
        current = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
        next    = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
    }

    private GameObject MakeCircle(string name, Vector3 pos, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.Circle();
        sr.sortingOrder = order;
        return go;
    }

    private void DrawNextBubbles()
    {
        currentSR.color = current.ToUnityColor();
        nextSR.color    = next.ToUnityColor();
    }

    private void Update()
    {
        if (BubbleGameManager.Instance != null && !BubbleGameManager.Instance.IsGameActive) return;

        UpdateAimLine();

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            Shoot();
    }

    private Vector2 AimDir()
    {
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = ((Vector2)mouse - (Vector2)transform.position).normalized;
        float minY = Mathf.Sin(MinAngleDeg * Mathf.Deg2Rad);
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

        // Avancer à la prochaine bulle
        current = next;
        next = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
        DrawNextBubbles();
    }
}
