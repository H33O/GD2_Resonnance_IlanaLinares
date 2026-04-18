using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Outil d'éditeur pour créer la scène du mini-jeu Tetris×Pac-Man en un clic.
/// Menu : Tools ▸ TetrisPacMan ▸ Build Scene
/// </summary>
public static class TPMSceneBuilder
{
    private const string ScenePath    = "Assets/Scenes/TetrisPacMan.unity";
    private const string SettingsPath = "Assets/ScriptableObjects/TPMSettings.asset";

    [MenuItem("Tools/TetrisPacMan/Build Scene")]
    public static void BuildScene()
    {
        // ── 1. Crée ou ouvre la scène ─────────────────────────────────────────

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 2. Crée le ScriptableObject settings s'il n'existe pas ────────────

        var settings = AssetDatabase.LoadAssetAtPath<TPMSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<TPMSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TPMSceneBuilder] TPMSettings créé : {SettingsPath}");
        }

        // ── 3. SceneSetup root GameObject ─────────────────────────────────────

        var setupGO   = new GameObject("SceneSetup");
        var setup     = setupGO.AddComponent<TPMSceneSetup>();
        setup.settings = settings;

        // ── 4. Sauvegarde ─────────────────────────────────────────────────────

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[TPMSceneBuilder] Scène créée : {ScenePath}");
        EditorUtility.DisplayDialog(
            "TetrisPacMan – Scène créée",
            $"Scène sauvegardée :\n{ScenePath}\n\nAjoute-la au Build Settings avant de lancer le jeu.",
            "OK");
    }

    [MenuItem("Tools/TetrisPacMan/Add To Build Settings")]
    public static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        bool alreadyAdded = false;
        foreach (var s in scenes)
        {
            if (s.path == ScenePath) { alreadyAdded = true; break; }
        }

        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[TPMSceneBuilder] Scène ajoutée aux Build Settings : {ScenePath}");
        }
        else
        {
            Debug.Log("[TPMSceneBuilder] Scène déjà présente dans les Build Settings.");
        }
    }
}
