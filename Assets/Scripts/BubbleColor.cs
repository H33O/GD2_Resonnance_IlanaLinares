using UnityEngine;

public enum BubbleColor { Red, Blue, Green, Yellow, Purple }

public static class BubbleColorExtensions
{
    /// <summary>
    /// Palette désaturée DA menu : blanc cassé et gris clairs, pas de couleurs criardes.
    /// </summary>
    private static readonly Color[] Colors =
    {
        new Color(0.85f, 0.82f, 0.80f, 1f),   // gris chaud (ex-rouge)
        new Color(0.78f, 0.82f, 0.88f, 1f),   // gris bleuté (ex-bleu)
        new Color(0.80f, 0.86f, 0.80f, 1f),   // gris verdâtre (ex-vert)
        new Color(0.90f, 0.88f, 0.82f, 1f),   // blanc crème (ex-jaune)
        new Color(0.84f, 0.80f, 0.86f, 1f),   // gris mauve (ex-violet)
    };

    public static Color ToUnityColor(this BubbleColor c) => Colors[(int)c];

    public static BubbleColor Random(int maxColors = 4) =>
        (BubbleColor)UnityEngine.Random.Range(0, Mathf.Clamp(maxColors, 1, Colors.Length));
}
