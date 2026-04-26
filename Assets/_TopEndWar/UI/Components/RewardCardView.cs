using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class RewardCardView : MonoBehaviour
    {
        TMP_Text _label;
        TMP_Text _amount;
        Image _icon;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(14f, UIConstants.UseRewardFrameSprites ? PanelVisualStyle.Cream : PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(gameObject, preferredWidth: 220f, preferredHeight: 128f, minWidth: 180f, minHeight: 120f);
            UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 6f, TextAnchor.MiddleCenter, false, false);

            if (_label == null)
            {
                _label = UIFactory.CreateText("Label", panel.ContentRoot, string.Empty, 20, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
                _amount = UIFactory.CreateText("Amount", panel.ContentRoot, string.Empty, 32, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                // READABILITY: Reward card labels should not drop into tiny prototype-sized text.
                UIFactory.ConfigureTextBlock(_label, 40f, true, 18f, 20f);
                UIFactory.ConfigureTextBlock(_amount, 52f, true, 20f, 32f);
            }

            if (_icon == null)
            {
                _icon = UIFactory.CreateUIObject("RewardIcon", transform).AddComponent<Image>();
                _icon.raycastTarget = false;
                RectTransform iconRect = _icon.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(52f, 52f);
                iconRect.anchoredPosition = new Vector2(18f, 0f);
            }

            _isBuilt = true;
        }

        public void Bind(RewardItemData data)
        {
            Build();
            _label.text = UILocalization.Get(data.labelKey, data.fallbackLabel);
            _amount.text = $"+{data.amount:N0}";
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseIconSprites && art != null)
            {
                UIArtLibrary.TryApply(_icon, art.GetRewardSprite(data.labelKey, data.fallbackLabel, data.accent), Color.clear, art.GetRewardAssetName(data.labelKey, data.fallbackLabel, data.accent));
            }
            else if (_icon != null)
            {
                _icon.enabled = false;
            }

            Color accent = UITheme.MutedGold;
            switch (data.accent)
            {
                case "teal":
                    accent = UITheme.Teal;
                    break;
                case "purple":
                    accent = UITheme.EpicPurple;
                    break;
                case "danger":
                    accent = UITheme.Danger;
                    break;
            }

            _amount.color = accent;
        }
    }
}
