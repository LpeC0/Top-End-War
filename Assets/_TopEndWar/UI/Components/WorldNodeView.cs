using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class WorldNodeView : MonoBehaviour
    {
        Button _button;
        Image _background;
        TMP_Text _label;
        LayoutElement _layoutElement;

        public void Build()
        {
            _background = UIFactory.GetOrAdd<Image>(gameObject);
            _button = UIFactory.GetOrAdd<Button>(gameObject);
            _button.targetGraphic = _background;
            _layoutElement = UIFactory.GetOrAdd<LayoutElement>(gameObject);
            _layoutElement.preferredWidth = 64f;
            _layoutElement.preferredHeight = 64f;

            if (_label == null)
            {
                _label = UIFactory.CreateText("StageLabel", transform, string.Empty, 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, Vector2.zero, Vector2.zero);
                _label.enableAutoSizing = true;
                _label.fontSizeMin = 20f;
                _label.fontSizeMax = 28f;
            }
        }

        public void Bind(StageNodeData data, UnityAction onClick)
        {
            Build();
            _label.text = data.stageId.ToString();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
            float size = data.isCurrent ? 104f : data.isBoss ? 92f : 64f;
            _layoutElement.preferredWidth = size;
            _layoutElement.preferredHeight = size;
            ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            _label.fontSizeMin = 20f;
            _label.fontSizeMax = data.isCurrent ? 28f : 24f;

            if (data.isBoss)
            {
                _background.color = UITheme.Danger;
                _label.color = UITheme.SoftCream;
            }
            else if (data.isCurrent)
            {
                _background.color = UITheme.Teal;
                _label.color = UITheme.DeepNavy;
            }
            else if (data.isCompleted)
            {
                _background.color = UITheme.ButtonGoldTop;
                _label.color = UITheme.DeepNavy;
            }
            else if (data.isLocked)
            {
                _background.color = UITheme.Gunmetal;
                _label.color = UITheme.TextSecondary;
            }
            else
            {
                _background.color = UITheme.WarmCream;
                _label.color = UITheme.DeepNavy;
            }
        }
    }
}
