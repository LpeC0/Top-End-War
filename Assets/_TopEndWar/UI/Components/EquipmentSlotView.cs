using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;

namespace TopEndWar.UI.Components
{
    public class EquipmentSlotView : MonoBehaviour
    {
        TMP_Text _slotName;
        TMP_Text _itemName;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(14f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(gameObject, preferredHeight: 100f, minHeight: 92f);
            UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 4f, TextAnchor.MiddleLeft, true, false);

            if (_slotName == null)
            {
                _slotName = UIFactory.CreateText("SlotName", panel.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Bold);
                _itemName = UIFactory.CreateText("ItemName", panel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
                UIFactory.ConfigureTextBlock(_slotName, 28f, true, 14f, 18f);
                UIFactory.ConfigureTextBlock(_itemName, 42f, true, 16f, 22f);
            }

            _isBuilt = true;
        }

        public void Bind(EquipmentSlotData data)
        {
            Build();
            _slotName.text = UILocalization.Get(data.slotKey, data.slotKey);
            _itemName.text = data.itemName;

            switch (data.state)
            {
                case "upgradeable":
                    _itemName.color = UITheme.Teal;
                    break;
                case "locked":
                    _itemName.color = UITheme.Danger;
                    break;
                case "empty":
                    _itemName.color = UITheme.Amber;
                    break;
                default:
                    _itemName.color = UITheme.SoftCream;
                    break;
            }
        }
    }
}
