using UnityEngine;

public enum BubbleColor { Red, Blue, Green, Yellow, Purple }

public static class BubbleColorExtensions
{
    private static readonly Color[] Colors =
    {
        new Color(0.9f, 0.2f, 0.2f),
        new Color(0.2f, 0.45f, 0.9f),
        new Color(0.2f, 0.8f, 0.3f),
        new Color(1.0f, 0.85f, 0.1f),
        new Color(0.7f, 0.2f, 0.9f)
    };

    public static Color ToUnityColor(this BubbleColor c) => Colors[(int)c];

    public static BubbleColor Random(int maxColors = 4) =>
        (BubbleColor)UnityEngine.Random.Range(0, Mathf.Clamp(maxColors, 1, Colors.Length));
}
