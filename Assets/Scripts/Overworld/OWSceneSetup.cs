using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construit procéduralement la scène Overworld au runtime :
/// plateforme de départ, colonnes de plateformes, portails, porte verrouillée.
/// </summary>
public class OWSceneSetup : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Références de la scène")]
    [SerializeField] private Camera    mainCamera;
    [SerializeField] private Transform playerSpawn;

    [Header("Joueur")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Plateformes")]
    [SerializeField] private float platformWidth       = 3f;
    [SerializeField] private float platformHeight      = 0.3f;
    [SerializeField] private float verticalSpacing     = 3.5f;
    [SerializeField] private int   platformCount       = 18;
    [SerializeField] private float horizontalRange     = 3.5f;
    [SerializeField] private Color platformColor       = new Color(0.25f, 0.25f, 0.35f);

    [Header("Portails (1 scène par portail)")]
    [SerializeField] private List<string> miniGameScenes  = new List<string> { "GameAndWatch", "Minijeu-Bulles", "SlashGame", "CircleArena" };
    [SerializeField] private List<string> miniGameLabels  = new List<string> { "Sonantia", "Écho", "Slash", "Arène" };
    [SerializeField] private Color        portalColor     = new Color(0.2f, 0.8f, 1f, 0.9f);

    [Header("Porte verrouillée")]
    [SerializeField] private float lockedDoorY           = 8f;
    [SerializeField] private Color doorColor             = new Color(0.7f, 0.3f, 0.1f);

    [Header("Fond")]
    [SerializeField] private Color backgroundColor       = new Color(0.05f, 0.05f, 0.1f);

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LayerGround  = "Default";
    private const float  WorldLeft    = -5f;
    private const float  WorldRight   =  5f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Assure l'existence du OWGameManager persistant
        EnsureOWGameManager();

        // Fond de caméra
        if (mainCamera != null)
            mainCamera.backgroundColor = backgroundColor;

        // Construction de la map
        BuildFloor();
        BuildPlatforms();
        PlacePortals();
        PlaceLockedDoor();
        SpawnPlayer();
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildFloor()
    {
        // Plateforme de départ large
        CreatePlatform(0f, -5f, 10f);
        // Murs latéraux invisibles (colliders)
        CreateWall(WorldLeft  - 0.5f, 0f, 50f);
        CreateWall(WorldRight + 0.5f, 0f, 50f);
    }

    private void BuildPlatforms()
    {
        // Schéma : alternance gauche / centre / droite + quelques "îles" centrales
        float[] xPositions = { -horizontalRange, 0f, horizontalRange, -horizontalRange * 0.5f, horizontalRange * 0.5f };
        int     scheme     = 0;

        for (int i = 0; i < platformCount; i++)
        {
            float y = -2f + i * verticalSpacing;
            float x = xPositions[scheme % xPositions.Length];
            CreatePlatform(x, y, platformWidth);
            scheme++;
        }
    }

    private void PlacePortals()
    {
        // Place 4 portails à intervalles verticaux réguliers, sur les plateformes
        int count = Mathf.Min(miniGameScenes.Count, miniGameLabels.Count);
        for (int i = 0; i < count; i++)
        {
            float y     = -2f + (i * 4 + 2) * verticalSpacing;
            float x     = (i % 2 == 0) ? -horizontalRange : horizontalRange;

            // Assure une plateforme sous le portail
            CreatePlatform(x, y - verticalSpacing, platformWidth);

            string scene = i < miniGameScenes.Count ? miniGameScenes[i] : "";
            string label = i < miniGameLabels.Count  ? miniGameLabels[i]  : $"Mini-Jeu {i + 1}";
            BuildPortal(x, y, scene, label);
        }
    }

    private void PlaceLockedDoor()
    {
        // Porte sur le côté gauche, mi-hauteur de la map
        float x = WorldLeft + 1f;
        float y = lockedDoorY;

        BuildLockedDoor(x, y);
    }

    // ── Builders ──────────────────────────────────────────────────────────────

    private GameObject CreatePlatform(float x, float y, float width)
    {
        var go = new GameObject($"Platform_{x:F1}_{y:F1}");
        go.transform.position = new Vector3(x, y, 0f);

        var sr           = go.AddComponent<SpriteRenderer>();
        sr.sprite        = CreateRect(1f, 1f);
        sr.color         = platformColor;
        sr.sortingOrder  = 0;
        go.transform.localScale = new Vector3(width, platformHeight, 1f);

        var col          = go.AddComponent<BoxCollider2D>();
        col.size         = Vector2.one;

        return go;
    }

    private void CreateWall(float x, float y, float height)
    {
        var go            = new GameObject($"Wall_{x:F1}");
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(1f, height, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    private void BuildPortal(float x, float y, string scene, string label)
    {
        // --- Objet principal portail ---
        var portalGO        = new GameObject($"Portal_{label}");
        portalGO.transform.position = new Vector3(x, y + 0.8f, 0f);

        var sr              = portalGO.AddComponent<SpriteRenderer>();
        sr.sprite           = CreateCircle();
        sr.color            = portalColor;
        sr.sortingOrder     = 2;
        portalGO.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        var col             = portalGO.AddComponent<CircleCollider2D>();
        col.isTrigger       = true;
        col.radius          = 0.5f;

        var portal          = portalGO.AddComponent<OWPortal>();
        portal.Configure(scene, label);

        // Attacher le SpriteRenderer au composant (via champs sérialisés)
        SetPrivateField(portal, "portalRenderer", sr);
        SetPrivateField(portal, "idleColor", portalColor);
        SetPrivateField(portal, "activeColor", new Color(1f, 1f, 0.2f, 1f));

        // --- Label texte au-dessus ---
        var labelGO = BuildWorldText(label, portalGO.transform.position + Vector3.up * 1.0f, 14, Color.white);

        // --- Prompt d'interaction ---
        var prompt  = BuildWorldText("[E] Entrer", portalGO.transform.position + Vector3.up * 1.4f, 10, Color.yellow);
        prompt.SetActive(false);
        SetPrivateField(portal, "promptObject", prompt);
    }

    private void BuildLockedDoor(float x, float y)
    {
        // --- Corps de la porte ---
        var doorGO        = new GameObject("LockedDoor");
        doorGO.transform.position = new Vector3(x, y, 0f);

        var sr            = doorGO.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateRect(1f, 1f);
        sr.color          = doorColor;
        sr.sortingOrder   = 2;
        doorGO.transform.localScale = new Vector3(1.2f, 2.4f, 1f);

        // Collider bloquant (pas trigger)
        var physCol       = doorGO.AddComponent<BoxCollider2D>();
        physCol.size      = Vector2.one;

        // Collider trigger pour détecter l'approche
        var triggerGO     = new GameObject("DoorTrigger");
        triggerGO.transform.SetParent(doorGO.transform, false);
        var triggerCol    = triggerGO.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size   = new Vector2(3f, 3f);   // zone large

        var door          = triggerGO.AddComponent<OWLockedDoor>();

        // --- Widget UI (World Space Canvas) ---
        var canvasGO      = new GameObject("DoorHintCanvas");
        canvasGO.transform.SetParent(doorGO.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);

        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var canvasRect     = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(4f, 1.2f);
        canvasGO.transform.localScale = Vector3.one * 0.012f;

        // Fond du panel
        var panelGO       = new GameObject("HintPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg      = panelGO.AddComponent<Image>();
        panelImg.color    = new Color(0f, 0f, 0f, 0.75f);
        var panelRect     = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Texte
        var textGO        = new GameObject("HintText");
        textGO.transform.SetParent(panelGO.transform, false);
        var tmp           = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "Je pense qu'il faut une clé";
        tmp.fontSize      = 18f;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;

        var textRect      = tmp.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        panelGO.SetActive(false);
        door.SetHintPanel(panelGO, tmp);

        // Plateforme devant la porte
        CreatePlatform(x + 1.5f, y - 1.5f, 2.5f);
    }

    // ── Spawn joueur ──────────────────────────────────────────────────────────

    private void SpawnPlayer()
    {
        Vector3 spawnPos = playerSpawn != null ? playerSpawn.position : new Vector3(0f, -3.5f, 0f);

        // Si un OWGameManager a mémorisé une position de retour, on l'utilise
        if (OWGameManager.Instance != null && OWGameManager.Instance.HasReturnPosition)
            spawnPos = OWGameManager.Instance.PlayerReturnPosition + Vector3.up * 1f;

        GameObject player;
        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            player = BuildDefaultPlayer(spawnPos);
        }

        player.tag = "Player";

        // Assigner la caméra
        if (mainCamera != null)
        {
            var camFollow = mainCamera.GetComponent<OWCameraFollow>();
            if (camFollow == null) camFollow = mainCamera.gameObject.AddComponent<OWCameraFollow>();
            camFollow.SetTarget(player.transform);
        }
    }

    private GameObject BuildDefaultPlayer(Vector3 position)
    {
        var go           = new GameObject("OWPlayer");
        go.transform.position = position;

        var sr           = go.AddComponent<SpriteRenderer>();
        sr.sprite        = CreateRect(1f, 1f);
        sr.color         = new Color(1f, 0.85f, 0.2f);
        sr.sortingOrder  = 5;
        go.transform.localScale = new Vector3(0.6f, 0.8f, 1f);

        var col          = go.AddComponent<CapsuleCollider2D>();
        col.size         = new Vector2(0.85f, 0.95f);
        col.offset       = new Vector2(0f, 0f);

        var rb           = go.AddComponent<Rigidbody2D>();
        rb.gravityScale  = 3f;
        rb.freezeRotation = true;

        go.AddComponent<OWPlayerController>();

        return go;
    }

    // ── Helpers sprites ───────────────────────────────────────────────────────

    private Sprite CreateRect(float width, float height)
    {
        var tex = new Texture2D(4, 4);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private Sprite CreateCircle()
    {
        int size = 64;
        var tex  = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private GameObject BuildWorldText(string text, Vector3 worldPos, int size, Color color)
    {
        var canvasGO       = new GameObject($"Label_{text}");
        canvasGO.transform.position = worldPos;

        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.sortingOrder = 8;

        var rect           = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta     = new Vector2(200f, 40f);
        canvasGO.transform.localScale = Vector3.one * 0.01f;

        var textGO         = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);

        var tmp            = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = size;
        tmp.color          = color;
        tmp.alignment      = TextAlignmentOptions.Center;

        var textRect       = tmp.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        return canvasGO;
    }

    // ── Réflexion pour assigner champs privés sérialisés ─────────────────────

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
        else Debug.LogWarning($"[OWSceneSetup] Champ '{fieldName}' introuvable sur {target.GetType().Name}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureOWGameManager()
    {
        if (OWGameManager.Instance != null) return;
        var go = new GameObject("OWGameManager");
        go.AddComponent<OWGameManager>();
    }
}
