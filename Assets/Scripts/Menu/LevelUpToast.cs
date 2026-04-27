using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Toast éphémère affiché quand le joueur monte de niveau.
///
/// Apparaît centré en haut avec "NIVEAU +1 !" en bleu ciel,
/// monte doucement et fade out en 2.2 s.
/// Créé dynamiquement via <see cref="Show"/>.
/// </summary>
public static class LevelUpToast
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg   = new Color(0.06f, 0.12f, 0.28f, 0.95f);
    private static readonly Color ColText = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColSub  = new Color(1.00f, 1.00f, 1.00f, 0.60f);

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float RisePx  = 80f;
    private const float TotalDur = 2.20f;
    private const float FadeIn   = 0.20f;
    private const float Hold     = 1.40f;
    private const float FadeOut  = 0.60f;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Affiche le toast "NIVEAU <paramref name="newLevel"/> !" dans le canvas donné.</summary>
    public static void Show(RectTransform canvasRT, int newLevel)
    {
        var host = new GameObject("LevelUpToast");
        host.transform.SetParent(canvasRT, false);
        var mono = host.AddComponent<ToastMono>();
        mono.Run(canvasRT, newLevel);
    }

    // ── MonoBehaviour interne ─────────────────────────────────────────────────

    private class ToastMono : MonoBehaviour
    {
        public void Run(RectTransform canvasRT, int level)
            => StartCoroutine(Sequence(canvasRT, level));

        private IEnumerator Sequence(RectTransform canvasRT, int level)
        {
            // ── Construction ──────────────────────────────────────────────────

            var cg        = gameObject.AddComponent<CanvasGroup>();
            cg.alpha      = 0f;
            cg.blocksRaycasts = false;

            var rt        = gameObject.GetComponent<RectTransform>();
            rt.anchorMin  = new Vector2(0.5f, 0.80f);
            rt.anchorMax  = new Vector2(0.5f, 0.80f);
            rt.pivot      = new Vector2(0.5f, 0.5f);
            rt.sizeDelta  = new Vector2(640f, 140f);
            rt.anchoredPosition = Vector2.zero;

            // Fond
            var bgGO  = new GameObject("Bg");
            bgGO.transform.SetParent(rt, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
            bgImg.color  = ColBg;
            bgImg.raycastTarget = false;
            var bgRT  = bgImg.rectTransform;
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Texte principal
            var tGO  = new GameObject("MainText");
            tGO.transform.SetParent(rt, false);
            var tTmp = tGO.AddComponent<TextMeshProUGUI>();
            tTmp.text      = $"NIVEAU {level} !";
            tTmp.fontSize  = 64f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color     = ColText;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;
            MenuAssets.ApplyFont(tTmp);
            var tRT  = tTmp.rectTransform;
            tRT.anchorMin = new Vector2(0f, 0.45f);
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;

            // Sous-texte
            var sGO  = new GameObject("SubText");
            sGO.transform.SetParent(rt, false);
            var sTmp = sGO.AddComponent<TextMeshProUGUI>();
            sTmp.text      = "niveau supérieur !";
            sTmp.fontSize  = 26f;
            sTmp.color     = ColSub;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.raycastTarget = false;
            MenuAssets.ApplyFont(sTmp);
            var sRT  = sTmp.rectTransform;
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = new Vector2(1f, 0.48f);
            sRT.offsetMin = sRT.offsetMax = Vector2.zero;

            // ── Animation ─────────────────────────────────────────────────────

            Vector2 startPos  = rt.anchoredPosition;
            Vector2 targetPos = startPos + Vector2.up * RisePx;
            float   elapsed   = 0f;

            // Fade in
            while (elapsed < FadeIn)
            {
                elapsed += Time.deltaTime;
                float n  = Mathf.Clamp01(elapsed / FadeIn);
                cg.alpha = n;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, n * 0.3f);
                yield return null;
            }
            cg.alpha = 1f;

            // Hold + montée douce
            elapsed = 0f;
            while (elapsed < Hold)
            {
                elapsed += Time.deltaTime;
                float n  = Mathf.Clamp01(elapsed / Hold);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, 0.3f + n * 0.7f);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < FadeOut)
            {
                elapsed += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(elapsed / FadeOut);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
