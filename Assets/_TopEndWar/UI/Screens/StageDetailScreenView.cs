using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class StageDetailScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _titleText;
        TMP_Text _briefingText;
        TMP_Text _powerCompareText;
        TagPillView _stateBadge;
        TagPillView _bossBadge;
        Transform _threatContainer;
        Transform _enemyContainer;
        Transform _rewardContainer;
        GameObject _firstClearPanel;
        Transform _firstClearRewardContainer;
        TMP_Text _loadoutText;
        TMP_Text _energyCostText;
        TMP_Text _statusText;
        PrimaryButtonView _startRunButton;
        PrimaryButtonView _changeLoadoutButton;

        readonly List<GameObject> _spawnedThreats = new List<GameObject>();
        readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        readonly List<GameObject> _spawnedRewards = new List<GameObject>();
        readonly List<GameObject> _spawnedFirstClearRewards = new List<GameObject>();

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            StageDetailData data = _dataProvider.GetStageDetailData();
            _titleText.text = $"{UILocalization.Get("stage.header.title", "STAGE DETAIL")}  W{data.worldId}-{data.stageId:00}";
            _briefingText.text = $"{data.stageName}\n{data.briefingText}";
            _powerCompareText.text = $"YOUR POWER  {data.playerPower:N0}\nTARGET POWER  {data.targetPower:N0}";
            _stateBadge.SetLabel(UILocalization.Get(data.powerStateKey, data.powerStateKey), data.powerStateKey == "stage.underpowered");
            _bossBadge.gameObject.SetActive(data.isBossStage);
            _loadoutText.text = $"{UILocalization.Get("stage.loadout", "ACTIVE LOADOUT")}\n{data.loadoutName}";
            _energyCostText.text = $"ENERGY\n{data.entryCost}";
            _firstClearPanel.SetActive(data.hasFirstClearBonus);
            _enemyContainer.gameObject.SetActive(data.enemyNames.Count > 0);
            _statusText.text = "Ready to deploy.";
            _startRunButton.SetLabelKey(LocalizationKeys.StageStartRun, "START RUN");

            BindTags(_spawnedThreats, _threatContainer, data.threatKeys);
            BindEnemyCards(data.enemyNames);
            BindRewardCards(_spawnedRewards, _rewardContainer, data.rewards);
            BindRewardCards(_spawnedFirstClearRewards, _firstClearRewardContainer, data.firstClearRewards);
        }

        void Build()
        {
            if (_titleText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);

            PanelBaseView board = UIFactory.CreateUIObject("StageMissionBoard", transform).AddComponent<PanelBaseView>();
            board.Build(28f, PanelVisualStyle.Hero);
            UIFactory.Stretch(board.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            PanelBaseView header = CreateAnchoredPanel(board.ContentRoot, "StageHeader", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 142f), new Vector2(0f, -4f), PanelVisualStyle.Cream, 18f);
            GameObject backButtonGo = UIFactory.CreateUIObject("BackButton", header.ContentRoot);
            UIFactory.SetAnchors(backButtonGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(138f, 64f), new Vector2(0f, 0f));
            PrimaryButtonView backButton = backButtonGo.AddComponent<PrimaryButtonView>();
            backButton.Build(ButtonVisualStyle.Tab);
            backButton.SetLabelKey("common.back", "BACK");
            backButton.SetOnClick(_screenManager.ActionRouter.GoBack);
            _titleText = UIFactory.CreateText("Title", header.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_titleText.rectTransform, new Vector2(150f, 18f), new Vector2(-150f, -18f));
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = 24f;
            _titleText.fontSizeMax = 40f;

            PanelBaseView preview = CreateAnchoredPanel(board.ContentRoot, "BattlePreviewHero", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 430f), new Vector2(0f, -158f), PanelVisualStyle.Cream, 26f);
            Image previewTint = UIFactory.CreateImage("PreviewDioramaTint", preview.ContentRoot, new Color(0.12f, 0.08f, 0.04f, 0.22f));
            UIFactory.Stretch(previewTint.rectTransform, Vector2.zero, Vector2.zero);
            previewTint.raycastTarget = false;
            TMP_Text previewLabel = UIFactory.CreateText("PreviewLabel", preview.ContentRoot, "MISSION DIORAMA", 20, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(previewLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(0f, -8f));
            _briefingText = UIFactory.CreateText("Briefing", preview.ContentRoot, string.Empty, 26, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_briefingText.rectTransform, new Vector2(54f, 62f), new Vector2(-54f, -54f));
            _briefingText.enableAutoSizing = true;
            _briefingText.fontSizeMin = 20f;
            _briefingText.fontSizeMax = 30f;

            GameObject progressRail = UIFactory.CreateUIObject("MissionProgressRail", preview.ContentRoot);
            UIFactory.SetAnchors(progressRail.GetComponent<RectTransform>(), new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(0f, 8f));
            HorizontalLayoutGroup railLayout = UIFactory.AddHorizontalLayout(progressRail, 18f, TextAnchor.MiddleCenter, true, false);
            railLayout.childForceExpandHeight = false;
            CreateRailStep(progressRail.transform, "START", UITheme.Teal);
            CreateRailStep(progressRail.transform, "GATE 1", UITheme.Sand);
            CreateRailStep(progressRail.transform, "GATE 2", UITheme.Sand);
            CreateRailStep(progressRail.transform, "BOSS", UITheme.Danger);

            GameObject stateRow = UIFactory.CreateUIObject("StateRow", board.ContentRoot);
            UIFactory.SetAnchors(stateRow.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 54f), new Vector2(0f, -602f));
            HorizontalLayoutGroup stateLayout = UIFactory.AddHorizontalLayout(stateRow, 12f, TextAnchor.MiddleCenter, false, false);
            stateLayout.childForceExpandHeight = false;
            _stateBadge = UIFactory.CreateUIObject("StateBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge = UIFactory.CreateUIObject("BossBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge.SetLabel(UILocalization.Get("stage.boss", "BOSS STAGE"), true);

            PanelBaseView power = CreateAnchoredPanel(board.ContentRoot, "PowerCompareArea", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 138f), new Vector2(0f, -666f), PanelVisualStyle.Cream, 18f);
            _powerCompareText = UIFactory.CreateText("PowerCompare", power.ContentRoot, string.Empty, 28, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_powerCompareText.rectTransform, new Vector2(24f, 18f), new Vector2(-24f, -18f));
            _powerCompareText.enableAutoSizing = true;
            _powerCompareText.fontSizeMin = 20f;
            _powerCompareText.fontSizeMax = 30f;

            PanelBaseView threatPanel = CreateAnchoredPanel(board.ContentRoot, "ThreatPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(0f, -818f), PanelVisualStyle.PlainDark, 12f);
            GameObject threatRow = UIFactory.CreateUIObject("ThreatContainer", threatPanel.ContentRoot);
            UIFactory.Stretch(threatRow.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup threatLayout = UIFactory.AddHorizontalLayout(threatRow, 10f, TextAnchor.MiddleLeft, false, false);
            threatLayout.childForceExpandHeight = false;
            _threatContainer = threatRow.transform;

            PanelBaseView enemyPanel = CreateAnchoredPanel(board.ContentRoot, "EnemyPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 190f), new Vector2(0f, -902f), PanelVisualStyle.Dark, 18f);
            VerticalLayoutGroup enemyPanelLayout = UIFactory.AddVerticalLayout(enemyPanel.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            enemyPanelLayout.childForceExpandHeight = false;
            TMP_Text enemyTitle = UIFactory.CreateText("EnemyTitle", enemyPanel.ContentRoot, UILocalization.Get("stage.section.enemies", "ENEMIES"), 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(enemyTitle, 30f);
            GameObject enemyList = UIFactory.CreateUIObject("EnemyContainer", enemyPanel.ContentRoot);
            UIFactory.AddLayoutElement(enemyList, preferredHeight: 118f, minHeight: 112f);
            HorizontalLayoutGroup enemyLayout = UIFactory.AddHorizontalLayout(enemyList, 12f, TextAnchor.MiddleCenter, true, false);
            enemyLayout.childForceExpandHeight = false;
            _enemyContainer = enemyList.transform;

            PanelBaseView rewardsPanel = CreateAnchoredPanel(board.ContentRoot, "RewardsPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 194f), new Vector2(0f, -1106f), PanelVisualStyle.Dark, 18f);
            VerticalLayoutGroup rewardsPanelLayout = UIFactory.AddVerticalLayout(rewardsPanel.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rewardsPanelLayout.childForceExpandHeight = false;
            TMP_Text rewardsTitle = UIFactory.CreateText("RewardsTitle", rewardsPanel.ContentRoot, UILocalization.Get("stage.section.rewards", "REWARDS"), 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(rewardsTitle, 30f);
            GameObject rewardGrid = UIFactory.CreateUIObject("RewardContainer", rewardsPanel.ContentRoot);
            UIFactory.AddLayoutElement(rewardGrid, preferredHeight: 126f, minHeight: 118f);
            GridLayoutGroup rewardLayout = rewardGrid.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(220f, 120f);
            rewardLayout.spacing = new Vector2(12f, 12f);
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;
            _rewardContainer = rewardGrid.transform;

            _firstClearPanel = CreateAnchoredPanel(board.ContentRoot, "FirstClearPanel", new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 0f), new Vector2(0f, 158f), new Vector2(0f, 118f), PanelVisualStyle.Dark, 14f).gameObject;
            PanelBaseView firstClearPanelBase = _firstClearPanel.GetComponent<PanelBaseView>();
            VerticalLayoutGroup firstClearPanelLayout = UIFactory.AddVerticalLayout(firstClearPanelBase.ContentRoot.gameObject, 8f, TextAnchor.UpperCenter, true, false);
            firstClearPanelLayout.childForceExpandHeight = false;
            TMP_Text firstClearTitle = UIFactory.CreateText("FirstClearTitle", firstClearPanelBase.ContentRoot, UILocalization.Get("stage.first_clear", "FIRST CLEAR BONUS"), 18, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(firstClearTitle, 24f);
            GameObject firstClearGrid = UIFactory.CreateUIObject("Rewards", firstClearPanelBase.ContentRoot);
            UIFactory.AddLayoutElement(firstClearGrid, preferredHeight: 112f, minHeight: 104f);
            HorizontalLayoutGroup firstClearLayout = UIFactory.AddHorizontalLayout(firstClearGrid, 10f, TextAnchor.MiddleCenter, true, false);
            firstClearLayout.childForceExpandHeight = false;
            _firstClearRewardContainer = firstClearGrid.transform;

            PanelBaseView loadoutPanel = CreateAnchoredPanel(board.ContentRoot, "LoadoutPanel", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 158f), new Vector2(0f, 118f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup loadoutLayout = UIFactory.AddHorizontalLayout(loadoutPanel.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, false);
            loadoutLayout.childForceExpandHeight = false;
            _loadoutText = UIFactory.CreateText("LoadoutText", loadoutPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.AddLayoutElement(_loadoutText.gameObject, flexibleWidth: 1f, preferredHeight: 80f, minHeight: 72f);
            _loadoutText.enableAutoSizing = true;
            _loadoutText.fontSizeMin = 16f;
            _loadoutText.fontSizeMax = 22f;
            GameObject loadoutButtonGo = UIFactory.CreateUIObject("ChangeLoadoutButton", loadoutPanel.ContentRoot);
            UIFactory.AddLayoutElement(loadoutButtonGo, preferredWidth: 210f, preferredHeight: 78f, minHeight: 72f);
            _changeLoadoutButton = loadoutButtonGo.AddComponent<PrimaryButtonView>();
            _changeLoadoutButton.Build(ButtonVisualStyle.Secondary);
            _changeLoadoutButton.SetLabelKey("stage.change_loadout", "CHANGE LOADOUT");
            _changeLoadoutButton.SetOnClick(_screenManager.ActionRouter.OpenChangeLoadout);

            _statusText = UIFactory.CreateText("StatusText", board.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.SetAnchors(_statusText.rectTransform, new Vector2(0.24f, 0f), new Vector2(0.76f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(0f, 104f));
            _statusText.enableAutoSizing = true;
            _statusText.fontSizeMin = 15f;
            _statusText.fontSizeMax = 18f;

            PanelBaseView footer = CreateAnchoredPanel(board.ContentRoot, "BottomArea", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 98f), new Vector2(0f, 0f), PanelVisualStyle.PlainDark, 12f);
            HorizontalLayoutGroup footerLayout = UIFactory.AddHorizontalLayout(footer.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;

            TMP_Text energyCost = UIFactory.CreateText("EnergyCost", footer.ContentRoot, "ENERGY\n10", 24, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            _energyCostText = energyCost;
            UIFactory.AddLayoutElement(energyCost.gameObject, preferredWidth: 170f, preferredHeight: 76f, minHeight: 72f);

            GameObject actionRow = UIFactory.CreateUIObject("ActionRow", footer.ContentRoot);
            HorizontalLayoutGroup actionLayout = UIFactory.AddHorizontalLayout(actionRow, 12f, TextAnchor.MiddleCenter, false, false);
            actionLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(actionRow, preferredHeight: 82f, minHeight: 78f, flexibleWidth: 1f);

            GameObject startGo = UIFactory.CreateUIObject("StartRunButton", actionRow.transform);
            UIFactory.AddLayoutElement(startGo, flexibleWidth: 1f, preferredHeight: 92f, minHeight: 84f);
            _startRunButton = startGo.AddComponent<PrimaryButtonView>();
            _startRunButton.Build(ButtonVisualStyle.Primary);
            _startRunButton.SetOnClick(StartRun);

            GameObject previewVictory = UIFactory.CreateUIObject("PreviewVictoryButton", actionRow.transform);
            UIFactory.AddLayoutElement(previewVictory, preferredWidth: 220f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView previewVictoryButton = previewVictory.AddComponent<PrimaryButtonView>();
            previewVictoryButton.Build(ButtonVisualStyle.Secondary);
            previewVictoryButton.SetLabelKey("result.preview.victory", "PREVIEW VICTORY");
            previewVictoryButton.SetOnClick(_screenManager.ActionRouter.ShowResultVictory);

            GameObject previewDefeat = UIFactory.CreateUIObject("PreviewDefeatButton", actionRow.transform);
            UIFactory.AddLayoutElement(previewDefeat, preferredWidth: 220f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView previewDefeatButton = previewDefeat.AddComponent<PrimaryButtonView>();
            previewDefeatButton.Build(ButtonVisualStyle.Danger);
            previewDefeatButton.SetLabelKey("result.preview.defeat", "PREVIEW DEFEAT");
            previewDefeatButton.SetOnClick(_screenManager.ActionRouter.ShowResultDefeat);
            previewVictory.SetActive(UIConstants.ShowDebugButtons);
            previewDefeat.SetActive(UIConstants.ShowDebugButtons);
        }

        void CreateRailStep(Transform parent, string label, Color accent)
        {
            GameObject step = UIFactory.CreateUIObject($"Rail_{label}", parent);
            UIFactory.AddLayoutElement(step, flexibleWidth: 1f, preferredHeight: 70f, minHeight: 64f);
            TMP_Text text = UIFactory.CreateText("Label", step.transform, label, 18, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        }

        PanelBaseView CreateAnchoredPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, PanelVisualStyle style, float padding)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(padding, style);
            UIFactory.SetAnchors(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return panel;
        }

        void BindTags(List<GameObject> cache, Transform parent, List<string> tags)
        {
            while (cache.Count < tags.Count)
            {
                GameObject go = UIFactory.CreateUIObject("Tag", parent);
                go.AddComponent<TagPillView>();
                cache.Add(go);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < tags.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<TagPillView>().SetLabel(tags[i], tags[i] == "Boss");
            }
        }

        void BindEnemyCards(List<string> enemies)
        {
            while (_spawnedEnemies.Count < enemies.Count)
            {
                GameObject panelGo = UIFactory.CreateUIObject("EnemyCard", _enemyContainer);
                PanelBaseView panel = panelGo.AddComponent<PanelBaseView>();
                panel.Build(12f, PanelVisualStyle.PlainDark);
                UIFactory.AddLayoutElement(panelGo, flexibleWidth: 1f, preferredHeight: 112f, minHeight: 104f);
                TMP_Text text = UIFactory.CreateText("EnemyText", panel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 22f;
                _spawnedEnemies.Add(panelGo);
            }

            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                bool active = i < enemies.Count;
                _spawnedEnemies[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                TMP_Text text = _spawnedEnemies[i].GetComponentInChildren<TMP_Text>();
                text.text = enemies[i];
            }
        }

        void BindRewardCards(List<GameObject> cache, Transform parent, List<RewardItemData> rewards)
        {
            while (cache.Count < rewards.Count)
            {
                GameObject rewardGo = UIFactory.CreateUIObject("Reward", parent);
                rewardGo.AddComponent<RewardCardView>();
                cache.Add(rewardGo);
            }

            for (int i = 0; i < cache.Count; i++)
            {
                bool active = i < rewards.Count;
                cache[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                cache[i].GetComponent<RewardCardView>().Bind(rewards[i]);
            }
        }

        void StartRun()
        {
            if (Application.CanStreamedLevelBeLoaded(UIConstants.SampleSceneName))
            {
                SceneManager.LoadScene(UIConstants.SampleSceneName);
            }
            else
            {
                _statusText.text = "SampleScene is not available in Build Settings.";
                Debug.LogWarning("[UI] SampleScene could not be loaded from MainMenu.");
            }
        }
    }
}
