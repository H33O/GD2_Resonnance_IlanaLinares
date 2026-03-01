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
    private const string TitleRetour = "RETOUR";
    private const string TitleEcho   = "ÉCHO";

    private void Start()
    {
        EnsureEventSystem();
        EnsureSceneTransition();

        WireButton("RetourButton", () => SceneTransition.Instance.LoadScene(SceneRetour, TitleRetour));
        WireButton("EchoButton",   () => SceneTransition.Instance.LoadScene(SceneEcho,   TitleEcho));
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

    /// <summary>Crée le SceneTransition singleton s'il n'existe pas encore.</summary>
    private void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }
}

