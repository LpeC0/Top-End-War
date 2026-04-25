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
    public class ResultScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerText;
        TMP_Text _stageText;
        TMP_Text _starsText;
        TMP_Text _recommendationText;
        TMP_Text _failureText;
        Transform _performanceContainer;
        Transform _rewardContainer;
        GameObject _firstClearPanel;
        Transform _firstClearRewardContainer;
        readonly List<GameObject> _spawnedPerformance = new List<GameObject>();
        readonly List<GameObject> _spawnedRewards = new List<GameObject>();
        readonly List<GameObject> _spawnedFirstClear = new List<GameObject>();

        PrimaryButtonView _primaryButton;
        PrimaryButtonView _secondaryA;
        PrimaryButtonView _secondaryB;
        PrimaryButtonView _secondaryC;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            ResultScreenData data = _dataProvider.GetResultScreenData();
            _headerText.text = data.isVictory ? UILocalization.Get("result.victory", "STAGE CLEAR") : UILocalization.Get("result.defeat", "DEFEAT");
            _stageText.text = data.stageName;
            _starsText.text = data.isVictory ? new string('*', Mathf.Clamp(data.stars, 1, 3)) : "NO STARS";
            _failureText.gameObject.SetActive(!data.isVictory);
            _failureText.text = data.failureReason;
            _recommendationText.text = $"{UILocalization.Get("result.recommendation", "RECOMMENDATION")}\n{data.recommendation}";
            _performanceContainer.gameObject.SetActive(data.isVictory && data.performanceGoals.Count > 0);
            _firstClearPanel.SetActive(data.isVictory && data.hasFirstClearBonus);

            BindTagPills(_spawnedPerformance, _performanceContainer, data.performanceGoals);
            BindRewardCards(_spawnedRewards, _rewardContainer, data.rewards);
            BindRewardCards(_spawnedFirstClear, _firstClearRewardContainer, data.firstClearRewards);

            if (data.isVictory)
            {
                _primaryButton.SetLabelKey(LocalizationKeys.ResultNextStage, "NEXT STAGE");
                _primaryButton.SetOnClick(_screenManager.ActionRouter.NextStageFromResult);
                _secondaryA.SetLabelKey("result.secondary.upgrade", "UPGRADE");
                _secondaryA.SetOnClick(_screenManager.ActionRouter.ShowCommander);
                _secondaryB.SetLabelKey(LocalizationKeys.ResultWorldMap, "WORLD MAP");
                _secondaryB.SetOnClick(_screenManager.ActionRouter.ShowWorldMap);
                _secondaryC.SetLabelKey(LocalizationKeys.ResultRetryStage, "RETRY STAGE");
                _secondaryC.SetOnClick(_screenManager.ActionRouter.RetryCurrentStage);
                _secondaryC.gameObject.SetActive(true);
            }
            else
            {
                _primaryButton.SetLabelKey(LocalizationKeys.CommanderUpgrade, "UPGRADE");
                _primaryButton.SetOnClick(_screenManager.ActionRouter.ShowCommander);
                _secondaryA.SetLabelKey(LocalizationKeys.ResultRetryStage, "RETRY STAGE");
                _secondaryA.SetOnClick(_screenManager.ActionRouter.RetryCurrentStage);
                _secondaryB.SetLabelKey(LocalizationKeys.ResultWorldMap, "WORLD MAP");
                _secondaryB.SetOnClick(_screenManager.ActionRouter.ShowWorldMap);
                _secondaryC.gameObject.SetActive(false);
            }
        }

        void Build()
        {
            if (_headerText != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 16f, TextAnchor.UpperCenter, true, false);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(18f);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 340f, minHeight: 320f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(header.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            _headerText = UIFactory.CreateText("HeaderText", header.ContentRoot, string.Empty, 34, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            _stageText = UIFactory.CreateText("StageText", header.ContentRoot, string.Empty, 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            _starsText = UIFactory.CreateText("StarsText", header.ContentRoot, string.Empty, 26, UITheme.ButtonGoldTop, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_headerText, 56f, true, 22f, 34f);
            UIFactory.ConfigureTextBlock(_stageText, 48f, true, 16f, 22f);
            UIFactory.ConfigureTextBlock(_starsText, 40f, true, 16f, 26f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 420f);

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

            PanelBaseView previewSwitch = CreateSection(content.transform, "PreviewSwitch", 86f, 84f);
            GameObject switchRow = UIFactory.CreateUIObject("SwitchRow", previewSwitch.ContentRoot);
            HorizontalLayoutGroup switchLayout = UIFactory.AddHorizontalLayout(switchRow, 12f, TextAnchor.MiddleCenter, true, false);
            switchLayout.childForceExpandHeight = false;
            CreatePreviewButton(switchRow.transform, true, "result.preview.victory", ButtonVisualStyle.Secondary);
            CreatePreviewButton(switchRow.transform, false, "result.preview.defeat", ButtonVisualStyle.Danger);
            previewSwitch.gameObject.SetActive(UIConstants.ShowDebugButtons);

            PanelBaseView performance = CreateSection(content.transform, "PerformancePanel", 140f, 120f);
            TMP_Text performanceTitle = UIFactory.CreateText("PerformanceTitle", performance.ContentRoot, "PERFORMANCE GOALS", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(performanceTitle, 24f);
            GameObject performanceRow = UIFactory.CreateUIObject("PerformanceContainer", performance.ContentRoot);
            HorizontalLayoutGroup performanceLayout = UIFactory.AddHorizontalLayout(performanceRow, 10f, TextAnchor.MiddleCenter, false, false);
            performanceLayout.childForceExpandHeight = false;
            _performanceContainer = performanceRow.transform;

            PanelBaseView rewards = CreateSection(content.transform, "RewardsPanel", 240f, 220f);
            TMP_Text rewardsTitle = UIFactory.CreateText("RewardsTitle", rewards.ContentRoot, "REWARDS", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(rewardsTitle, 24f);
            GameObject rewardGrid = UIFactory.CreateUIObject("RewardContainer", rewards.ContentRoot);
            GridLayoutGroup rewardLayout = rewardGrid.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(220f, 128f);
            rewardLayout.spacing = new Vector2(12f, 12f);
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;
            _rewardContainer = rewardGrid.transform;

            _firstClearPanel = CreateSection(content.transform, "FirstClearPanel", 220f, 200f).gameObject;
            PanelBaseView firstClear = _firstClearPanel.GetComponent<PanelBaseView>();
            TMP_Text firstClearTitle = UIFactory.CreateText("FirstClearTitle", firstClear.ContentRoot, UILocalization.Get("stage.first_clear", "FIRST CLEAR BONUS"), 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(firstClearTitle, 24f);
            GameObject firstClearGrid = UIFactory.CreateUIObject("Rewards", firstClear.ContentRoot);
            GridLayoutGroup firstClearLayout = firstClearGrid.AddComponent<GridLayoutGroup>();
            firstClearLayout.cellSize = new Vector2(220f, 128f);
            firstClearLayout.spacing = new Vector2(12f, 12f);
            firstClearLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            firstClearLayout.constraintCount = 2;
            _firstClearRewardContainer = firstClearGrid.transform;

            PanelBaseView recommendation = CreateSection(content.transform, "RecommendationPanel", 160f, 150f);
            _failureText = UIFactory.CreateText("FailureText", recommendation.ContentRoot, string.Empty, 22, UITheme.Danger, FontStyles.Bold);
            _recommendationText = UIFactory.CreateText("RecommendationText", recommendation.ContentRoot, string.Empty, 20, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_failureText, 34f, true, 14f, 22f);
            UIFactory.ConfigureTextBlock(_recommendationText, 90f, true, 15f, 20f);

            PanelBaseView footer = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            footer.Build(18f);
            UIFactory.AddLayoutElement(footer.gameObject, preferredHeight: 160f, minHeight: 160f);
            VerticalLayoutGroup footerLayout = UIFactory.AddVerticalLayout(footer.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, true, false);
            footerLayout.childForceExpandHeight = false;

            GameObject primaryGo = UIFactory.CreateUIObject("PrimaryAction", footer.ContentRoot);
            UIFactory.AddLayoutElement(primaryGo, preferredHeight: 76f, minHeight: 72f);
            _primaryButton = primaryGo.AddComponent<PrimaryButtonView>();
            _primaryButton.Build(ButtonVisualStyle.Primary);

            GameObject secondaryRow = UIFactory.CreateUIObject("SecondaryRow", footer.ContentRoot);
            HorizontalLayoutGroup secondaryLayout = UIFactory.AddHorizontalLayout(secondaryRow, 12f, TextAnchor.MiddleCenter, true, false);
            secondaryLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(secondaryRow, preferredHeight: 76f, minHeight: 72f);
            _secondaryA = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
            _secondaryB = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
            _secondaryC = CreateActionButton(secondaryRow.transform, ButtonVisualStyle.Secondary);
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

        PrimaryButtonView CreateActionButton(Transform parent, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject("SecondaryAction", parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 76f, minHeight: 72f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            return button;
        }

        void CreatePreviewButton(Transform parent, bool victory, string key, ButtonVisualStyle style)
        {
            GameObject buttonGo = UIFactory.CreateUIObject(key, parent);
            UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 60f);
            PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
            button.Build(style);
            button.SetLabelKey(key, key);
            button.SetOnClick(() =>
            {
                _dataProvider.SetResultPreview(victory);
                RefreshView();
            });
        }

        void BindTagPills(List<GameObject> cache, Transform parent, List<string> tags)
        {
            while (cache.Count < tags.Count)
            {
                GameObject go = UIFactory.CreateUIObject("Goal", parent);
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

                cache[i].GetComponent<TagPillView>().SetLabel(tags[i]);
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
    }
}
