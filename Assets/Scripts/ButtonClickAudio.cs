using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// S'instancie une seule fois par scène et branche automatiquement
/// <see cref="AudioManager.PlayClick"/> sur le <see cref="Button.onClick"/>
/// de chaque bouton présent ou créé dynamiquement.
///
/// Utilise un <see cref="SceneManager.sceneLoaded"/> pour se ré-appliquer
/// après chaque changement de scène.
/// </summary>
public class ButtonClickAudio : MonoBehaviour
{
    // ── Singleton léger (non-persistant) ──────────────────────────────────────

    private static ButtonClickAudio _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        HookAllButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Branchement ───────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attendre une frame que les GameObjects procéduraux soient construits
        StartCoroutine(HookNextFrame());
    }

    private System.Collections.IEnumerator HookNextFrame()
    {
        yield return null;   // 1 frame
        yield return null;   // 2 frames (certains setups utilisent Start())
        HookAllButtons();
    }

    /// <summary>
    /// Parcourt tous les <see cref="Button"/> actifs dans la scène et ajoute
    /// le listener clic si celui-ci n'est pas déjà enregistré.
    /// </summary>
    public static void HookAllButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
            HookButton(btn);
    }

    /// <summary>Branche le son de clic sur un bouton unique.</summary>
    public static void HookButton(Button btn)
    {
        if (btn == null) return;

        // Retirer puis ré-ajouter pour éviter les doublons
        btn.onClick.RemoveListener(AudioManager.PlayClick);
        btn.onClick.AddListener(AudioManager.PlayClick);
    }
}
