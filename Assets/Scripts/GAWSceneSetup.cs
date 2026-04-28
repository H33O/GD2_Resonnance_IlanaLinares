using TMPro;
using UnityEngine;

/// <summary>
/// Bootstrap visuel de la scène GameAndWatch.
///
/// Responsabilités :
///   1. Désactive le sprite « fond jeu » statique.
///   2. Configure la caméra en fond noir pur.
///   3. Spawne de légers halos qui pulsent (<see cref="GAWHalo"/>).
///   4. Spawne de nombreuses micro-lucioles rebondissantes (<see cref="GAWFirefly"/>).
///   5. Applique la police Michroma sur tous les TextMeshProUGUI de la scène.
///
/// Attacher ce composant sur le GameObject GAWSetup dans la scène GameAndWatch.
/// </summary>
public class GAWSceneSetup : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const int HaloCount    = 18;
    private const int FireflyCount = 22;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        DisableBackgroundSprite();
        ConfigureCamera();
        SpawnHalos();
        SpawnFireflies();
        ApplyMichromaToScene();
    }

    // ── 1. Désactivation du fond statique ─────────────────────────────────────

    private static void DisableBackgroundSprite()
    {
        var fondJeu = GameObject.Find("fond jeu");
        if (fondJeu != null)
            fondJeu.SetActive(false);
    }

    // ── 2. Fond noir ──────────────────────────────────────────────────────────

    private static void ConfigureCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic    = true;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    // ── 3. Halos pulsants ─────────────────────────────────────────────────────

    private static void SpawnHalos()
    {
        for (int i = 0; i < HaloCount; i++)
        {
            var go = new GameObject($"GAW_Halo_{i}");
            go.AddComponent<GAWHalo>();
        }
    }

    // ── 4. Micro-lucioles rebondissantes ──────────────────────────────────────

    private static void SpawnFireflies()
    {
        for (int i = 0; i < FireflyCount; i++)
        {
            var go = new GameObject($"GAW_Firefly_{i}");
            go.AddComponent<GAWFirefly>();
        }
    }

    // ── 5. Police Michroma sur tous les TMP ───────────────────────────────────

    private static void ApplyMichromaToScene()
    {
        var font = LoadMichroma();
        if (font == null)
        {
            Debug.LogWarning("[GAWSceneSetup] Michroma-Regular SDF introuvable — " +
                             "génère le font asset via Window > TextMeshPro > Font Asset Creator.");
            return;
        }

        var allTmp = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var tmp in allTmp)
            tmp.font = font;
    }

    private static TMP_FontAsset LoadMichroma()
    {
        var f = Resources.Load<TMP_FontAsset>("Michroma-Regular SDF");
        if (f != null) return f;

#if UNITY_EDITOR
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Michroma-Regular SDF.asset");
        if (f != null) return f;

        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Michroma/Michroma-Regular SDF.asset");
#endif
        return f;
    }
}
