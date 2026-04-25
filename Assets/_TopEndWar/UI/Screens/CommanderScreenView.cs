using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class CommanderScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _titleText;
        TMP_Text _summaryText;
        TMP_Text _tabContentText;
        TMP_Text _statusText;
        Transform _slotContainer;
        readonly List<GameObject> _slotObjects = new List<GameObject>();
        int _upgradeBonus;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            CommanderScreenData data = _dataProvider.GetCommanderScreenData();
            _titleText.text = $"{UILocalization.Get("commander.header.title", "COMMANDER / EQUIPMENT")}  {data.commanderName}";
            _summaryText.text = $"POWER  {data.totalPower + _upgradeBonus:N0}\nHP  {data.hp:N0}   DPS  {data.dps:N0}   DEF  {data.defense:N0}\n{data.roleDescription}";
            _tabContentText.text = "Loadout focus: frontline pressure, armor sustain, and weapon uptime.";
            BindSlots(data.slots);
        }

        void Build()
        {
            if (_titleText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 16f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(18f);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 140f, minHeight: 140f);
            _titleText = UIFactory.CreateText("Title", header.ContentRoot, string.Empty, 30, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_titleText, 44f, true, 18f, 30f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 560f);

            GameObject scrollGo = UIFactory.CreateUIObject("ScrollView", contentArea.transform);
            UIFactory.Stretch(scrollGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject viewport = UIFactory.CreateUIObject("Viewport", scrollGo.transform);
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            UIFactory.Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            GameObject content = UIFactory.CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.content = contentRect;

            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(content, 16f, TextAnchor.UpperCenter, true, false);
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PanelBaseView summaryPanel = CreateSection(content.transform, "PowerSummary", 160f, 150f);
            _summaryText = UIFactory.CreateText("Summary", summaryPanel.ContentRoot, string.Empty, 20, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_summaryText, 110f, true, 15f, 20f);

            PanelBaseView visual = CreateSection(content.transform, "CommanderVisual", 260f, 240f);
            TMP_Text visualText = UIFactory.CreateText("VisualText", visual.ContentRoot, "TACTICAL COMMAND DIORAMA\nCommander silhouette placeholder\nUpgrade focus: armor, rifle uptime, squad sustain", 22, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(visualText.rectTransform, Vector2.zero, Vector2.zero);
            visualText.enableAutoSizing = true;
            visualText.fontSizeMin = 18f;
            visualText.fontSizeMax = 22f;

            PanelBaseView squad = CreateSection(content.transform, "SquadPanel", 150f, 140f);
            TMP_Text squadText = UIFactory.CreateText("SquadText", squad.ContentRoot, "SQUAD STRIP\nAlpha Squad\nBulwark Team\nMedic Pair\nDrone Crew", 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(squadText, 110f, true, 15f, 22f);

            PanelBaseView tabs = CreateSection(content.transform, "TabsPanel", 180f, 170f);
            GameObject tabsRow = UIFactory.CreateUIObject("TabsRow", tabs.ContentRoot);
            HorizontalLayoutGroup tabsLayout = UIFactory.AddHorizontalLayout(tabsRow, 12f, TextAnchor.MiddleCenter, true, false);
            tabsLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(tabsRow, preferredHeight: 64f, minHeight: 64f);
            CreateTabButton(tabsRow.transform, "commander.loadout_tab", "Loadout focus: frontline pressure, armor sustain, and weapon uptime.");
            CreateTabButton(tabsRow.transform, "commander.skills_tab", "Skill focus: suppression burst, emergency shield, and drone command.");
            CreateTabButton(tabsRow.transform, "commander.stats_tab", "Stat focus: HP, DPS, DEF, fire rate, and squad reinforcement tempo.");
            _tabContentText = UIFactory.CreateText("TabContent", tabs.ContentRoot, string.Empty, 20, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_tabContentText, 72f, true, 15f, 20f);

            PanelBaseView slots = CreateSection(content.transform, "SlotsPanel", 760f, 520f);
            TMP_Text slotsTitle = UIFactory.CreateText("SlotsTitle", slots.ContentRoot, "EQUIPMENT SLOTS", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(slotsTitle, 24f);
            _slotContainer = UIFactory.CreateUIObject("SlotContainer", slots.ContentRoot).transform;
            VerticalLayoutGroup slotLayout = UIFactory.AddVerticalLayout(_slotContainer.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            slotLayout.childForceExpandHeight = false;

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(18f);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 150f, minHeight: 150f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;
            _statusText = UIFactory.CreateText("StatusText", footer.ContentRoot, "Upgrade console ready.", 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 26f, true, 15f, 18f);
            GameObject actions = UIFactory.CreateUIObject("ActionsRow", footer.ContentRoot);
            VerticalLayoutGroup actionsLayout = UIFactory.AddVerticalLayout(actions, 10f, TextAnchor.MiddleCenter, true, false);
            actionsLayout.childForceExpandHeight = false;
            CreateActionButton(actions.transform, LocalizationKeys.CommanderUpgrade, ApplyUpgrade, ButtonVisualStyle.Primary);
            CreateActionButton(actions.transform, "commander.auto_equip", ApplyAutoEquip, ButtonVisualStyle.Secondary);
        }

        void ApplyUpgrade()
        {
            _upgradeBonus += 100;
            _statusText.text = $"Upgrade applied. Commander power +{_upgradeBonus:N0}.";
            _screenManager.ActionRouter.ApplyCommanderUpgrade();
            RefreshView();
        }

        void ApplyAutoEquip()
        {
            _tabContentText.text = "Auto Equip preview: strongest available gear slotted by mock rules.";
            _statusText.text = "Best gear equipped";
            _screenManager.ActionRouter.ApplyAutoEquip();
        }

        PanelBaseView CreateSection(Transform parent, string name, float preferredHeight, float minHeight)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(18f);
            UIFactory.AddLayoutElement(panel.gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.UpperLeft, true, false);
            layout.childForceExpandHeight = false;
            return panel;
        }

        void BindSlots(List<EquipmentSlotData> slots)
        {
            while (_slotObjects.Count < slots.Count)
            {
                GameObject slotGo = UIFactory.CreateUIObject("Slot", _slotContainer);
                slotGo.AddComponent<EquipmentSlotView>();
                _slotObjects.Add(slotGo);
            }

            for (int i = 0; i < _slotObjects.Count; i++)
            {
                bool active = i < slots.Count;
                _slotObjects[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                _slotObjects[i].GetComponent<EquipmentSlotView>().Bind(slots[i]);
            }
        }

        void CreateTabButton(Transform parent, string key, string content)
        {
            GameObject buttonGo = UIFactory.CreateUIObject($"{key}_Tab", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 60f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, key);
            button.SetOnClick(() => _tabContentText.text = content);
        }

        void CreateActionButton(Transform parent, string key, UnityEngine.Events.UnityAction action, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject($"{key}_Action", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 64f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            button.SetLabelKey(key, key);
            button.SetOnClick(action);
        }
    }
}
