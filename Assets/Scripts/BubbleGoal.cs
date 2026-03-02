using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Visual goal bar at the top of the visible camera area.
/// When a projectile reaches it, the player wins.
/// </summary>
public class BubbleGoal : MonoBehaviour
{
    public static BubbleGoal Instance { get; private set; }

    private float halfWidth;

    private void Awake() => Instance = this;

    /// <summary>Initialises the goal at the top of the visible screen.</summary>
    public void Init(Camera cam)
    {
        halfWidth = cam.orthographicSize * cam.aspect;

        // Position: very top of the visible camera area
        float goalY = cam.orthographicSize - 0.2f;
        transform.position = new Vector3(0f, goalY, 0f);

        BuildVisual(halfWidth * 2f);
        StartCoroutine(PulseRoutine());
    }

    /// <summary>Returns true if the given world position is inside the goal zone.</summary>
    public bool Contains(Vector2 worldPos)
        => worldPos.y >= transform.position.y - 0.3f
        && Mathf.Abs(worldPos.x) <= halfWidth;

    // ── Visual ───────────────────────────────────────────────────────────────

    private void BuildVisual(float totalWidth)
    {
        // Golden bar spanning the full screen width
        var bar = new GameObject("GoalBar");
        bar.transform.SetParent(transform);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localScale    = new Vector3(totalWidth, 0.18f, 1f);
        var barSR        = bar.AddComponent<SpriteRenderer>();
        barSR.sprite     = BuildRectSprite();
        barSR.color      = new Color(1f, 0.88f, 0.12f, 1f);
        barSR.sortingOrder = 12;

        // Three glowing circles: left, centre, right
        SpawnCircle(new Vector3(-totalWidth * 0.45f, 0f, 0f));
        SpawnCircle(new Vector3(0f,                  0f, 0f));
        SpawnCircle(new Vector3( totalWidth * 0.45f, 0f, 0f));

        // "GOAL" world-space text above the bar
        var labelGO = new GameObject("GoalLabel");
        labelGO.transform.SetParent(transform);
        labelGO.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        var tmp        = labelGO.AddComponent<TextMeshPro>();
        tmp.text       = "GOAL";
        tmp.fontSize   = 0.7f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.color      = new Color(1f, 0.92f, 0.15f, 1f);
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.sortingOrder = 13;
    }

    private void SpawnCircle(Vector3 localPos)
    {
        var go  = new GameObject("GoalCircle");
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * 0.35f;
        var sr           = go.AddComponent<SpriteRenderer>();
        sr.sprite        = SpriteGenerator.Circle();
        sr.color         = new Color(1f, 0.95f, 0.2f, 1f);
        sr.sortingOrder  = 13;
    }

    private static Sprite BuildRectSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float s = 1f + 0.05f * Mathf.Sin(Time.time * 3f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
    }
}
