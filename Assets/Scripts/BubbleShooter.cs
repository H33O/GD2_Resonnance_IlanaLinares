using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Canon en bas de l'écran. Vise avec la souris ou le toucher, tire au tap ou clic.
/// Affiche la ligne de visée et la prochaine bulle.
/// </summary>
public class BubbleShooter : MonoBehaviour
{
    private BubbleColor current, next;
    private SpriteRenderer currentSR, nextSR;
    private LineRenderer aimLine;
    private Camera cam;

    private Vector2 lastInputPosition;
    private static readonly float MinAngleDeg = 8f;

    private void Start()
    {
        cam = Camera.main;
        lastInputPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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

        var curGO = MakeCircle("Current", transform.position, d * 0.9f, 3);
        curGO.transform.SetParent(transform);
        currentSR = curGO.GetComponent<SpriteRenderer>();

        var nxtGO = MakeCircle("Next", transform.position + new Vector3(-2f, 0f, 0f), d * 0.6f, 3);
        nxtGO.transform.SetParent(transform);
        nextSR = nxtGO.GetComponent<SpriteRenderer>();

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

        bool shoot = false;

        // Touch (mobile)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    lastInputPosition = touch.position.ReadValue();
                }
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    shoot = true;
                }
            }
        }
        else
        {
            // Souris / clavier — éditeur
            lastInputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                shoot = true;
        }

        UpdateAimLine();
        if (shoot) Shoot();
    }

    /// <summary>Calcule la direction de visée depuis la dernière position d'entrée.</summary>
    private Vector2 AimDir()
    {
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(lastInputPosition.x, lastInputPosition.y, 10f));
        Vector2 dir = ((Vector2)world - (Vector2)transform.position).normalized;
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

        current = next;
        next = BubbleColorExtensions.Random(BubbleGrid.Instance.ColorCount);
        DrawNextBubbles();
    }
}
