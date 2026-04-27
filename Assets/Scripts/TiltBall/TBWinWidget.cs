using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de résultat affiché à la fin d'un niveau TiltBall.
/// Typographie JimNightshade + fonds sprite jauge (TBUIStyle).
///
/// Deux modes :
///   <see cref="Show"/>        — fin de niveau intermédiaire (non utilisé directement, garde rétrocompat)
///   <see cref="ShowVictory"/> — victoire finale après les 10 niveaux, affiche XP ×2
/// </summary>
public class TBWinWidget : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay      = new Color(0f,    0f,    0f,    0.75f);
    private static readonly Color ColPanel        = new Color(0.05f, 0.04f, 0.11f, 0.97f);
    private static readonly Color ColTitleNormal  = new Color(1f,    0.85f, 0.10f, 1f);
    private static readonly Color ColTitleVictory = new Color(0.30f, 1.00f, 0.55f, 1f);
    private static readonly Color ColText         = Color.white;
    private static readonly Color ColRowBg        = new Color(1f,    1f,    1f,    0.04f);
    private static readonly Color ColBtnBg        = new Color(0.10f, 0.55f, 0.28f, 1f);
    private static readonly Color ColBtnText      = Color.white;
    private static readonly Color ColBarFill      = new Color(1f,    0.78f, 0.08f, 0.80f);
    private static readonly Color ColBarBg        = new Color(0f,    0f,    0f,    0.30f);
    private static readonly Color ColXpBg         = new Color(0.10f, 0.38f, 0.18f, 0.90f);
    private static readonly Color ColXpText       = new Color(0.30f, 1.00f, 0.45f, 1f);
    private static readonly Color ColXpBonus      = new Color(1.00f, 0.90f, 0.10f, 1f);

    // ── API statique ──────────────────────────────────────────────────────────

    /// <summary>Victoire finale — affiche score + XP ×2 accordée.</summary>
    public static void ShowVictory(float elapsedTime, int score,
                                   int totalXpGranted, int baseXp,
                                   Action onContinue)
    {
        var canvasGO = BuildCanvas();
        var widget   = canvasGO.AddComponent<TBWinWidget>();
        widget.BuildVictory(canvasGO.GetComponent<Canvas>().GetComponent<RectTransform>(),
                            elapsedTime, score, totalXpGranted, baseXp, onContinue, canvasGO);
    }

    /// <summary>Rétrocompatibilité — fin de niveau standard (non utilisé par le flux normal).</summary>
    public static void Show(float elapsedTime, int score, Action onContinue)
    {
        var canvasGO = BuildCanvas();
        var widget   = canvasGO.AddComponent<TBWinWidget>();
        widget.BuildNormal(canvasGO.GetComponent<Canvas>().GetComponent<RectTransform>(),
                           elapsedTime, score, onContinue, canvasGO);
    }

    // ── Canvas partagé ────────────────────────────────────────────────────────

    private static GameObject BuildCanvas()
    {
        var canvasGO = new GameObject("WinWidget");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    // ── Écran de victoire finale ──────────────────────────────────────────────

    private void BuildVictory(RectTransform root, float elapsedTime, int score,
                               int totalXp, int baseXp, Action onContinue, GameObject selfRoot)
    {
        // Overlay
        MakeJaugePanel("Overlay", root, Vector2.zero, Vector2.one, ColOverlay);

        // Panneau central (plus haut que le normal pour loger le bloc XP)
        var panel = MakeJaugePanel("Panel", root,
            new Vector2(0.07f, 0.22f), new Vector2(0.93f, 0.82f), ColPanel);

        // ── Titre VICTOIRE ────────────────────────────────────────────────────
        MakeTmp("Title", panel.rectTransform,
            "🏆  VICTOIRE !", 62f, ColTitleVictory, FontStyles.Bold,
            new Vector2(0f, 0.84f), new Vector2(1f, 0.99f), TextAlignmentOptions.Center);

        MakeTmp("SubTitle", panel.rectTransform,
            "10 niveaux complétés", 28f, new Color(1f, 1f, 1f, 0.55f), FontStyles.Normal,
            new Vector2(0f, 0.77f), new Vector2(1f, 0.86f), TextAlignmentOptions.Center);

        // ── Ligne Temps ───────────────────────────────────────────────────────
        BuildDataRow("RowTime", panel.rectTransform,
            new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.75f),
            "TEMPS", FormatTime(elapsedTime));

        // ── Ligne Score ───────────────────────────────────────────────────────
        BuildDataRow("RowScore", panel.rectTransform,
            new Vector2(0.04f, 0.47f), new Vector2(0.96f, 0.60f),
            "SCORE", $"{score} pts");

        // ── Barre de score ────────────────────────────────────────────────────
        BuildScoreBar("ScoreBar", panel.rectTransform,
            new Vector2(0.06f, 0.41f), new Vector2(0.94f, 0.47f),
            score);

        // ── Bloc XP ×2 ───────────────────────────────────────────────────────
        var xpPanel = MakeJaugePanel("XpPanel", panel.rectTransform,
            new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.39f), ColXpBg);

        MakeTmp("XpTitle", xpPanel.rectTransform,
            "XP ACCORDÉE", 24f, new Color(1f, 1f, 1f, 0.55f), FontStyles.Normal,
            new Vector2(0f, 0.58f), new Vector2(1f, 0.98f), TextAlignmentOptions.Center);

        MakeTmp("XpBase", xpPanel.rectTransform,
            $"{baseXp} XP", 30f, ColXpText, FontStyles.Bold,
            new Vector2(0f, 0.25f), new Vector2(0.42f, 0.62f), TextAlignmentOptions.Center);

        MakeTmp("XpMult", xpPanel.rectTransform,
            "×2", 52f, ColXpBonus, FontStyles.Bold,
            new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.85f), TextAlignmentOptions.Center);

        MakeTmp("XpTotal", xpPanel.rectTransform,
            $"= {totalXp} XP", 38f, ColXpText, FontStyles.Bold,
            new Vector2(0.58f, 0.10f), new Vector2(1f, 0.65f), TextAlignmentOptions.Center);

        // ── Bouton RETOUR AU MENU ─────────────────────────────────────────────
        MakeButton("BtnMenu", panel.rectTransform,
            "RETOUR AU MENU",
            new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.16f),
            () => { Destroy(selfRoot); onContinue?.Invoke(); });
    }

    // ── Écran intermédiaire (rétrocompat) ─────────────────────────────────────

    private void BuildNormal(RectTransform root, float elapsedTime, int score,
                              Action onContinue, GameObject selfRoot)
    {
        MakeJaugePanel("Overlay", root, Vector2.zero, Vector2.one, ColOverlay);

        var panel = MakeJaugePanel("Panel", root,
            new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.76f), ColPanel);

        MakeTmp("Title", panel.rectTransform,
            "NIVEAU TERMINÉ", 60f, ColTitleNormal, FontStyles.Bold,
            new Vector2(0f, 0.80f), new Vector2(1f, 0.98f), TextAlignmentOptions.Center);

        BuildDataRow("RowTime", panel.rectTransform,
            new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.75f),
            "TEMPS", FormatTime(elapsedTime));

        BuildDataRow("RowScore", panel.rectTransform,
            new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.50f),
            "SCORE", $"{score} pts");

        BuildScoreBar("ScoreBar", panel.rectTransform,
            new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.26f),
            score);

        MakeButton("BtnContinue", panel.rectTransform,
            "CONTINUER →",
            new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.16f),
            () => { Destroy(selfRoot); onContinue?.Invoke(); });
    }

    // ── Ligne de données (label + valeur) ─────────────────────────────────────

    private static void BuildDataRow(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, string labelTxt, string valueTxt)
    {
        var rowImg = MakeJaugePanel(name, parent, aMin, aMax, ColRowBg);

        MakeTmp($"{name}_Key", rowImg.rectTransform,
            labelTxt, 28f, new Color(1f, 1f, 1f, 0.55f), FontStyles.Normal,
            new Vector2(0.03f, 0f), new Vector2(0.48f, 1f),
            TextAlignmentOptions.MidlineLeft);

        MakeTmp($"{name}_Val", rowImg.rectTransform,
            valueTxt, 34f, new Color(1f, 0.85f, 0.10f, 1f), FontStyles.Bold,
            new Vector2(0.50f, 0f), new Vector2(0.97f, 1f),
            TextAlignmentOptions.MidlineRight);
    }

    // ── Barre de score ────────────────────────────────────────────────────────

    private static void BuildScoreBar(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, int score)
    {
        MakeJaugePanel($"{name}Bg", parent, aMin, aMax, ColBarBg);

        var fillGO  = new GameObject($"{name}Fill");
        fillGO.transform.SetParent(parent, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.raycastTarget = false;
        float fill  = Mathf.Clamp01(score / 1000f);
        TBUIStyle.ApplyJaugeFill(fillImg, ColBarFill, fill);
        var fillRT  = fillImg.rectTransform;
        fillRT.anchorMin = aMin;
        fillRT.anchorMax = aMax;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
    }

    // ── Bouton ────────────────────────────────────────────────────────────────

    private static void MakeButton(string name, RectTransform parent,
        string label, Vector2 aMin, Vector2 aMax, Action onClick)
    {
        var btnImg = MakeJaugePanel(name, parent, aMin, aMax, ColBtnBg);

        var btn = btnImg.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        MakeTmp($"{name}_Lbl", btnImg.rectTransform,
            label, 42f, ColBtnText, FontStyles.Bold,
            Vector2.zero, Vector2.one,
            TextAlignmentOptions.Center);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Image MakeJaugePanel(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        TBUIStyle.ApplyJauge(img, color);
        var rt  = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    private static TextMeshProUGUI MakeTmp(string name, RectTransform parent,
        string text, float fontSize, Color color, FontStyles style,
        Vector2 aMin, Vector2 aMax, TextAlignmentOptions align)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.fontStyle  = style;
        tmp.alignment  = align;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        TBUIStyle.ApplyFont(tmp);
        var rt         = tmp.rectTransform;
        rt.anchorMin   = aMin; rt.anchorMax = aMax;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private static string FormatTime(float t)
    {
        int   m  = Mathf.FloorToInt(t / 60f);
        int   s  = Mathf.FloorToInt(t % 60f);
        float ms = (t % 1f) * 100f;
        return $"{m:00}:{s:00}.{(int)ms:00}";
    }
}
