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
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 16f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(18f);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 140f, minHeight: 140f);
            GameObject backButtonGo = UIFactory.CreateUIObject("BackButton", header.ContentRoot);
            RectTransform backRect = backButtonGo.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(0f, 0f);
            UIFactory.AddLayoutElement(backButtonGo, preferredWidth: 180f, preferredHeight: 64f, minHeight: 60f, minWidth: 140f);
            PrimaryButtonView backButton = backButtonGo.AddComponent<PrimaryButtonView>();
            backButton.Build(ButtonVisualStyle.Tab);
            backButton.SetLabelKey("common.back", "BACK");
            backButton.SetOnClick(_screenManager.ActionRouter.GoBack);
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
            layout.padding = new RectOffset(0, 0, 0, 18);
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PanelBaseView preview = CreateSection(content.transform, "StagePreview", 360f, 340f);
            TMP_Text previewLabel = UIFactory.CreateText("PreviewLabel", preview.ContentRoot, "PRE-BATTLE PREVIEW", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(previewLabel, 24f);
            _briefingText = UIFactory.CreateText("Briefing", preview.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_briefingText, 220f, true, 18f, 24f);

            GameObject stateRow = UIFactory.CreateUIObject("StateRow", content.transform);
            HorizontalLayoutGroup stateLayout = UIFactory.AddHorizontalLayout(stateRow, 12f, TextAnchor.MiddleLeft, false, false);
            stateLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(stateRow, preferredHeight: 48f, minHeight: 48f);
            _stateBadge = UIFactory.CreateUIObject("StateBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge = UIFactory.CreateUIObject("BossBadge", stateRow.transform).AddComponent<TagPillView>();
            _bossBadge.SetLabel(UILocalization.Get("stage.boss", "BOSS STAGE"), true);

            PanelBaseView power = CreateSection(content.transform, "PowerPanel", 150f, 140f);
            _powerCompareText = UIFactory.CreateText("PowerCompare", power.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_powerCompareText, 96f, true, 16f, 24f);

            PanelBaseView threatPanel = CreateSection(content.transform, "ThreatPanel", 120f, 110f);
            TMP_Text threatTitle = UIFactory.CreateText("ThreatTitle", threatPanel.ContentRoot, UILocalization.Get("stage.section.threats", "THREAT TAGS"), 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(threatTitle, 24f);
            GameObject threatRow = UIFactory.CreateUIObject("ThreatContainer", threatPanel.ContentRoot);
            HorizontalLayoutGroup threatLayout = UIFactory.AddHorizontalLayout(threatRow, 10f, TextAnchor.MiddleLeft, false, false);
            threatLayout.childForceExpandHeight = false;
            _threatContainer = threatRow.transform;

            PanelBaseView enemyPanel = CreateSection(content.transform, "EnemyPanel", 260f, 220f);
            TMP_Text enemyTitle = UIFactory.CreateText("EnemyTitle", enemyPanel.ContentRoot, UILocalization.Get("stage.section.enemies", "ENEMY PREVIEW"), 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(enemyTitle, 24f);
            GameObject enemyList = UIFactory.CreateUIObject("EnemyContainer", enemyPanel.ContentRoot);
            VerticalLayoutGroup enemyLayout = UIFactory.AddVerticalLayout(enemyList, 10f, TextAnchor.UpperCenter, true, false);
            enemyLayout.childForceExpandHeight = false;
            _enemyContainer = enemyList.transform;

            PanelBaseView rewardsPanel = CreateSection(content.transform, "RewardsPanel", 240f, 220f);
            TMP_Text rewardsTitle = UIFactory.CreateText("RewardsTitle", rewardsPanel.ContentRoot, UILocalization.Get("stage.section.rewards", "REWARDS"), 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(rewardsTitle, 24f);
            GameObject rewardGrid = UIFactory.CreateUIObject("RewardContainer", rewardsPanel.ContentRoot);
            GridLayoutGroup rewardLayout = rewardGrid.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(220f, 128f);
            rewardLayout.spacing = new Vector2(12f, 12f);
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;
            _rewardContainer = rewardGrid.transform;

            _firstClearPanel = CreateSection(content.transform, "FirstClearPanel", 220f, 200f).gameObject;
            PanelBaseView firstClearPanelBase = _firstClearPanel.GetComponent<PanelBaseView>();
            TMP_Text firstClearTitle = UIFactory.CreateText("FirstClearTitle", firstClearPanelBase.ContentRoot, UILocalization.Get("stage.first_clear", "FIRST CLEAR BONUS"), 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(firstClearTitle, 24f);
            GameObject firstClearGrid = UIFactory.CreateUIObject("Rewards", firstClearPanelBase.ContentRoot);
            GridLayoutGroup firstClearLayout = firstClearGrid.AddComponent<GridLayoutGroup>();
            firstClearLayout.cellSize = new Vector2(220f, 128f);
            firstClearLayout.spacing = new Vector2(12f, 12f);
            firstClearLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            firstClearLayout.constraintCount = 2;
            _firstClearRewardContainer = firstClearGrid.transform;

            PanelBaseView loadoutPanel = CreateSection(content.transform, "LoadoutPanel", 120f, 110f);
            _loadoutText = UIFactory.CreateText("LoadoutText", loadoutPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_loadoutText, 72f, true, 16f, 22f);
            GameObject loadoutButtonGo = UIFactory.CreateUIObject("ChangeLoadoutButton", loadoutPanel.ContentRoot);
            UIFactory.AddLayoutElement(loadoutButtonGo, preferredHeight: 72f, minHeight: 68f);
            _changeLoadoutButton = loadoutButtonGo.AddComponent<PrimaryButtonView>();
            _changeLoadoutButton.Build(ButtonVisualStyle.Secondary);
            _changeLoadoutButton.SetLabelKey("stage.change_loadout", "CHANGE LOADOUT");
            _changeLoadoutButton.SetOnClick(_screenManager.ActionRouter.OpenChangeLoadout);

            _statusText = UIFactory.CreateText("StatusText", content.transform, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 32f, true, 13f, 18f);

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(18f);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 150f, minHeight: 150f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;

            GameObject actionRow = UIFactory.CreateUIObject("ActionRow", footer.ContentRoot);
            HorizontalLayoutGroup actionLayout = UIFactory.AddHorizontalLayout(actionRow, 12f, TextAnchor.MiddleCenter, false, false);
            actionLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(actionRow, preferredHeight: 76f, minHeight: 76f);

            GameObject startGo = UIFactory.CreateUIObject("StartRunButton", actionRow.transform);
            UIFactory.AddLayoutElement(startGo, flexibleWidth: 1f, preferredHeight: 100f, minHeight: 92f);
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

        PanelBaseView CreateSection(Transform parent, string name, float preferredHeight, float minHeight)
        {
            PanelBaseView panel = UIFactory.CreateUIObject(name, parent).AddComponent<PanelBaseView>();
            panel.Build(18f);
            UIFactory.AddLayoutElement(panel.gameObject, preferredHeight: preferredHeight, minHeight: minHeight);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.UpperLeft, true, false);
            layout.childForceExpandHeight = false;
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
                panel.Build(12f);
                UIFactory.AddLayoutElement(panelGo, preferredHeight: 84f, minHeight: 84f);
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
