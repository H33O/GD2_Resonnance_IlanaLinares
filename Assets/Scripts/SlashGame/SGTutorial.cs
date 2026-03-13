using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the 5-step interactive tutorial integrated into gameplay.
/// Pauses between steps and waits for player interaction.
/// Attach to the "Tutorial" GameObject.
/// </summary>
public class SGTutorial : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    public SGSlashSpawner    slashSpawner;
    public SGSquadData       squadData;
    public TextMeshProUGUI   instructionText;
    public CanvasGroup        instructionGroup;

    [Header("Timings")]
    public float stepFadeInDuration  = 0.4f;
    public float stepFadeOutDuration = 0.3f;

    // ── Internal ──────────────────────────────────────────────────────────────

    private SGSlash pendingSlash;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (instructionGroup != null) instructionGroup.alpha = 0f;
        StartCoroutine(TutorialRoutine());
    }

    // ── Tutorial flow ─────────────────────────────────────────────────────────

    private IEnumerator TutorialRoutine()
    {
        // ── Step 1: first slow slash ──────────────────────────────────────────
        yield return ShowInstruction("TAP TO PARRY");
        yield return new WaitForSeconds(0.8f);

        pendingSlash = slashSpawner.SpawnTutorialSlash(90f); // from the top
        yield return WaitForParry();
        yield return HideInstruction();

        // ── Step 2: second slash from different direction ─────────────────────
        yield return new WaitForSeconds(0.6f);
        yield return ShowInstruction("PARRY AGAIN");

        pendingSlash = slashSpawner.SpawnTutorialSlash(225f); // from bottom-left
        yield return WaitForParry();
        yield return HideInstruction();

        // ── Step 3: energy cone intro ─────────────────────────────────────────
        yield return new WaitForSeconds(0.4f);
        yield return ShowInstruction("FILL THE CONE");
        yield return new WaitForSeconds(1.8f);
        yield return HideInstruction();

        // ── Step 4: third slash — cone should be partially filled already ─────
        pendingSlash = slashSpawner.SpawnTutorialSlash(315f);
        yield return WaitForParry();

        // ── Step 5: message d'attente — les alliés se débloquent par le score ──
        yield return new WaitForSeconds(0.5f);
        yield return ShowInstruction("PARE POUR DÉBLOQUER TES ALLIÉS!");

        yield return new WaitForSeconds(1.8f);
        yield return HideInstruction();

        // ── Begin real gameplay ───────────────────────────────────────────────
        SGGameManager.Instance?.BeginPlay();
        slashSpawner.StartSpawning();

        Destroy(gameObject); // tutorial done
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator WaitForParry()
    {
        // Subscribe temporarily to parry event via polling GameManager state
        int startScore = SGGameManager.Instance != null ? SGGameManager.Instance.Score : 0;
        while (true)
        {
            int cur = SGGameManager.Instance != null ? SGGameManager.Instance.Score : 0;
            if (cur > startScore) break;  // score changed → parry happened
            yield return null;
        }
    }

    private IEnumerator ShowInstruction(string text)
    {
        if (instructionText  != null) instructionText.text = text;
        if (instructionGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < stepFadeInDuration)
        {
            elapsed += Time.deltaTime;
            instructionGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / stepFadeInDuration);
            yield return null;
        }
        instructionGroup.alpha = 1f;
    }

    private IEnumerator HideInstruction()
    {
        if (instructionGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < stepFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            instructionGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / stepFadeOutDuration);
            yield return null;
        }
        instructionGroup.alpha = 0f;
    }
}
