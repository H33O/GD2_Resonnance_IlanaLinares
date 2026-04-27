using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Outil éditeur pour créer la scène <c>Intro</c> et l'ajouter aux Build Settings.
///
/// Utilisation : menu Unity → <b>Resonance → Create Intro Scene</b>.
/// </summary>
#if UNITY_EDITOR
public static class IntroLeafSetup
{
    private const string IntroScenePath = "Assets/Scenes/Intro.unity";
    private const string MenuSceneName  = "Menu";

    [MenuItem("Resonance/Create Intro Scene")]
    public static void CreateIntroScene()
    {
        // ── Créer la scène vide ───────────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Caméra ────────────────────────────────────────────────────────────
        var camGO = new GameObject("MainCamera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic    = true;
        cam.tag             = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // ── Bootstrap IntroLeaf ───────────────────────────────────────────────
        var introGO = new GameObject("IntroLeaf");
        introGO.AddComponent<IntroLeaf>();

        // ── Sauvegarder la scène ──────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, IntroScenePath);
        Debug.Log($"[IntroLeafSetup] Scène créée : {IntroScenePath}");

        // ── Ajouter aux Build Settings en premier (index 0) ───────────────────
        AddSceneToBuildSettings(IntroScenePath, 0);

        // ── Vérifier que Menu est dans les Build Settings ─────────────────────
        EnsureMenuInBuildSettings();

        Debug.Log("[IntroLeafSetup] Scène Intro ajoutée en index 0 des Build Settings.");
        EditorUtility.DisplayDialog("Intro créée",
            $"Scène Intro créée : {IntroScenePath}\n\n" +
            "Elle est maintenant en index 0 des Build Settings.\n" +
            "Vérifiez que la scène 'Menu' est bien présente dans les Build Settings.",
            "OK");
    }

    private static void AddSceneToBuildSettings(string scenePath, int insertIndex)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        // Supprimer toute entrée existante pour cette scène
        scenes.RemoveAll(s => s.path == scenePath);

        // Insérer à l'index voulu (clamp pour éviter IndexOutOfRange)
        int clampedIndex = Mathf.Clamp(insertIndex, 0, scenes.Count);
        scenes.Insert(clampedIndex, new EditorBuildSettingsScene(scenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureMenuInBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        bool menuFound = false;
        foreach (var s in scenes)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(s.path);
            if (name == MenuSceneName) { menuFound = true; break; }
        }

        if (!menuFound)
        {
            // Chercher la scène Menu dans le projet
            string[] guids = AssetDatabase.FindAssets("t:Scene Menu");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == MenuSceneName)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    EditorBuildSettings.scenes = scenes.ToArray();
                    Debug.Log($"[IntroLeafSetup] Scène Menu ajoutée aux Build Settings : {path}");
                    return;
                }
            }
            Debug.LogWarning("[IntroLeafSetup] Scène 'Menu' introuvable dans le projet. " +
                             "Ajoute-la manuellement aux Build Settings.");
        }
    }
}
#endif
