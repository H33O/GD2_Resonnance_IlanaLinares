using UnityEngine;

/// <summary>
/// Portail de retour placé dans chaque mini-jeu.
/// Appelle OWGameManager.ReturnToOverworld() quand le joueur l'active.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OWReturnPortal : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool autoReturn          = false;
    [SerializeField] private bool countAsCompletion   = true;

    [Header("Prompt")]
    [SerializeField] private GameObject promptObject;

    private bool playerInRange = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (autoReturn) return;
        if (!playerInRange) return;

        bool activated = false;

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            activated = true;

        if (!activated && UnityEngine.InputSystem.Touchscreen.current != null)
        {
            foreach (var touch in UnityEngine.InputSystem.Touchscreen.current.touches)
            {
                if (touch.tapCount.ReadValue() >= 2) { activated = true; break; }
            }
        }

        if (activated) Return();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (promptObject != null) promptObject.SetActive(true);
        if (autoReturn) Return();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptObject != null) promptObject.SetActive(false);
    }

    /// <summary>Déclenche le retour vers l'overworld.</summary>
    public void Return()
    {
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.ReturnToOverworld(countAsCompletion);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(OWGameManager.SceneOverworld);
    }
}
