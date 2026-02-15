using UnityEngine;

public class GameSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupPlayerSprite();
    }

    private void SetupPlayerSprite()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = SpriteGenerator.CreateWhiteSquare();
            }
        }
    }
}
