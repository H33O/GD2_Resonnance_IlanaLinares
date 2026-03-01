using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Peint les tuiles en bordure autour du cadre visible par la caméra orthographique.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class MenuTileBorder : MonoBehaviour
{
    [Header("Tuiles")]
    [SerializeField] private TileBase cornerTile;
    [SerializeField] private TileBase horizontalTile;
    [SerializeField] private TileBase verticalTile;

    [Header("Épaisseur (en tuiles)")]
    [SerializeField] private int borderThickness = 1;

    private Tilemap tilemap;

    private void Start()
    {
        tilemap = GetComponent<Tilemap>();
        PaintBorder();
    }

    /// <summary>Calcule les limites de la caméra et peint les tuiles en bordure.</summary>
    private void PaintBorder()
    {
        tilemap.ClearAllTiles();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[MenuTileBorder] Aucune MainCamera trouvée.");
            return;
        }

        if (!cam.orthographic)
        {
            Debug.LogWarning("[MenuTileBorder] La caméra doit être orthographique.");
            return;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3Int min = tilemap.WorldToCell(new Vector3(-halfW, -halfH, 0f));
        Vector3Int max = tilemap.WorldToCell(new Vector3( halfW,  halfH, 0f));

        for (int x = min.x - borderThickness; x <= max.x + borderThickness; x++)
        {
            for (int y = min.y - borderThickness; y <= max.y + borderThickness; y++)
            {
                bool insideH = x >= min.x && x <= max.x;
                bool insideV = y >= min.y && y <= max.y;

                // Ne dessine que les tuiles à l'extérieur ou dans la bande de bordure
                bool onBorderH = x < min.x || x > max.x;
                bool onBorderV = y < min.y || y > max.y;
                bool inThickness = !insideH || !insideV;

                if (!inThickness) continue;

                TileBase tile;
                if (onBorderH && onBorderV)
                    tile = cornerTile     != null ? cornerTile     : horizontalTile;
                else if (onBorderV)
                    tile = horizontalTile != null ? horizontalTile : cornerTile;
                else
                    tile = verticalTile   != null ? verticalTile   : cornerTile;

                if (tile != null)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }
}
