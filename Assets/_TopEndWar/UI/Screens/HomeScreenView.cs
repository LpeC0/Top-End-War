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
    public class HomeScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerTitle;
        TMP_Text _headerSubtitle;
        TMP_Text _campaignInfo;
        TMP_Text _powerInfo;
        TMP_Text _claimNotice;
        TMP_Text _statusText;
        PrimaryButtonView _continueButton;

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            HomeScreenData data = _dataProvider.GetHomeScreenData();
            _headerTitle.text = $"{UILocalization.Get("home.header.title", "HOME / HQ")}  {data.currentWorldName}";
            _headerSubtitle.text = UILocalization.Get("home.header.subtitle", "Frontline command, squad upkeep, and campaign control.");
            _campaignInfo.text = $"{data.currentStageName}\nSTAGE {data.currentStageId:00}  |  {data.completedStages}/{data.totalStages} CLEARED";
            _powerInfo.text = $"{UILocalization.Get("home.power.status", "POWER STATUS")}\nYOUR POWER  {data.playerPower:N0}\nTARGET POWER  {data.targetPower:N0}\nSTATE  {UILocalization.Get(data.powerState, data.powerState)}";
            _claimNotice.text = $"{UILocalization.Get("home.claim.notice", "Claim notice available at HQ dispatch.")}\nTOTAL RUNS  {data.totalRuns}";
            _statusText.text = UILocalization.Get("home.status.ready", "HQ console standing by.");
            _continueButton.SetLabelKey(LocalizationKeys.HomeContinueCta, "CONTINUE CAMPAIGN");
            _continueButton.SetOnClick(_screenManager.ActionRouter.ContinueCampaign);
        }

        void Build()
        {
            if (_headerTitle != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 16f, TextAnchor.UpperCenter, true, true);
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView headerPanel = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            headerPanel.Build(20f);
            UIFactory.AddLayoutElement(headerPanel.gameObject, preferredHeight: 140f, minHeight: 140f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(headerPanel.ContentRoot.gameObject, 8f, TextAnchor.UpperLeft, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerTitle = UIFactory.CreateText("HeaderTitle", headerPanel.ContentRoot, string.Empty, 34, UITheme.SoftCream, FontStyles.Bold);
            _headerSubtitle = UIFactory.CreateText("HeaderSubtitle", headerPanel.ContentRoot, string.Empty, 20, UITheme.TextSecondary);
            UIFactory.ConfigureTextBlock(_headerTitle, 42f, true, 22f, 34f);
            UIFactory.ConfigureTextBlock(_headerSubtitle, 54f, true, 16f, 20f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 640f);

            GameObject scrollGo = UIFactory.CreateUIObject("ScrollView", contentArea.transform);
            UIFactory.Stretch(scrollGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = UIFactory.CreateUIObject("Viewport", scrollGo.transform);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            UIFactory.Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            GameObject content = UIFactory.CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            scrollRect.content = contentRect;

            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(content, 16f, TextAnchor.UpperCenter, true, false);
            layout.padding = new RectOffset(0, 0, 0, 20);
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PanelBaseView campaignPanel = UIFactory.CreateUIObject("CampaignPanel", content.transform).AddComponent<PanelBaseView>();
            campaignPanel.Build(20f);
            UIFactory.AddLayoutElement(campaignPanel.gameObject, preferredHeight: 300f, minHeight: 280f);
            VerticalLayoutGroup campaignLayout = UIFactory.AddVerticalLayout(campaignPanel.ContentRoot.gameObject, 14f, TextAnchor.UpperLeft, true, false);
            campaignLayout.childForceExpandHeight = false;
            TMP_Text campaignLabel = UIFactory.CreateText("CampaignLabel", campaignPanel.ContentRoot, "CONTINUE CAMPAIGN", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(campaignLabel, 26f);
            _campaignInfo = UIFactory.CreateText("CampaignInfo", campaignPanel.ContentRoot, string.Empty, 30, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_campaignInfo, 92f, true, 18f, 30f);
            GameObject continueButtonGo = UIFactory.CreateUIObject("ContinueButton", campaignPanel.ContentRoot);
            UIFactory.AddLayoutElement(continueButtonGo, preferredHeight: 76f, minHeight: 72f);
            _continueButton = continueButtonGo.AddComponent<PrimaryButtonView>();
            _continueButton.Build(ButtonVisualStyle.Primary);

            GameObject splitRow = UIFactory.CreateUIObject("StatusRow", content.transform);
            HorizontalLayoutGroup splitLayout = UIFactory.AddHorizontalLayout(splitRow, 16f, TextAnchor.UpperCenter, true, false);
            splitLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(splitRow, preferredHeight: 220f, minHeight: 220f);

            PanelBaseView powerPanel = UIFactory.CreateUIObject("PowerPanel", splitRow.transform).AddComponent<PanelBaseView>();
            powerPanel.Build(18f);
            UIFactory.AddLayoutElement(powerPanel.gameObject, flexibleWidth: 1f, preferredHeight: 220f, minHeight: 220f);
            _powerInfo = UIFactory.CreateText("PowerInfo", powerPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_powerInfo, 160f, true, 15f, 22f);

            PanelBaseView claimPanel = UIFactory.CreateUIObject("ClaimPanel", splitRow.transform).AddComponent<PanelBaseView>();
            claimPanel.Build(18f);
            UIFactory.AddLayoutElement(claimPanel.gameObject, flexibleWidth: 1f, preferredHeight: 220f, minHeight: 220f);
            _claimNotice = UIFactory.CreateText("ClaimNotice", claimPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_claimNotice, 160f, true, 15f, 22f);

            PanelBaseView quickActions = UIFactory.CreateUIObject("QuickActions", content.transform).AddComponent<PanelBaseView>();
            quickActions.Build(18f);
            UIFactory.AddLayoutElement(quickActions.gameObject, preferredHeight: 310f, minHeight: 300f);
            VerticalLayoutGroup quickLayout = UIFactory.AddVerticalLayout(quickActions.ContentRoot.gameObject, 12f, TextAnchor.UpperCenter, true, false);
            quickLayout.childForceExpandHeight = false;
            TMP_Text quickTitle = UIFactory.CreateText("QuickTitle", quickActions.ContentRoot, "QUICK ACTIONS", 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(quickTitle, 24f);
            _statusText = UIFactory.CreateText("StatusText", quickActions.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 30f, true, 15f, 18f);

            CreateQuickRow(quickActions.ContentRoot, new List<(string key, System.Action action)>
            {
                ("home.quick.free_reward", () => HandleLocalStatus("Reward claimed", _screenManager.ActionRouter.ClaimFreeReward)),
                ("home.quick.daily", () => HandleLocalStatus("Daily Missions coming soon", _screenManager.ActionRouter.OpenDailyMissions))
            });
            CreateQuickRow(quickActions.ContentRoot, new List<(string key, System.Action action)>
            {
                ("home.quick.upgrade", _screenManager.ActionRouter.ShowCommander),
                ("home.quick.event", () => HandleLocalStatus("Events coming soon", _screenManager.ActionRouter.OpenEvents))
            });
        }

        void HandleLocalStatus(string status, System.Action action)
        {
            _statusText.text = status;
            action?.Invoke();
        }

        void CreateQuickRow(Transform parent, List<(string key, System.Action action)> actions)
        {
            GameObject row = UIFactory.CreateUIObject("QuickRow", parent);
            HorizontalLayoutGroup rowLayout = UIFactory.AddHorizontalLayout(row, 12f, TextAnchor.MiddleCenter, true, false);
            rowLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(row, preferredHeight: 84f, minHeight: 84f);

            foreach ((string key, System.Action action) entry in actions)
            {
                GameObject buttonGo = UIFactory.CreateUIObject($"{entry.key}_Button", row.transform);
                UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 84f, minHeight: 84f);
                PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
                button.Build(ButtonVisualStyle.Secondary);
                button.SetLabelKey(entry.key, entry.key);
                button.SetOnClick(() => entry.action());
            }
        }
    }
}
