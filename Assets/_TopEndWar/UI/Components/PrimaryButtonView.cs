using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public enum ButtonVisualStyle
    {
        Primary,
        Secondary,
        Danger,
        Tab
    }

    public class PrimaryButtonView : MonoBehaviour
    {
        Button _button;
        Image _background;
        Image _icon;
        TMP_Text _label;
        ButtonVisualStyle _currentStyle;

        public void Build(ButtonVisualStyle style = ButtonVisualStyle.Primary)
        {
            _currentStyle = style;
            _background = UIFactory.GetOrAdd<Image>(gameObject);
            _button = UIFactory.GetOrAdd<Button>(gameObject);
            ApplyStyle(style);

            if (_label == null)
            {
                int fontSize = style == ButtonVisualStyle.Tab ? 18 : 24;
                _label = UIFactory.CreateText("Label", transform, string.Empty, fontSize, style == ButtonVisualStyle.Primary ? UITheme.DeepNavy : UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, new Vector2(12f, 10f), new Vector2(-12f, -10f));
            }

            _label.enableAutoSizing = true;
            _label.fontSizeMin = style == ButtonVisualStyle.Tab ? 16f : 22f;
            _label.fontSizeMax = style == ButtonVisualStyle.Tab ? 18f : 26f;
            _label.color = style == ButtonVisualStyle.Primary ? UITheme.DeepNavy : UITheme.SoftCream;
            float preferredHeight = style == ButtonVisualStyle.Primary ? 98f : style == ButtonVisualStyle.Tab ? 72f : 76f;
            float minHeight = style == ButtonVisualStyle.Primary ? 90f : style == ButtonVisualStyle.Tab ? 68f : 64f;
            UIFactory.AddLayoutElement(gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
        }

        void ApplyStyle(ButtonVisualStyle style)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = ResolveColor(style, false);
            colors.highlightedColor = ResolveColor(style, true);
            colors.pressedColor = ResolveColor(style, false) * 0.82f;
            colors.selectedColor = ResolveColor(style, true);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.75f);
            _button.colors = colors;
            _button.targetGraphic = _background;
            ApplyBackground(style, colors.normalColor);

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = style == ButtonVisualStyle.Danger ? UITheme.DangerDark : UITheme.MutedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public void SetLabelKey(string key, string fallback = null)
        {
            Build(_currentStyle);
            // LOCALIZATION: Buttons resolve through keys first, then fall back to readable text.
            _label.text = UILocalization.Get(key, fallback);
        }

        public void SetLabelText(string value)
        {
            Build(_currentStyle);
            _label.text = value;
        }

        public void SetOnClick(UnityAction action)
        {
            Build(_currentStyle);
            _button.onClick.RemoveAllListeners();
            if (action != null)
            {
                _button.onClick.AddListener(action);
            }
        }

        public void SetSelected(bool selected)
        {
            Build(_currentStyle);
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseBottomNavSprites && art != null && _currentStyle == ButtonVisualStyle.Tab)
            {
                UIArtLibrary.TryApply(_background, art.GetBottomNavSprite(selected), selected ? ResolveColor(_currentStyle, true) : ResolveColor(_currentStyle, false), selected ? "UI_BottomNav_Item_Active" : "UI_BottomNav_Item");
                return;
            }

            _background.sprite = null;
            _background.type = Image.Type.Simple;
            _background.color = selected ? ResolveColor(_currentStyle, true) : ResolveColor(_currentStyle, false);
        }

        public void SetIcon(Sprite sprite, string assetName)
        {
            Build(_currentStyle);
            if (_icon == null)
            {
                _icon = UIFactory.CreateUIObject("Icon", transform).AddComponent<Image>();
                _icon.raycastTarget = false;
                RectTransform iconRect = _icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(30f, 30f);
                iconRect.anchoredPosition = new Vector2(0f, -9f);
            }

            bool hasIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(_icon, sprite, Color.clear, assetName);
            _icon.enabled = hasIcon;
            if (_label != null)
            {
                UIFactory.Stretch(_label.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, hasIcon ? -34f : -8f));
            }
        }

        Color ResolveColor(ButtonVisualStyle style, bool active)
        {
            switch (style)
            {
                case ButtonVisualStyle.Secondary:
                    return active ? UITheme.Gunmetal : UITheme.NavyPanel;
                case ButtonVisualStyle.Danger:
                    return active ? UITheme.Danger : UITheme.DangerDark;
                case ButtonVisualStyle.Tab:
                    return active ? UITheme.TealDark : UITheme.Gunmetal;
                default:
                    return active ? UITheme.ButtonGoldTop : UITheme.ButtonGoldBottom;
            }
        }

        void ApplyBackground(ButtonVisualStyle style, Color fallbackColor)
        {
            if (!UIConstants.UseButtonSprites)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            if (style == ButtonVisualStyle.Tab)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            if (style == ButtonVisualStyle.Secondary && ShouldUseCompactFallback())
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = fallbackColor;
                return;
            }

            UIArtLibrary.TryApply(_background, ResolveSprite(style), fallbackColor, ResolveAssetName(style));
        }

        bool ShouldUseCompactFallback()
        {
            string objectName = gameObject.name.ToLowerInvariant();
            return objectName.Contains("quick")
                || objectName.Contains("back")
                || objectName.Contains("preview")
                || objectName.Contains("utility")
                || objectName.Contains("_tab");
        }

        Sprite ResolveSprite(ButtonVisualStyle style)
        {
            UIArtLibrary art = UIArtLibrary.Instance;
            if (art == null)
            {
                return null;
            }

            switch (style)
            {
                case ButtonVisualStyle.Primary:
                    return art.PrimaryButton;
                case ButtonVisualStyle.Tab:
                    return art.TabButton != null ? art.TabButton : art.BottomNavItem;
                default:
                    return art.SecondaryButton;
            }
        }

        string ResolveAssetName(ButtonVisualStyle style)
        {
            switch (style)
            {
                case ButtonVisualStyle.Primary:
                    return "UI_Button_Primary";
                case ButtonVisualStyle.Tab:
                    return "UI_Button_Tab";
                default:
                    return "UI_Button_Secondary";
            }
        }
    }
}
