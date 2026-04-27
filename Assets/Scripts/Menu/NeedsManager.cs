using System;
using UnityEngine;

/// <summary>
/// [OBSOLÈTE] Ancien gestionnaire des jauges Eau / Nourriture / Sommeil.
/// Ce système a été remplacé par <see cref="QuestManager"/>.
/// La classe est conservée vide pour éviter des erreurs de référence dans les scènes existantes.
/// </summary>
public class NeedsManager : MonoBehaviour
{
    public static NeedsManager Instance { get; private set; }

    // Événements conservés pour compatibilité (ne sont plus déclenchés)
    public event Action<float, float, float> OnNeedsChanged;
    public event Action<int>                 OnDayAdvanced;

    public float Water => 100f;
    public float Food  => 100f;
    public float Sleep => 100f;
    public int   Day   => 1;
    public float CycleProgress => 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RefillWater(float amount) { }
    public void RefillFood (float amount) { }
    public void RefillSleep(float amount) { }

    public static NeedsManager EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("NeedsManager");
        return go.AddComponent<NeedsManager>();
    }
}
