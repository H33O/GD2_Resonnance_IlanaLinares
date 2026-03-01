using System.Collections;
using UnityEngine;

/// <summary>
/// Joue la musique en boucle et monte progressivement le volume au démarrage.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Musique")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float targetVolume = 0.7f;

    [Header("Fondu d'entrée")]
    [SerializeField] private float fadeInDuration = 3f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource       = GetComponent<AudioSource>();
        audioSource.clip  = musicClip;
        audioSource.loop  = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (musicClip == null)
        {
            Debug.LogWarning("[MusicManager] Aucun AudioClip assigné.");
            return;
        }

        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    /// <summary>Monte le volume de 0 à targetVolume sur fadeInDuration secondes.</summary>
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
