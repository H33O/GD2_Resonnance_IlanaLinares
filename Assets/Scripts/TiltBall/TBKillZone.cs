using UnityEngine;

/// <summary>
/// Trigger invisible placé hors des murs physiques aux niveaux double-slide (≥ 4).
/// Si le joueur le touche (débordement sous fort momentum), déclenche sa mort
/// et redémarre le niveau — même comportement qu'un contact ennemi.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TBKillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<TBPlayerController>();
        player?.Die();
    }
}
