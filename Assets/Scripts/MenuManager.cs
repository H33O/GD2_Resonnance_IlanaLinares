using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gère uniquement la logique fonctionnelle du menu : câblage des boutons.
/// Toute l'esthétique est définie directement dans la scène via le Canvas.
/// </summary>
public class MenuManager : MonoBehaviour
{
    private const string SceneRetour = "GameAndWatch";
    private const string SceneEcho   = "Minijeu-Bulles";

    private void Start()
    {
        EnsureEventSystem();

        WireButton("RetourButton", () => SceneManager.LoadScene(SceneRetour));
        WireButton("EchoButton",   () => SceneManager.LoadScene(SceneEcho));
        WireButton("QuitButton",   OnQuit);
    }

    // ── Logique boutons ───────────────────────────────────────────────────────

    /// <summary>Trouve un GameObject par nom et y attache une action au Button.</summary>
    private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning($"[MenuManager] Bouton '{objectName}' introuvable dans la scène.");
            return;
        }

        Button btn = go.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning($"[MenuManager] Composant Button manquant sur '{objectName}'.");
            return;
        }

        btn.onClick.AddListener(action);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}

