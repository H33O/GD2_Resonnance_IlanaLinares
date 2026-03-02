using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gère l'état du jeu (score, coups, victoire/défaite) et crée l'UI automatiquement.
/// </summary>
public class BubbleGameManager : MonoBehaviour
{
    public static BubbleGameManager Instance { get; private set; }

    [Header("Règles")]
    [SerializeField] private int maxShots = 30;
    [SerializeField] private int targetScore = 500;
    [SerializeField] private int lowShotsThreshold = 6;

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;

    private const string SceneMenu = "Menu";

    public int Score { get; private set; }
    public int ShotsLeft { get; private set; }
    public bool IsGameActive { get; private set; }

    // UI
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI shotsText;
    private TextMeshProUGUI statusText;
    private Transform       canvasTransform;
    private Image           shotsBorderImage;

    // Couleurs
    private static readonly Color ColorShotsNormal = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color ColorShotsDanger = new Color(1f, 0.18f, 0.18f, 1f);

    // Audio
    private AudioSource audioSource;

    // Glitch caméra
    private Vector3    cameraOriginalPos;
    private Coroutine  glitchCoroutine;

    private void Awake()
    {
        Instance = this;
        ShotsLeft = maxShots;
        IsGameActive = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip   = backgroundMusic;
        audioSource.loop   = true;
        audioSource.volume = 0.75f;
        if (backgroundMusic != null) audioSource.Play();

        CreateUI();
        SpawnGoal();
    }

    private void Start()
    {
        if (Camera.main != null)
            cameraOriginalPos = Camera.main.transform.position;
    }

    private void SpawnGoal()
    {
        var goalGO = new GameObject("BubbleGoal");
        var goal   = goalGO.AddComponent<BubbleGoal>();
        goal.Init(Camera.main);
    }

    // ── Score & coups ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (!IsGameActive) return;

        bool danger = ShotsLeft > 0 && ShotsLeft <= lowShotsThreshold;

        if (danger)
        {
            // Couleur rouge pulsante
            float flash = 0.55f + 0.45f * Mathf.Sin(Time.time * 6f);
            Color dangerColor = Color.Lerp(ColorShotsDanger, Color.white, flash * 0.25f);
            shotsText.color = dangerColor;
            if (shotsBorderImage != null) shotsBorderImage.color = dangerColor;

            // Scale pulsante sur le panneau de coups (seulement si pas en animation swallow)
            if (!isSwallowing)
            {
                float scalePulse = 1f + 0.18f * Mathf.Abs(Mathf.Sin(Time.time * 8f));
                if (shotsBorderImage != null)
                    shotsBorderImage.rectTransform.localScale = Vector3.one * scalePulse;
            }

            // Démarre le glitch caméra si pas encore actif
            if (glitchCoroutine == null)
                glitchCoroutine = StartCoroutine(CameraGlitchRoutine());
        }
        else
        {
            shotsText.color = ColorShotsNormal;
            if (shotsBorderImage != null)
            {
                shotsBorderImage.color = ColorShotsNormal;
                shotsBorderImage.rectTransform.localScale = Vector3.one;
            }

            if (glitchCoroutine != null)
            {
                StopCoroutine(glitchCoroutine);
                glitchCoroutine = null;
                if (Camera.main != null)
                    Camera.main.transform.position = cameraOriginalPos;
            }
        }
    }

    // ── Glitch caméra ─────────────────────────────────────────────────────────

    /// <summary>Périodiquement secoue la caméra quand les tirs sont presque épuisés.</summary>
    private IEnumerator CameraGlitchRoutine()
    {
        while (IsGameActive && ShotsLeft > 0 && ShotsLeft <= lowShotsThreshold)
        {
            yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));

            float intensity = Mathf.Lerp(0.03f, 0.18f, 1f - (float)ShotsLeft / lowShotsThreshold);
            int frames = Random.Range(2, 5);
            for (int i = 0; i < frames; i++)
            {
                Camera.main.transform.position = cameraOriginalPos + new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity * 0.4f, intensity * 0.4f),
                    0f);
                yield return null;
            }
            Camera.main.transform.position = cameraOriginalPos;
        }

        if (Camera.main != null)
            Camera.main.transform.position = cameraOriginalPos;
        glitchCoroutine = null;
    }

    // ── Perfect ───────────────────────────────────────────────────────────────

    /// <summary>Affiche un label selon le nombre de bulles tombées.</summary>
    public void ShowPerfect(int dropCount)
    {
        StartCoroutine(PerfectRoutine(dropCount));
    }

    private IEnumerator PerfectRoutine(int dropCount)
    {
        string label = dropCount >= 8 ? "AMAZING!" :
                       dropCount >= 4 ? "PERFECT!" :
                       dropCount >= 2 ? "GREAT!"   : "NICE!";

        // All labels → orange, bigger
        Color labelColor = new Color(1f, 0.45f, 0.05f, 1f);

        var go  = new GameObject("PerfectLabel");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text       = label;
        tmp.fontSize   = 5.5f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.color      = labelColor;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        const float duration  = 1.4f;
        const float riseRange = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / duration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            float alpha  = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            float scale  = Mathf.Lerp(0.5f, 1.3f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2.5f)));

            go.transform.position   = new Vector3(0f, Mathf.Lerp(-0.5f, -0.5f + riseRange, smooth), 0f);
            go.transform.localScale = Vector3.one * scale;
            tmp.color               = new Color(1f, 0.45f, 0.05f, alpha);

            yield return null;
        }

        Destroy(go);
    }

    // ── Shake caméra (chute de bulles) ────────────────────────────────────────

    /// <summary>Déclenche un bref tremblement de caméra (quand des bulles tombent).</summary>
    public void ShakeCamera(float duration = 0.25f, float intensity = 0.08f)
    {
        StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float falloff = 1f - elapsed / duration;
            Camera.main.transform.position = cameraOriginalPos + new Vector3(
                Random.Range(-intensity, intensity) * falloff,
                Random.Range(-intensity * 0.4f, intensity * 0.4f) * falloff,
                0f);
            yield return null;
        }
        Camera.main.transform.position = cameraOriginalPos;
    }

    /// <summary>Adds points to the score (display only — victory is grid-clear only).</summary>
    public void AddScore(int amount)
    {
        if (!IsGameActive) return;
        Score += amount;
        RefreshUI();
    }

    /// <summary>Consumes one shot. Returns false if out of shots or game over.</summary>
    public bool TryShoot()
    {
        if (!IsGameActive || ShotsLeft <= 0) return false;
        ShotsLeft--;
        RefreshUI();
        if (ShotsLeft == 0)
            StartCoroutine(LastShotSafetyCheck());
        return true;
    }

    /// <summary>
    /// Safety net: if the last projectile never triggered an end-state,
    /// force defeat after a short delay.
    /// </summary>
    private System.Collections.IEnumerator LastShotSafetyCheck()
    {
        yield return new WaitForSeconds(2.5f);
        if (IsGameActive)
            EndGame(false);
    }

    /// <summary>Checks loss condition after each bubble placement.</summary>
    public void CheckEnd()
    {
        if (!IsGameActive) return;
        if (ShotsLeft <= 0) EndGame(false);
    }

    /// <summary>Awards extra shots when a bonus bubble is hit and animates the shots counter.</summary>
    public void AwardBonusShots(int amount)
    {
        if (!IsGameActive) return;
        ShotsLeft += amount;
        RefreshUI();
        StartCoroutine(ShotsSwallowRoutine(amount));
    }

    private bool isSwallowing = false;

    private IEnumerator ShotsSwallowRoutine(int amount)
    {
        // ── Floating "+X" label near the shots panel ──────────────────────────
        var labelGO = new GameObject("BonusShotsLabel");
        labelGO.transform.SetParent(canvasTransform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0f);
        labelRT.sizeDelta        = new Vector2(200f, 80f);
        labelRT.anchoredPosition = new Vector2(0f, 310f);
        var labelTMP        = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text       = $"+{amount}";
        labelTMP.fontSize   = 58f;
        labelTMP.fontStyle  = FontStyles.Bold;
        labelTMP.color      = new Color(0.25f, 1f, 0.45f);
        labelTMP.alignment  = TextAlignmentOptions.Center;

        // ── Swallow scale animation on the shots border ────────────────────────
        isSwallowing = true;
        const float growDur   = 0.12f;
        const float overshoot = 1.55f;
        const float shrinkDur = 0.22f;
        const float settleDur = 0.10f;
        float e = 0f;

        // Grow
        while (e < growDur)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(1f, overshoot, e / growDur);
            if (shotsBorderImage != null) shotsBorderImage.rectTransform.localScale = Vector3.one * s;
            yield return null;
        }
        // Shrink
        e = 0f;
        while (e < shrinkDur)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(overshoot, 0.9f, e / shrinkDur);
            if (shotsBorderImage != null) shotsBorderImage.rectTransform.localScale = Vector3.one * s;
            yield return null;
        }
        // Settle
        e = 0f;
        while (e < settleDur)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(0.9f, 1f, e / settleDur);
            if (shotsBorderImage != null) shotsBorderImage.rectTransform.localScale = Vector3.one * s;
            yield return null;
        }
        if (shotsBorderImage != null) shotsBorderImage.rectTransform.localScale = Vector3.one;
        isSwallowing = false;

        // ── Fade out the "+X" label ────────────────────────────────────────────
        float fade = 0.5f, fe = 0f;
        Vector2 startPos = labelRT.anchoredPosition;
        while (fe < fade && labelGO != null)
        {
            fe += Time.deltaTime;
            float t = fe / fade;
            labelTMP.color          = new Color(0.25f, 1f, 0.45f, 1f - t);
            labelRT.anchoredPosition = startPos + Vector2.up * (80f * t);
            yield return null;
        }
        if (labelGO != null) Destroy(labelGO);
    }

    // ── Fin de partie ─────────────────────────────────────────────────────────

    /// <summary>Triggers victory when the projectile reaches the goal bar.</summary>
    public void TriggerVictory()
    {
        if (!IsGameActive) return;
        EndGame(true);
    }

    /// <summary>Déclenche la défaite quand les bulles atteignent le shooter.</summary>
    public void TriggerDefeat()
    {
        if (!IsGameActive) return;
        EndGame(false);
    }

    private void EndGame(bool win)
    {
        IsGameActive = false;
        if (win)
            CreateVictoryScreen();
        else
            CreateDefeatScreen();
    }

    private void CreateVictoryScreen()
    {
        int shotsUsed = maxShots - ShotsLeft;

        // ── Dark overlay ──────────────────────────────────────────────────────
        var overlay = new GameObject("VictoryOverlay");
        overlay.transform.SetParent(canvasTransform, false);
        var overlayRT = overlay.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color         = new Color(0f, 0f, 0f, 0.72f);
        overlayImg.raycastTarget = false;

        // ── Central panel ─────────────────────────────────────────────────────
        var panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(overlay.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(620f, 540f);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.10f, 0.08f, 0.96f);

        // ── VICTORY title ─────────────────────────────────────────────────────
        MakePanelText(panel.transform, "VICTORY!", new Vector2(0f, -75f), new Vector2(580f, 130f),
                      72, FontStyles.Bold, new Color(0.25f, 1f, 0.45f));

        // ── Shot count ────────────────────────────────────────────────────────
        string shotLabel = shotsUsed == 1 ? "Completed in 1 shot!" : $"Completed in {shotsUsed} shots!";
        MakePanelText(panel.transform, shotLabel, new Vector2(0f, -185f), new Vector2(580f, 70f),
                      34, FontStyles.Normal, new Color(1f, 0.92f, 0.3f));

        // ── Score ─────────────────────────────────────────────────────────────
        MakePanelText(panel.transform, $"Score: {Score}", new Vector2(0f, -255f), new Vector2(580f, 55f),
                      28, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f));

        // ── Buttons ───────────────────────────────────────────────────────────
        MakeButton(panel.transform, "Restart",
                   new Vector2(0f, -355f),
                   new Color(0.18f, 0.44f, 0.90f),
                   () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        MakeButton(panel.transform, "Menu",
                   new Vector2(0f, -460f),
                   new Color(0.35f, 0.35f, 0.35f),
                   () => SceneManager.LoadScene(SceneMenu));
    }

    private void MakePanelText(Transform parent, string text, Vector2 pos, Vector2 size,
                                float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject("PanelText");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.alignment  = TextAlignmentOptions.Center;
    }

    // ── UI créée automatiquement ──────────────────────────────────────────────

    private void CreateUI()
    {
        // — EventSystem (obligatoire pour que les boutons répondent aux clics)
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // — Canvas : réutilise l'existant ou en crée un
        Canvas existing = FindFirstObjectByType<Canvas>();
        GameObject canvasGO;

        if (existing != null)
        {
            canvasGO = existing.gameObject;
            // S'assure que le canvas existant peut recevoir les clics
            if (canvasGO.GetComponent<GraphicRaycaster>() == null)
                canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGO = new GameObject("BubbleCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        canvasTransform = canvasGO.transform;
        Transform ct = canvasTransform;

        // — Score (top-left)
        scoreText = MakeText(ct, "Score: 0", new Vector2(20, -20), new Vector2(300, 55), TextAlignmentOptions.TopLeft);
        scoreText.fontSize = 30;

        // — Shots remaining (bottom centre)
        shotsText = MakeShotsPanel(ct);

        // — Win / loss message (centre screen)
        statusText = MakeText(ct, "", Vector2.zero, new Vector2(500, 120), TextAlignmentOptions.Center, center: true);
        statusText.fontSize = 55;
        statusText.gameObject.SetActive(false);
    }

    /// <summary>Adds Restart and Menu buttons after the game ends.</summary>
    private void CreateEndButtons()
    {
        MakeButton(canvasTransform, "Restart",
                   new Vector2(0f, -100f),
                   new Color(0.18f, 0.44f, 0.90f),
                   () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        MakeButton(canvasTransform, "Menu",
                   new Vector2(0f, -220f),
                   new Color(0.35f, 0.35f, 0.35f),
                   () => SceneManager.LoadScene(SceneMenu));
    }

    /// <summary>Shows the full defeat screen with dark overlay, score, and action buttons.</summary>
    private void CreateDefeatScreen()
    {
        // ── Dark full-screen overlay ───────────────────────────────────────────
        var overlay = new GameObject("DefeatOverlay");
        overlay.transform.SetParent(canvasTransform, false);
        var overlayRT = overlay.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color        = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = false; // ne bloque pas les clics vers les boutons enfants

        // ── Central panel ─────────────────────────────────────────────────────
        var panel = new GameObject("DefeatPanel");
        panel.transform.SetParent(overlay.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(600f, 500f);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // ── DEFEAT title ──────────────────────────────────────────────────────
        var title = new GameObject("DefeatTitle");
        title.transform.SetParent(panel.transform, false);
        var titleRT = title.AddComponent<RectTransform>();
        titleRT.anchorMin        = titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.sizeDelta        = new Vector2(560f, 130f);
        titleRT.anchoredPosition = new Vector2(0f, -80f);
        var titleTMP = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "DEFEAT";
        titleTMP.fontSize  = 72;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(0.95f, 0.25f, 0.25f);
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ── Final score ───────────────────────────────────────────────────────
        var scoreLine = new GameObject("DefeatScore");
        scoreLine.transform.SetParent(panel.transform, false);
        var scoreRT = scoreLine.AddComponent<RectTransform>();
        scoreRT.anchorMin        = scoreRT.anchorMax = new Vector2(0.5f, 1f);
        scoreRT.sizeDelta        = new Vector2(560f, 70f);
        scoreRT.anchoredPosition = new Vector2(0f, -195f);
        var scoreTMP = scoreLine.AddComponent<TextMeshProUGUI>();
        scoreTMP.text      = $"Score: {Score}";
        scoreTMP.fontSize  = 38;
        scoreTMP.color     = new Color(1f, 0.85f, 0.3f);
        scoreTMP.alignment = TextAlignmentOptions.Center;

        // ── Restart button ────────────────────────────────────────────────────
        MakeButton(panel.transform, "Restart",
                   new Vector2(0f, -295f),
                   new Color(0.18f, 0.44f, 0.90f),
                   () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        // ── Menu button ───────────────────────────────────────────────────────
        MakeButton(panel.transform, "Menu",
                   new Vector2(0f, -395f),
                   new Color(0.35f, 0.35f, 0.35f),
                   () => SceneManager.LoadScene(SceneMenu));
    }

    private void MakeButton(Transform parent, string label, Vector2 pos, Color color,
                             UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(380f, 110f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 42;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshUI()
    {
        scoreText.text = $"Score: {Score}";
        shotsText.text = $"{ShotsLeft}";

        // Reset colour when no longer in danger
        if (ShotsLeft > lowShotsThreshold || ShotsLeft <= 0)
        {
            shotsText.color = ColorShotsNormal;
            if (shotsBorderImage != null) shotsBorderImage.color = ColorShotsNormal;
        }
    }

    private TextMeshProUGUI MakeShotsPanel(Transform parent)
    {
        // ── Cadre (border) ────────────────────────────────────────────────────
        var border = new GameObject("ShotsBorder");
        border.transform.SetParent(parent, false);
        var borderRT = border.AddComponent<RectTransform>();
        borderRT.anchorMin        = borderRT.anchorMax = new Vector2(0.5f, 0f);
        borderRT.sizeDelta        = new Vector2(196f, 116f);
        borderRT.anchoredPosition = new Vector2(0f, 180f);
        shotsBorderImage = border.AddComponent<Image>();
        shotsBorderImage.color = ColorShotsNormal;

        // ── Fond sombre ───────────────────────────────────────────────────────
        var bg = new GameObject("ShotsPanel");
        bg.transform.SetParent(border.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2( 4f,  4f);
        bgRT.offsetMax = new Vector2(-4f, -4f);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);

        // ── Chiffre centré ────────────────────────────────────────────────────
        var count = new GameObject("Count");
        count.transform.SetParent(bg.transform, false);
        var countRT = count.AddComponent<RectTransform>();
        countRT.anchorMin = Vector2.zero;
        countRT.anchorMax = Vector2.one;
        countRT.offsetMin = countRT.offsetMax = Vector2.zero;
        var countTMP = count.AddComponent<TextMeshProUGUI>();
        countTMP.text      = $"{maxShots}";
        countTMP.fontSize  = 68;
        countTMP.fontStyle = FontStyles.Bold;
        countTMP.color     = ColorShotsNormal;
        countTMP.alignment = TextAlignmentOptions.Center;

        return countTMP;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, Vector2 pos, Vector2 size,
                                     TextAlignmentOptions align, bool center = false)
    {
        var go = new GameObject("TMP");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();

        if (center)
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        else
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = align;
        return tmp;
    }
}
