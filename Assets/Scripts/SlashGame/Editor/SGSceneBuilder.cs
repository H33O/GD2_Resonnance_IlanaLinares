#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility that creates and configures the SlashGame scene with a
/// single root SceneSetup object (all logic builds at runtime).
/// Run via  Tools ▸ SlashGame ▸ Build Scene.
/// </summary>
public static class SGSceneBuilder
{
    private const string ScenePath      = "Assets/Scenes/SlashGame.unity";
    private const string SettingsPath   = "Assets/Settings/SGSettings.asset";
    private const string SquadDataPath  = "Assets/Settings/SGSquadData.asset";

    [MenuItem("Tools/SlashGame/Build Scene")]
    public static void BuildSlashGameScene()
    {
        // ── Open scene ────────────────────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[SGSceneBuilder] Could not open '{ScenePath}'.");
            return;
        }

        // ── Clear existing objects ────────────────────────────────────────────
        foreach (var root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // ── Load settings ─────────────────────────────────────────────────────
        var settings  = AssetDatabase.LoadAssetAtPath<SGSettings>(SettingsPath);
        var squadData = AssetDatabase.LoadAssetAtPath<SGSquadData>(SquadDataPath);

        if (settings == null)
            Debug.LogWarning("[SGSceneBuilder] SGSettings asset not found — create it via Assets ▸ Create ▸ SlashGame ▸ Settings.");

        if (squadData == null)
            Debug.LogWarning("[SGSceneBuilder] SGSquadData asset not found — create it via Assets ▸ Create ▸ SlashGame ▸ SquadData.");

        // ── Create SceneSetup root ────────────────────────────────────────────
        var go    = new GameObject("SceneSetup");
        var setup = go.AddComponent<SGSceneSetup>();
        setup.settings  = settings;
        setup.squadData = squadData;

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SGSceneBuilder] SlashGame scene built and saved successfully.");

        // ── Ensure scene is in Build Settings ────────────────────────────────
        AddToBuildSettings(ScenePath);
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        foreach (var s in scenes)
            if (s.path == scenePath) return; // already registered

        scenes.Add(new EditorBuildSettingsScene(scenePath, enabled: true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[SGSceneBuilder] Added '{scenePath}' to Build Settings.");
    }
}
#endif
