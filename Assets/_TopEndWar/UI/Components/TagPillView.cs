using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class TagPillView : MonoBehaviour
    {
        TMP_Text _label;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            Image image = UIFactory.GetOrAdd<Image>(gameObject);
            image.color = UITheme.TealDark;

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = UITheme.Teal;
            outline.effectDistance = new Vector2(1f, -1f);

            if (_label == null)
            {
                _label = UIFactory.CreateText("Label", transform, string.Empty, 18, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(_label.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
                _label.enableAutoSizing = true;
                _label.fontSizeMin = 12f;
                _label.fontSizeMax = 18f;
            }

            UIFactory.AddLayoutElement(gameObject, preferredHeight: 42f, minHeight: 38f, minWidth: 72f);
            UIFactory.AddContentSizeFitter(gameObject, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize);
            _isBuilt = true;
        }

        public void SetLabel(string value, bool danger = false)
        {
            Build();
            _label.text = value;
            GetComponent<Image>().color = danger ? UITheme.DangerDark : UITheme.TealDark;
        }
    }
}
