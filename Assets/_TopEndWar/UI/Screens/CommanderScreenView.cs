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
        TMP_Text _commanderPlaceholderText;
        Image _commanderFullImage;
        Transform _slotContainer;
        Transform _leftSlotContainer;
        Transform _rightSlotContainer;
        Transform _reserveSlotContainer;
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
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(16f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 110f, minHeight: 104f);
            _titleText = UIFactory.CreateText("Title", header.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_titleText.rectTransform, Vector2.zero, Vector2.zero);
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = 24f;
            _titleText.fontSizeMax = 38f;

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 780f);

            PanelBaseView commanderBoard = UIFactory.CreateUIObject("CommanderMainBoard", contentArea.transform).AddComponent<PanelBaseView>();
            commanderBoard.Build(24f, PanelVisualStyle.Hero);
            UIFactory.Stretch(commanderBoard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            PanelBaseView summaryPanel = CreateAnchoredPanel(commanderBoard.ContentRoot, "PowerSummary", new Vector2(0.18f, 1f), new Vector2(0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 118f), new Vector2(0f, -2f), PanelVisualStyle.Cream, 16f);
            _summaryText = UIFactory.CreateText("Summary", summaryPanel.ContentRoot, string.Empty, 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_summaryText.rectTransform, Vector2.zero, Vector2.zero);
            _summaryText.enableAutoSizing = true;
            _summaryText.fontSizeMin = 18f;
            _summaryText.fontSizeMax = 26f;

            PanelBaseView visual = CreateAnchoredPanel(commanderBoard.ContentRoot, "CommanderHeroArea", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 650f), new Vector2(0f, -130f), PanelVisualStyle.Cream, 18f);
            Image visualGlow = UIFactory.CreateImage("HeroGlow", visual.ContentRoot, new Color(0.96f, 0.78f, 0.42f, 0.12f));
            UIFactory.Stretch(visualGlow.rectTransform, new Vector2(210f, 28f), new Vector2(-210f, -28f));
            visualGlow.raycastTarget = false;

            GameObject leftSlots = UIFactory.CreateUIObject("LeftEquipmentSlots", visual.ContentRoot);
            UIFactory.SetAnchors(leftSlots.GetComponent<RectTransform>(), new Vector2(0f, 0.12f), new Vector2(0.26f, 0.92f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup leftLayout = UIFactory.AddVerticalLayout(leftSlots, 14f, TextAnchor.MiddleCenter, true, false);
            leftLayout.childForceExpandHeight = false;
            _leftSlotContainer = leftSlots.transform;

            GameObject rightSlots = UIFactory.CreateUIObject("RightEquipmentSlots", visual.ContentRoot);
            UIFactory.SetAnchors(rightSlots.GetComponent<RectTransform>(), new Vector2(0.74f, 0.12f), new Vector2(1f, 0.92f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rightLayout = UIFactory.AddVerticalLayout(rightSlots, 14f, TextAnchor.MiddleCenter, true, false);
            rightLayout.childForceExpandHeight = false;
            _rightSlotContainer = rightSlots.transform;

            _commanderFullImage = UIFactory.CreateUIObject("CommanderFullArt", visual.ContentRoot).AddComponent<Image>();
            _commanderFullImage.preserveAspect = true;
            _commanderFullImage.raycastTarget = false;
            RectTransform commanderRect = _commanderFullImage.rectTransform;
            commanderRect.anchorMin = new Vector2(0.5f, 0.5f);
            commanderRect.anchorMax = new Vector2(0.5f, 0.5f);
            commanderRect.pivot = new Vector2(0.5f, 0.5f);
            commanderRect.sizeDelta = new Vector2(430f, 550f);
            commanderRect.anchoredPosition = new Vector2(0f, -28f);
            UIArtLibrary art = UIArtLibrary.Instance;
            bool hasCommanderArt = UIConstants.UseCommanderSprites && UIArtLibrary.TryApply(_commanderFullImage, art != null ? art.CommanderFull : null, Color.clear, "Commander_Full_01");
            _commanderFullImage.enabled = hasCommanderArt;
            _commanderPlaceholderText = UIFactory.CreateText("VisualText", visual.ContentRoot, "COMMANDER DIORAMA\nUpgrade armor, rifle uptime, and squad sustain", 24, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(_commanderPlaceholderText.rectTransform, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _commanderPlaceholderText.enableAutoSizing = true;
            _commanderPlaceholderText.fontSizeMin = 18f;
            _commanderPlaceholderText.fontSizeMax = 26f;
            _commanderPlaceholderText.gameObject.SetActive(!hasCommanderArt);

            PanelBaseView tabs = CreateAnchoredPanel(commanderBoard.ContentRoot, "TabsPanel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 152f), new Vector2(0f, 306f), PanelVisualStyle.Dark, 14f);
            VerticalLayoutGroup tabsPanelLayout = UIFactory.AddVerticalLayout(tabs.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            tabsPanelLayout.childForceExpandHeight = false;
            GameObject tabsRow = UIFactory.CreateUIObject("TabsRow", tabs.ContentRoot);
            HorizontalLayoutGroup tabsLayout = UIFactory.AddHorizontalLayout(tabsRow, 12f, TextAnchor.MiddleCenter, true, false);
            tabsLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(tabsRow, preferredHeight: 62f, minHeight: 58f);
            CreateTabButton(tabsRow.transform, "commander.loadout_tab", "Loadout focus: frontline pressure, armor sustain, and weapon uptime.");
            CreateTabButton(tabsRow.transform, "commander.skills_tab", "Skill focus: suppression burst, emergency shield, and drone command.");
            CreateTabButton(tabsRow.transform, "commander.stats_tab", "Stat focus: HP, DPS, DEF, fire rate, and squad reinforcement tempo.");
            _tabContentText = UIFactory.CreateText("TabContent", tabs.ContentRoot, string.Empty, 19, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_tabContentText, 56f, true, 15f, 20f);

            PanelBaseView squad = CreateAnchoredPanel(commanderBoard.ContentRoot, "SquadPanel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(0f, 140f), PanelVisualStyle.Cream, 14f);
            VerticalLayoutGroup squadLayout = UIFactory.AddVerticalLayout(squad.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            squadLayout.childForceExpandHeight = false;
            TMP_Text squadTitle = UIFactory.CreateText("SquadTitle", squad.ContentRoot, "SQUAD", 22, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(squadTitle, 28f);
            GameObject squadRow = UIFactory.CreateUIObject("SquadRow", squad.ContentRoot);
            UIFactory.AddLayoutElement(squadRow, preferredHeight: 94f, minHeight: 88f);
            HorizontalLayoutGroup squadRowLayout = UIFactory.AddHorizontalLayout(squadRow, 10f, TextAnchor.MiddleCenter, true, false);
            squadRowLayout.childForceExpandHeight = false;
            CreateSquadCard(squadRow.transform, "Alpha", "20");
            CreateSquadCard(squadRow.transform, "Bulwark", "18");
            CreateSquadCard(squadRow.transform, "Medic", "18");
            CreateSquadCard(squadRow.transform, "Drone", "18");

            PanelBaseView reserveSlots = CreateAnchoredPanel(commanderBoard.ContentRoot, "ReserveSlots", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(0f, 12f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup reserveLayout = UIFactory.AddHorizontalLayout(reserveSlots.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, true, false);
            reserveLayout.childForceExpandHeight = false;
            _reserveSlotContainer = reserveSlots.ContentRoot;
            _slotContainer = _reserveSlotContainer;

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(14f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 142f, minHeight: 132f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 8f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;
            _statusText = UIFactory.CreateText("StatusText", footer.ContentRoot, "Upgrade console ready.", 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 26f, true, 15f, 18f);
            GameObject actions = UIFactory.CreateUIObject("ActionsRow", footer.ContentRoot);
            UIFactory.AddLayoutElement(actions, preferredHeight: 82f, minHeight: 76f);
            HorizontalLayoutGroup actionsLayout = UIFactory.AddHorizontalLayout(actions, 12f, TextAnchor.MiddleCenter, true, false);
            actionsLayout.childForceExpandHeight = false;
            TMP_Text costBox = UIFactory.CreateText("UpgradeCost", actions.transform, "COST\n12,000", 22, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AddLayoutElement(costBox.gameObject, preferredWidth: 160f, preferredHeight: 76f, minHeight: 70f);
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

        PanelBaseView CreateSection(Transform parent, string name, float preferredHeight, float minHeight, PanelVisualStyle style = PanelVisualStyle.Auto)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(18f, style);
            UIFactory.AddLayoutElement(panel.gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.UpperLeft, true, false);
            layout.childForceExpandHeight = false;
            return panel;
        }

        PanelBaseView CreateAnchoredPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, PanelVisualStyle style, float padding)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(padding, style);
            UIFactory.SetAnchors(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return panel;
        }

        void BindSlots(List<EquipmentSlotData> slots)
        {
            while (_slotObjects.Count < slots.Count)
            {
                GameObject slotGo = UIFactory.CreateUIObject("Slot", GetSlotParent(_slotObjects.Count));
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

                Transform targetParent = GetSlotParent(i);
                if (_slotObjects[i].transform.parent != targetParent)
                {
                    _slotObjects[i].transform.SetParent(targetParent, false);
                }

                _slotObjects[i].GetComponent<EquipmentSlotView>().Bind(slots[i]);

                LayoutElement layoutElement = UIFactory.AddLayoutElement(_slotObjects[i], flexibleWidth: i < 6 ? -1f : 1f, preferredHeight: i < 6 ? 132f : 96f, minHeight: i < 6 ? 120f : 88f);
                if (i < 6)
                {
                    layoutElement.preferredWidth = -1f;
                    layoutElement.flexibleWidth = 1f;
                }
            }
        }

        Transform GetSlotParent(int index)
        {
            if (index < 3 && _leftSlotContainer != null)
            {
                return _leftSlotContainer;
            }

            if (index < 6 && _rightSlotContainer != null)
            {
                return _rightSlotContainer;
            }

            return _reserveSlotContainer != null ? _reserveSlotContainer : _slotContainer;
        }

        void CreateSquadCard(Transform parent, string label, string level)
        {
            PanelBaseView card = UIFactory.CreateUIObject($"{label}SquadCard", parent).AddComponent<PanelBaseView>();
            card.Build(10f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(card.gameObject, flexibleWidth: 1f, preferredHeight: 90f, minHeight: 84f);
            TMP_Text text = UIFactory.CreateText("SquadText", card.ContentRoot, $"{label}\nLv.{level}", 18, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 20f;
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
