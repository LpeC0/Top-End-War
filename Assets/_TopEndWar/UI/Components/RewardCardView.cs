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
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(14f);
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

            _isBuilt = true;
        }

        public void Bind(RewardItemData data)
        {
            Build();
            _label.text = UILocalization.Get(data.labelKey, data.fallbackLabel);
            _amount.text = $"+{data.amount:N0}";

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
