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
            _label.fontSizeMax = style == ButtonVisualStyle.Tab ? 18f : 24f;
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
            _background.color = colors.normalColor;

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
            _background.color = selected ? ResolveColor(_currentStyle, true) : ResolveColor(_currentStyle, false);
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
    }
}
