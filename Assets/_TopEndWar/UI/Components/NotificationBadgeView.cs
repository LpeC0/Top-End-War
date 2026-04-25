using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class NotificationBadgeView : MonoBehaviour
    {
        TMP_Text _countText;

        public void Build()
        {
            Image image = UIFactory.GetOrAdd<Image>(gameObject);
            image.color = UITheme.Danger;
            LayoutElement layout = UIFactory.GetOrAdd<LayoutElement>(gameObject);
            layout.preferredWidth = 28f;
            layout.preferredHeight = 28f;

            if (_countText == null)
            {
                _countText = UIFactory.CreateText("Count", transform, "0", 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                RectTransform rect = _countText.rectTransform;
                UIFactory.Stretch(rect, Vector2.zero, Vector2.zero);
            }
        }

        public void SetCount(int count)
        {
            Build();
            gameObject.SetActive(count > 0);
            _countText.text = count.ToString();
        }
    }
}
