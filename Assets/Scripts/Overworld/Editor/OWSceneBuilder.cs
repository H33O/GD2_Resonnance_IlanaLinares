using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crée et configure la scène Overworld depuis le menu Unity Editor.
/// Menu : Overworld / Build Overworld Scene
/// </summary>
public static class OWSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Overworld.unity";

    [MenuItem("Overworld/Build Overworld Scene")]
    public static void BuildScene()
    {
        // Sauvegarde la scène courante
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Crée ou ouvre la scène
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Caméra ────────────────────────────────────────────────────────────
        var cameraGO        = new GameObject("Main Camera");
        cameraGO.tag        = "MainCamera";
        var cam             = cameraGO.AddComponent<Camera>();
        cam.orthographic    = true;
        cam.orthographicSize = 6f;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<OWCameraFollow>();

        // URP Camera Data
        cameraGO.AddComponent<UniversalAdditionalCameraData>();

        // ── Point de spawn ────────────────────────────────────────────────────
        var spawnGO         = new GameObject("PlayerSpawn");
        spawnGO.transform.position = new Vector3(0f, -3.5f, 0f);

        // ── OWGameManager (racine de scène, sera DontDestroyOnLoad) ──────────
        var managerGO       = new GameObject("OWGameManager");
        managerGO.AddComponent<OWGameManager>();

        // ── OWSceneSetup ─────────────────────────────────────────────────────
        var setupGO         = new GameObject("OWSceneSetup");
        var setup           = setupGO.AddComponent<OWSceneSetup>();

        // Assigne les références sérialisées via SerializedObject
        var so              = new SerializedObject(setup);
        so.FindProperty("mainCamera").objectReferenceValue  = cam;
        so.FindProperty("playerSpawn").objectReferenceValue = spawnGO.transform;

        // Liste des scènes mini-jeux
        var scenesProp = so.FindProperty("miniGameScenes");
        scenesProp.ClearArray();
        string[] scenes = { "GameAndWatch", "Minijeu-Bulles", "SlashGame", "CircleArena" };
        for (int i = 0; i < scenes.Length; i++)
        {
            scenesProp.InsertArrayElementAtIndex(i);
            scenesProp.GetArrayElementAtIndex(i).stringValue = scenes[i];
        }

        // Liste des labels
        var labelsProp = so.FindProperty("miniGameLabels");
        labelsProp.ClearArray();
        string[] labels = { "Sonantia", "Écho", "Slash", "Arène" };
        for (int i = 0; i < labels.Length; i++)
        {
            labelsProp.InsertArrayElementAtIndex(i);
            labelsProp.GetArrayElementAtIndex(i).stringValue = labels[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        // ── EventSystem ───────────────────────────────────────────────────────
        var eventSystemGO   = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── SceneTransition ───────────────────────────────────────────────────
        var transitionGO    = new GameObject("SceneTransition");
        transitionGO.AddComponent<SceneTransition>();

        // ── Sauvegarde de la scène ────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);

        // ── Ajoute la scène aux Build Settings si absent ──────────────────────
        AddSceneToBuildSettings(ScenePath);

        Debug.Log($"[OWSceneBuilder] Scène Overworld créée : {ScenePath}");
        EditorUtility.DisplayDialog("Overworld",
            $"Scène créée avec succès !\n{ScenePath}\n\nVérifiez que toutes les scènes mini-jeux sont dans les Build Settings.",
            "OK");
    }

    // ── Menu pour ajouter uniquement la scène aux Build Settings ─────────────

    [MenuItem("Overworld/Add All Scenes to Build Settings")]
    public static void AddAllScenesToBuild()
    {
        string[] scenePaths =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/Overworld.unity",
            "Assets/Scenes/GameAndWatch.unity",
            "Assets/Scenes/Minijeu-Bulles.unity",
            "Assets/Scenes/SlashGame.unity",
        };

        var existingScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        foreach (string path in scenePaths)
        {
            bool alreadyIn = false;
            foreach (var s in existingScenes)
                if (s.path == path) { alreadyIn = true; break; }

            if (!alreadyIn)
                existingScenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = existingScenes.ToArray();
        Debug.Log("[OWSceneBuilder] Build Settings mis à jour avec toutes les scènes.");
        EditorUtility.DisplayDialog("Build Settings", "Toutes les scènes ont été ajoutées aux Build Settings.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == path) return;  // déjà présente

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
