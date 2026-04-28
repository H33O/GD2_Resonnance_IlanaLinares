using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton persistant gérant la musique d'ambiance et les effets sonores globaux.
///
/// Usage :
///   <c>AudioManager.Instance.PlayMusic(clip);</c>
///   <c>AudioManager.PlayClick();</c>
///
/// Chaque scène qui démarre sa propre musique appelle <see cref="PlayMusic"/>.
/// Les boutons déclenchent le son clic via <see cref="PlayClick"/>.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Musiques d'ambiance")]
    [Tooltip("Musique du menu principal.")]
    public AudioClip menuMusic;

    [Tooltip("Musique du jeu Bubble Shooter.")]
    public AudioClip bubbleMusic;

    [Tooltip("Musique du jeu Parry Game.")]
    public AudioClip parryMusic;

    [Tooltip("Musique du jeu TiltBall (fightgame music).")]
    public AudioClip tiltBallMusic;

    [Header("Effets sonores")]
    [Tooltip("Son joué au clic sur n'importe quel bouton UI.")]
    public AudioClip clickSfx;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.65f;
    [Range(0f, 1f)] public float sfxVolume   = 1.00f;

    [Header("Fondu")]
    [Tooltip("Durée du fondu enchaîné entre deux musiques.")]
    public float crossfadeDuration = 1.2f;

    // ── Internals ─────────────────────────────────────────────────────────────

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private Coroutine fadeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Source musique (boucle)
        musicSource          = GetComponent<AudioSource>();
        musicSource.loop     = true;
        musicSource.volume   = 0f;
        musicSource.playOnAwake = false;

        // Source SFX (one-shot)
        sfxSource            = gameObject.AddComponent<AudioSource>();
        sfxSource.loop       = false;
        sfxSource.playOnAwake = false;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Joue une musique en fondu enchaîné si elle est différente de l'actuelle.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossfadeRoutine(clip));
    }

    /// <summary>Joue le son de clic UI. Peut être appelé depuis n'importe quel contexte.</summary>
    public static void PlayClick()
    {
        if (Instance == null || Instance.clickSfx == null) return;
        Instance.sfxSource.PlayOneShot(Instance.clickSfx, Instance.sfxVolume);
    }

    /// <summary>Joue un effet sonore ponctuel.</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // ── Fondu enchaîné ────────────────────────────────────────────────────────

    private IEnumerator CrossfadeRoutine(AudioClip nextClip)
    {
        float startVol = musicSource.volume;
        float elapsed  = 0f;
        float half     = crossfadeDuration * 0.5f;

        // Fade out de la musique actuelle
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / half);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip   = nextClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in de la nouvelle musique
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / half);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }
}
