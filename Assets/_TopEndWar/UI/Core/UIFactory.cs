using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Core
{
    public static class UIFactory
    {
        public static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(RectTransform rectTransform, Vector2 paddingMin, Vector2 paddingMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = paddingMin;
            rectTransform.offsetMax = paddingMax;
            return rectTransform;
        }

        public static RectTransform SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            return rectTransform;
        }

        public static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            GameObject go = CreateUIObject(name, parent);
            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            // READABILITY: Use truncate because the current TMP font asset lacks the ellipsis glyph.
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing, TextAnchor anchor = TextAnchor.MiddleLeft, bool expandWidth = true, bool expandHeight = false)
        {
            HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(go);
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return layout;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing, TextAnchor anchor = TextAnchor.UpperLeft, bool expandWidth = true, bool expandHeight = false)
        {
            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return layout;
        }

        public static ContentSizeFitter AddContentSizeFitter(GameObject go, ContentSizeFitter.FitMode horizontal, ContentSizeFitter.FitMode vertical)
        {
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
            fitter.horizontalFit = horizontal;
            fitter.verticalFit = vertical;
            return fitter;
        }

        public static LayoutElement AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f, float minWidth = -1f, float minHeight = -1f)
        {
            LayoutElement element = GetOrAdd<LayoutElement>(go);
            if (preferredWidth >= 0f)
            {
                element.preferredWidth = preferredWidth;
            }

            if (preferredHeight >= 0f)
            {
                element.preferredHeight = preferredHeight;
            }

            if (flexibleWidth >= 0f)
            {
                element.flexibleWidth = flexibleWidth;
            }

            if (flexibleHeight >= 0f)
            {
                element.flexibleHeight = flexibleHeight;
            }

            if (minWidth >= 0f)
            {
                element.minWidth = minWidth;
            }

            if (minHeight >= 0f)
            {
                element.minHeight = minHeight;
            }

            return element;
        }

        public static void ConfigureTextBlock(TMP_Text text, float preferredHeight, bool autoSize = false, float minFontSize = 16f, float maxFontSize = 32f)
        {
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            // READABILITY: Use truncate because the current TMP font asset lacks the ellipsis glyph.
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = autoSize;
            if (autoSize)
            {
                text.fontSizeMin = minFontSize;
                text.fontSizeMax = maxFontSize;
            }

            AddLayoutElement(text.gameObject, preferredHeight: preferredHeight, minHeight: preferredHeight * 0.75f);
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
