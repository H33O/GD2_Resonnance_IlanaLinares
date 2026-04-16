using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Portail interactif dans l'overworld. Affiche un label et déclenche
/// la transition vers un mini-jeu lorsque le joueur l'active.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OWPortal : MonoBehaviour
{
    [Header("Mini-jeu associé")]
    [SerializeField] private string targetScene;
    [SerializeField] private string portalLabel = "Mini-Jeu";

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptObject;   // assigné par OWSceneSetup

    [Header("Visuel")]
    [SerializeField] private SpriteRenderer portalRenderer;
    [SerializeField] private Color idleColor    = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color activeColor  = new Color(1f, 1f, 0.2f, 1f);
    [SerializeField] private float pulseSpeed   = 2f;

    private bool playerInRange  = false;
    private bool transitioning  = false;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (portalRenderer != null)
            portalRenderer.color = idleColor;
    }

    public void Configure(string scene, string label)
    {
        targetScene  = scene;
        portalLabel  = label;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!playerInRange || transitioning) return;

        AnimatePortal();
        CheckActivation();
    }

    private void AnimatePortal()
    {
        if (portalRenderer == null) return;
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        portalRenderer.color = Color.Lerp(idleColor, activeColor, pulse);
    }

    private void CheckActivation()
    {
        bool activated = false;

        // Clavier
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            activated = true;

        // Tactile : double-tap dans la zone
        if (!activated && UnityEngine.InputSystem.Touchscreen.current != null)
        {
            foreach (var touch in UnityEngine.InputSystem.Touchscreen.current.touches)
            {
                if (touch.tapCount.ReadValue() >= 2)
                {
                    activated = true;
                    break;
                }
            }
        }

        if (activated) StartCoroutine(EnterPortal());
    }

    // ── Transition ────────────────────────────────────────────────────────────

    private IEnumerator EnterPortal()
    {
        transitioning = true;
        SetPromptVisible(false);

        // Petit délai visuel avant transition
        if (portalRenderer != null)
            portalRenderer.color = Color.white;

        yield return new WaitForSeconds(0.15f);

        var manager = OWGameManager.Instance;
        if (manager != null)
            manager.EnterMiniGame(targetScene, transform.position);
        else
        {
            Debug.LogWarning("[OWPortal] OWGameManager introuvable.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        SetPromptVisible(true);

        if (portalRenderer != null)
            portalRenderer.color = activeColor;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        transitioning = false;
        SetPromptVisible(false);

        if (portalRenderer != null)
            portalRenderer.color = idleColor;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetPromptVisible(bool visible)
    {
        if (promptObject != null)
            promptObject.SetActive(visible);
    }

    public string PortalLabel => portalLabel;
}
