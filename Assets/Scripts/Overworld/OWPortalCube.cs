using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Portail dans l'overworld.
/// Sprite assigné librement dans l'Inspector — déplaçable et modifiable en éditeur.
/// Le joueur (tag "Player") entre en trigger → transition vers la scène cible.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class OWPortalCube : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Scène cible")]
    [SerializeField] public string targetScene;

    [Header("Label affiché au-dessus")]
    [SerializeField] public string label       = "Mini-Jeu";
    [SerializeField] public float  labelOffset = 0.9f;
    [SerializeField] public float  labelScale  = 0.008f;
    [SerializeField] public int    labelFontSize = 28;

    [Header("Couleurs d'interaction")]
    [SerializeField] public Color  idleColor    = Color.white;
    [SerializeField] public Color  hoverColor   = new Color(0.4f, 1f, 0.9f, 1f);
    [SerializeField] public Color  enterColor   = Color.yellow;

    // ── Privés ────────────────────────────────────────────────────────────────

    private SpriteRenderer sr;
    private bool           transitioning = false;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        sr       = GetComponent<SpriteRenderer>();
        sr.color = idleColor;

        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;
    }

    private void Start()
    {
        // Ne reconstruit pas si un enfant "Label" existe déjà (ex : duplicata en éditeur)
        if (transform.Find("Label") == null)
            BuildLabel();
    }

    // ── Triggers ──────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transitioning || !other.CompareTag("Player")) return;
        transitioning = true;
        StartCoroutine(Enter());
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || transitioning) return;
        sr.color = hoverColor;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        sr.color      = idleColor;
        transitioning = false;
    }

    // ── Transition ────────────────────────────────────────────────────────────

    private IEnumerator Enter()
    {
        sr.color = enterColor;
        yield return new WaitForSeconds(0.12f);

        if (OWGameManager.Instance != null)
            OWGameManager.Instance.EnterMiniGame(targetScene, transform.position);
        else if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(targetScene, label);
        else
            SceneManager.LoadScene(targetScene);
    }

    // ── Label ─────────────────────────────────────────────────────────────────

    private void BuildLabel()
    {
        var canvasGO       = new GameObject("Label");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, labelOffset, 0f);
        canvasGO.transform.localScale    = Vector3.one * labelScale;

        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 60f);

        var textGO   = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);

        var tmp          = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = label;
        tmp.fontSize     = labelFontSize;
        tmp.color        = Color.white;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.alignment    = TextAlignmentOptions.Center;

        var r        = tmp.rectTransform;
        r.anchorMin  = Vector2.zero;
        r.anchorMax  = Vector2.one;
        r.offsetMin  = r.offsetMax = Vector2.zero;
    }

    // ── Gizmo éditeur ────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (transform.localScale.y * 0.5f + 0.3f),
            $"→ {targetScene}");
        #endif
    }
}
