using System;
using UnityEngine;

/// <summary>
/// Character types available in the squad.
/// </summary>
public enum SGCharacterType
{
    Speed    = 0,
    Lucky    = 1,
    Guardian = 2,
    Fury     = 3,
}

/// <summary>
/// Static identity data for each character: color, name, score threshold, description.
/// </summary>
public static class SGCharacterDefs
{
    // Score required to unlock each character
    public static readonly int[] UnlockScores = { 10, 25, 50, 100 };

    // Distinct color per character circle
    public static readonly Color[] Colors =
    {
        new Color(0.40f, 0.80f, 1.00f),   // Speed    — bleu ciel
        new Color(0.50f, 1.00f, 0.50f),   // Lucky    — vert clair
        new Color(1.00f, 0.85f, 0.25f),   // Guardian — or
        new Color(1.00f, 0.35f, 0.35f),   // Fury     — rouge
    };

    public static readonly string[] Names        = { "SPEED", "LUCKY", "GUARDIAN", "FURY" };
    public static readonly string[] Descriptions =
    {
        "Fenêtre de parade élargie",
        "Chance de doubler les points",
        "Parade automatique",
        "Mode Fury au combo max",
    };
}

/// <summary>
/// ScriptableObject that stores the runtime state of the player's squad.
/// Characters are unlocked automatically when score thresholds are reached.
/// Create via  Assets ▸ Create ▸ SlashGame ▸ SquadData.
/// </summary>
[CreateAssetMenu(fileName = "SGSquadData", menuName = "SlashGame/SquadData")]
public class SGSquadData : ScriptableObject
{
    [Serializable]
    public class CharacterSlot
    {
        public SGCharacterType type;
        public bool            unlocked;
        [Range(0, 5)]
        public int             level;
    }

    [Header("Squad Slots (match enum order)")]
    public CharacterSlot[] slots = new CharacterSlot[4]
    {
        new CharacterSlot { type = SGCharacterType.Speed,    unlocked = false, level = 0 },
        new CharacterSlot { type = SGCharacterType.Lucky,    unlocked = false, level = 0 },
        new CharacterSlot { type = SGCharacterType.Guardian, unlocked = false, level = 0 },
        new CharacterSlot { type = SGCharacterType.Fury,     unlocked = false, level = 0 },
    };

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns true if the given character type is unlocked.</summary>
    public bool IsUnlocked(SGCharacterType type)
    {
        int idx = (int)type;
        return idx < slots.Length && slots[idx].unlocked;
    }

    /// <summary>Returns the level of the given character (0 = base).</summary>
    public int GetLevel(SGCharacterType type)
    {
        int idx = (int)type;
        return idx < slots.Length ? slots[idx].level : 0;
    }

    /// <summary>
    /// Checks all score thresholds and unlocks the first newly eligible character.
    /// Returns its index, or -1 if nothing new was unlocked.
    /// </summary>
    public int TryUnlockForScore(int currentScore)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].unlocked && currentScore >= SGCharacterDefs.UnlockScores[i])
            {
                slots[i].unlocked = true;
                return i;
            }
        }
        return -1;
    }

    /// <summary>Returns the index of a random already-unlocked slot, or 0 as fallback.</summary>
    public int GetRandomUnlockedIndex()
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].unlocked) count++;

        if (count == 0) return 0;

        int pick = UnityEngine.Random.Range(0, count);
        int seen = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].unlocked) continue;
            if (seen == pick) return i;
            seen++;
        }
        return 0;
    }

    /// <summary>Applies one upgrade to a character slot.</summary>
    public void Upgrade(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex].level = Mathf.Min(slots[slotIndex].level + 1, 5);
    }

    /// <summary>Resets all slots — call at session start so each run starts fresh.</summary>
    public void ResetForSession()
    {
        foreach (var s in slots) { s.unlocked = false; s.level = 0; }
    }

    // ── Derived stat helpers ──────────────────────────────────────────────────

    /// <summary>Parry window bonus added by Speed character (seconds).</summary>
    public float SpeedParryBonus =>
        IsUnlocked(SGCharacterType.Speed) ? 0.04f + GetLevel(SGCharacterType.Speed) * 0.01f : 0f;

    /// <summary>Chance that Guardian auto-parries the next slash (0-1).</summary>
    public float GuardianAutoParryChance =>
        IsUnlocked(SGCharacterType.Guardian)
            ? 0.12f + GetLevel(SGCharacterType.Guardian) * 0.03f
            : 0f;
}
