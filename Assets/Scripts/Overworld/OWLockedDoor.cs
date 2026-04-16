using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Porte verrouillée dans l'overworld.
/// Affiche un widget UI "Je pense qu'il faut une clé".
/// Se déverrouille si OWGameManager.HasKey est vrai.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OWLockedDoor : MonoBehaviour
{
    [Header("Textes UI")]
    [SerializeField] private GameObject hintPanel;          // Panel contenant le texte hint
    [SerializeField] private TextMeshProUGUI hintText;

    private const string HintMessage = "Je pense qu'il faut une clé";
    private const string UnlockMessage = "La porte s'ouvre...";

    private bool playerInRange = false;
    private bool isUnlocked    = false;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetHintVisible(false);

        if (hintText != null)
            hintText.text = HintMessage;

        // S'abonner à l'événement de déverrouillage
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.OnDoorUnlocked.AddListener(OnDoorUnlocked);
    }

    private void OnEnable()
    {
        // Abonnement après rechargement de scène si le manager existait déjà
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.OnDoorUnlocked.AddListener(OnDoorUnlocked);
    }

    private void OnDisable()
    {
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.OnDoorUnlocked.RemoveListener(OnDoorUnlocked);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!playerInRange || isUnlocked) return;
        CheckInteraction();
    }

    private void CheckInteraction()
    {
        bool tryOpen = false;

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            tryOpen = true;

        if (!tryOpen && UnityEngine.InputSystem.Touchscreen.current != null)
        {
            foreach (var touch in UnityEngine.InputSystem.Touchscreen.current.touches)
            {
                if (touch.tapCount.ReadValue() >= 2) { tryOpen = true; break; }
            }
        }

        if (!tryOpen) return;

        if (OWGameManager.Instance != null && OWGameManager.Instance.TryUnlockDoor())
        {
            // Le manager émettra l'événement OnDoorUnlocked
        }
        else
        {
            // Affiche le hint "il faut une clé"
            ShowHint(HintMessage);
        }
    }

    // ── Événement déverrouillage ──────────────────────────────────────────────

    private void OnDoorUnlocked()
    {
        isUnlocked = true;
        StartCoroutine(UnlockSequence());
    }

    private IEnumerator UnlockSequence()
    {
        ShowHint(UnlockMessage);
        yield return new WaitForSeconds(1.5f);
        SetHintVisible(false);

        // Désactiver le collider bloquant et le sprite de la porte
        GetComponent<Collider2D>().enabled = false;

        // Cherche un SpriteRenderer enfant pour faire disparaître la porte
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0f;
            Color start   = sr.color;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                sr.color = Color.Lerp(start, Color.clear, elapsed / 0.6f);
                yield return null;
            }
            sr.gameObject.SetActive(false);
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isUnlocked) return;
        playerInRange = true;
        ShowHint(HintMessage);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        SetHintVisible(false);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private void ShowHint(string message)
    {
        if (hintText != null) hintText.text = message;
        SetHintVisible(true);
    }

    private void SetHintVisible(bool visible)
    {
        if (hintPanel != null) hintPanel.SetActive(visible);
    }

    // ── Setters pour OWSceneSetup ─────────────────────────────────────────────

    public void SetHintPanel(GameObject panel, TextMeshProUGUI text)
    {
        hintPanel = panel;
        hintText  = text;
    }
}
