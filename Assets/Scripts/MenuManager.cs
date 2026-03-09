using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gère uniquement la logique fonctionnelle du menu : câblage des boutons.
/// La transition visuelle est entièrement gérée par SceneTransition.
/// </summary>
public class MenuManager : MonoBehaviour
{
    private const string SceneRetour  = "GameAndWatch";
    private const string SceneEcho    = "Minijeu-Bulles";
    private const string SceneArena   = "CircleArena";
    private const string TitleRetour  = "RETOUR";
    private const string TitleEcho    = "ÉCHO";
    private const string TitleArena   = "ARÈNE";

    private void Start()
    {
        EnsureEventSystem();
        EnsureSceneTransition();

        WireButton("RetourButton", () => SceneTransition.Instance.LoadScene(SceneRetour, TitleRetour));
        WireButton("EchoButton",   () => SceneTransition.Instance.LoadScene(SceneEcho,   TitleEcho));
        WireButton("ArenaButton",  () => SceneTransition.Instance.LoadScene(SceneArena,  TitleArena));
        WireButton("QuitButton",   OnQuit);
    }

    private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[MenuManager] '{objectName}' introuvable."); return; }

        Button btn = go.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning($"[MenuManager] Button manquant sur '{objectName}'."); return; }

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

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }
}


