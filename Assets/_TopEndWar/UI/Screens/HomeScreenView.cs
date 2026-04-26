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
            VerticalLayoutGroup rootLayout = UIFactory.AddVerticalLayout(gameObject, 10f, TextAnchor.UpperCenter, true, false);
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.childForceExpandHeight = false;

            PanelBaseView headerPanel = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            headerPanel.Build(18f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(headerPanel.gameObject, preferredHeight: 122f, minHeight: 112f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(headerPanel.ContentRoot.gameObject, 4f, TextAnchor.MiddleCenter, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerTitle = UIFactory.CreateText("HeaderTitle", headerPanel.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            _headerSubtitle = UIFactory.CreateText("HeaderSubtitle", headerPanel.ContentRoot, string.Empty, 20, UITheme.TealDark, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_headerTitle, 40f, true, 24f, 34f);
            UIFactory.ConfigureTextBlock(_headerSubtitle, 42f, true, 16f, 20f);

            GameObject contentArea = UIFactory.CreateUIObject("ContentArea", transform);
            UIFactory.AddLayoutElement(contentArea, flexibleHeight: 1f, minHeight: 680f);

            PanelBaseView homeBoard = UIFactory.CreateUIObject("HomeMainBoard", contentArea.transform).AddComponent<PanelBaseView>();
            homeBoard.Build(24f, PanelVisualStyle.Hero);
            UIFactory.Stretch(homeBoard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup boardLayout = UIFactory.AddVerticalLayout(homeBoard.ContentRoot.gameObject, 16f, TextAnchor.UpperCenter, true, false);
            boardLayout.padding = new RectOffset(8, 8, 8, 8);
            boardLayout.childForceExpandHeight = false;

            PanelBaseView campaignPanel = UIFactory.CreateUIObject("CampaignPanel", homeBoard.ContentRoot).AddComponent<PanelBaseView>();
            campaignPanel.Build(24f, PanelVisualStyle.Cream);
            UIFactory.AddLayoutElement(campaignPanel.gameObject, preferredHeight: 330f, minHeight: 300f);
            VerticalLayoutGroup campaignLayout = UIFactory.AddVerticalLayout(campaignPanel.ContentRoot.gameObject, 12f, TextAnchor.UpperCenter, true, false);
            campaignLayout.childForceExpandHeight = false;
            TMP_Text campaignLabel = UIFactory.CreateText("CampaignLabel", campaignPanel.ContentRoot, "CAMPAIGN OBJECTIVE", 20, UITheme.TealDark, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(campaignLabel, 30f);
            _campaignInfo = UIFactory.CreateText("CampaignInfo", campaignPanel.ContentRoot, string.Empty, 34, UITheme.DeepNavy, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_campaignInfo, 126f, true, 22f, 36f);
            GameObject continueButtonGo = UIFactory.CreateUIObject("ContinueButton", campaignPanel.ContentRoot);
            UIFactory.AddLayoutElement(continueButtonGo, preferredHeight: 94f, minHeight: 86f);
            _continueButton = continueButtonGo.AddComponent<PrimaryButtonView>();
            _continueButton.Build(ButtonVisualStyle.Primary);

            GameObject splitRow = UIFactory.CreateUIObject("StatusRow", homeBoard.ContentRoot);
            HorizontalLayoutGroup splitLayout = UIFactory.AddHorizontalLayout(splitRow, 16f, TextAnchor.UpperCenter, true, false);
            splitLayout.childForceExpandHeight = false;
            UIFactory.AddLayoutElement(splitRow, preferredHeight: 188f, minHeight: 176f);

            PanelBaseView powerPanel = UIFactory.CreateUIObject("PowerPanel", splitRow.transform).AddComponent<PanelBaseView>();
            powerPanel.Build(18f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(powerPanel.gameObject, flexibleWidth: 1f, preferredHeight: 188f, minHeight: 176f);
            _powerInfo = UIFactory.CreateText("PowerInfo", powerPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.Stretch(_powerInfo.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            _powerInfo.enableAutoSizing = true;
            _powerInfo.fontSizeMin = 16f;
            _powerInfo.fontSizeMax = 22f;

            PanelBaseView claimPanel = UIFactory.CreateUIObject("ClaimPanel", splitRow.transform).AddComponent<PanelBaseView>();
            claimPanel.Build(18f, PanelVisualStyle.PlainDark);
            UIFactory.AddLayoutElement(claimPanel.gameObject, flexibleWidth: 1f, preferredHeight: 188f, minHeight: 176f);
            _claimNotice = UIFactory.CreateText("ClaimNotice", claimPanel.ContentRoot, string.Empty, 22, UITheme.SoftCream, FontStyles.Bold);
            UIFactory.Stretch(_claimNotice.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            _claimNotice.enableAutoSizing = true;
            _claimNotice.fontSizeMin = 16f;
            _claimNotice.fontSizeMax = 22f;

            PanelBaseView quickActions = UIFactory.CreateUIObject("QuickActions", homeBoard.ContentRoot).AddComponent<PanelBaseView>();
            quickActions.Build(20f, PanelVisualStyle.Dark);
            UIFactory.AddLayoutElement(quickActions.gameObject, preferredHeight: 318f, minHeight: 286f);
            VerticalLayoutGroup quickLayout = UIFactory.AddVerticalLayout(quickActions.ContentRoot.gameObject, 10f, TextAnchor.UpperCenter, true, false);
            quickLayout.childForceExpandHeight = false;
            TMP_Text quickTitle = UIFactory.CreateText("QuickTitle", quickActions.ContentRoot, "QUICK ACTIONS", 22, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(quickTitle, 30f);
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
            UIFactory.AddLayoutElement(row, preferredHeight: 76f, minHeight: 72f);

            foreach ((string key, System.Action action) entry in actions)
            {
                GameObject buttonGo = UIFactory.CreateUIObject($"{entry.key}_Button", row.transform);
                UIFactory.AddLayoutElement(buttonGo, flexibleWidth: 1f, preferredHeight: 76f, minHeight: 72f);
                PrimaryButtonView button = buttonGo.AddComponent<PrimaryButtonView>();
                button.Build(ButtonVisualStyle.Secondary);
                button.SetLabelKey(entry.key, entry.key);
                button.SetOnClick(() => entry.action());
            }
        }
    }
}
