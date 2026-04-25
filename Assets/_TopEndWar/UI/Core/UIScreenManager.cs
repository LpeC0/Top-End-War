using TopEndWar.UI.Components;
using TopEndWar.UI.Data;
using TopEndWar.UI.Screens;
using TopEndWar.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Core
{
    public class UIScreenManager : MonoBehaviour
    {
        MockUIDataProvider _dataProvider;
        TopBarView _topBarView;
        BottomNavView _bottomNavView;
        HomeScreenView _homeScreen;
        WorldMapScreenView _worldMapScreen;
        StageDetailScreenView _stageDetailScreen;
        CommanderScreenView _commanderScreen;
        ResultScreenView _resultScreen;
        UIActionRouter _actionRouter;
        GameObject _toastRoot;
        TMP_Text _toastText;
        Coroutine _toastCoroutine;

        bool _isBootstrapped;

        public UIActionRouter ActionRouter => _actionRouter;

        public void Bootstrap()
        {
            if (_isBootstrapped)
            {
                RefreshSharedChrome();
                ShowHome();
                return;
            }

            // DEGISIKLIK: MainMenu now boots into a reusable screen manager instead of a one-off menu builder.
            _dataProvider = UIFactory.GetOrAdd<MockUIDataProvider>(gameObject);
            _actionRouter = new UIActionRouter(this, _dataProvider);
            RectTransform root = GetComponent<RectTransform>();
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            Vector2 safePadding = new Vector2(24f, 24f);

            GameObject background = UIFactory.CreateUIObject("Backdrop", transform);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = UITheme.DeepNavy;
            UIFactory.Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject topBarGo = UIFactory.CreateUIObject("TopBar", transform);
            RectTransform topBarRect = topBarGo.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(0f, 120f);
            topBarRect.anchoredPosition = new Vector2(0f, -safePadding.y);
            topBarRect.offsetMin = new Vector2(safePadding.x, -144f);
            topBarRect.offsetMax = new Vector2(-safePadding.x, -safePadding.y);
            _topBarView = topBarGo.AddComponent<TopBarView>();
            _topBarView.Build();

            GameObject bottomNavGo = UIFactory.CreateUIObject("BottomNav", transform);
            RectTransform bottomNavRect = bottomNavGo.GetComponent<RectTransform>();
            bottomNavRect.anchorMin = new Vector2(0f, 0f);
            bottomNavRect.anchorMax = new Vector2(1f, 0f);
            bottomNavRect.pivot = new Vector2(0.5f, 0f);
            bottomNavRect.sizeDelta = new Vector2(0f, 140f);
            bottomNavRect.anchoredPosition = new Vector2(0f, safePadding.y);
            bottomNavRect.offsetMin = new Vector2(safePadding.x, safePadding.y);
            bottomNavRect.offsetMax = new Vector2(-safePadding.x, 164f);
            _bottomNavView = bottomNavGo.AddComponent<BottomNavView>();
            _bottomNavView.Build(_actionRouter.HandleBottomNav);

            GameObject contentRoot = UIFactory.CreateUIObject("ContentRoot", transform);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            UIFactory.Stretch(contentRect, new Vector2(safePadding.x, 188f), new Vector2(-safePadding.x, -164f));

            _homeScreen = CreateScreen<HomeScreenView>("HomeScreen", contentRoot.transform);
            _worldMapScreen = CreateScreen<WorldMapScreenView>("WorldMapScreen", contentRoot.transform);
            _stageDetailScreen = CreateScreen<StageDetailScreenView>("StageDetailScreen", contentRoot.transform);
            _commanderScreen = CreateScreen<CommanderScreenView>("CommanderScreen", contentRoot.transform);
            _resultScreen = CreateScreen<ResultScreenView>("ResultScreen", contentRoot.transform);

            _homeScreen.Initialize(this, _dataProvider);
            _worldMapScreen.Initialize(this, _dataProvider);
            _stageDetailScreen.Initialize(this, _dataProvider);
            _commanderScreen.Initialize(this, _dataProvider);
            _resultScreen.Initialize(this, _dataProvider);
            BuildToastOverlay();

            _isBootstrapped = true;
            ShowHome();
        }

        public void ShowHome()
        {
            RefreshSharedChrome();
            _homeScreen.RefreshView();
            SetVisibleScreen(UIConstants.HomeScreenId, true, true);
        }

        public void ShowWorldMap()
        {
            RefreshSharedChrome();
            _worldMapScreen.RefreshView();
            SetVisibleScreen(UIConstants.WorldMapScreenId, true, true);
        }

        public void ShowStageDetail(int stageId)
        {
            _dataProvider.SelectStage(stageId);
            _stageDetailScreen.RefreshView();
            SetVisibleScreen(UIConstants.StageDetailScreenId, false, false);
        }

        public void ShowCommander()
        {
            RefreshSharedChrome();
            _commanderScreen.RefreshView();
            SetVisibleScreen(UIConstants.CommanderScreenId, true, true);
        }

        public void ShowResult(bool victoryPreview)
        {
            _dataProvider.SetResultPreview(victoryPreview);
            _resultScreen.RefreshView();
            SetVisibleScreen(UIConstants.ResultScreenId, false, false);
        }

        public void ShowResultVictory()
        {
            ShowResult(true);
        }

        public void ShowResultDefeat()
        {
            ShowResult(false);
        }

        public void AdvanceToNextStage()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            int nextStage = _dataProvider.SelectedStageId + 1;
            if (nextStage > world.stageCount)
            {
                ShowWorldMap();
                return;
            }

            _dataProvider.SelectStage(nextStage);
            ShowStageDetail(nextStage);
        }

        public void ShowComingSoon(string featureName)
        {
            ShowToast($"{featureName} coming soon");
        }

        public void GoBack()
        {
            // DEGISIKLIK: First-pass navigation contract keeps Stage Detail back behavior predictable.
            ShowWorldMap();
        }

        public void ShowToast(string message)
        {
            if (_toastRoot == null || _toastText == null)
            {
                Debug.Log($"[UI] {message}");
                return;
            }

            _toastText.text = message;
            _toastRoot.SetActive(true);
            _toastRoot.transform.SetAsLastSibling();

            if (_toastCoroutine != null)
            {
                StopCoroutine(_toastCoroutine);
            }

            _toastCoroutine = StartCoroutine(HideToastAfterDelay(1.8f));
        }

        void RefreshSharedChrome()
        {
            if (_topBarView != null)
            {
                _topBarView.Bind(_dataProvider.GetTopBarData());
            }
        }

        void SetVisibleScreen(string screenId, bool showTopBar, bool showBottomNav)
        {
            _homeScreen.gameObject.SetActive(screenId == UIConstants.HomeScreenId);
            _worldMapScreen.gameObject.SetActive(screenId == UIConstants.WorldMapScreenId);
            _stageDetailScreen.gameObject.SetActive(screenId == UIConstants.StageDetailScreenId);
            _commanderScreen.gameObject.SetActive(screenId == UIConstants.CommanderScreenId);
            _resultScreen.gameObject.SetActive(screenId == UIConstants.ResultScreenId);
            _topBarView.gameObject.SetActive(showTopBar);
            _bottomNavView.gameObject.SetActive(showBottomNav);
            _bottomNavView.SetActiveScreen(screenId);
        }

        T CreateScreen<T>(string name, Transform parent) where T : Component
        {
            GameObject go = UIFactory.CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            UIFactory.Stretch(rect, Vector2.zero, Vector2.zero);
            return go.AddComponent<T>();
        }

        void BuildToastOverlay()
        {
            if (_toastRoot != null)
            {
                return;
            }

            PanelBaseView toastPanel = UIFactory.CreateUIObject("ToastOverlay", transform).AddComponent<PanelBaseView>();
            toastPanel.Build(18f);
            _toastRoot = toastPanel.gameObject;
            RectTransform toastRect = _toastRoot.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = new Vector2(520f, 84f);
            toastRect.anchoredPosition = new Vector2(0f, -150f);

            _toastText = UIFactory.CreateText("ToastText", toastPanel.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(_toastText.rectTransform, Vector2.zero, Vector2.zero);
            _toastText.enableAutoSizing = true;
            _toastText.fontSizeMin = 18f;
            _toastText.fontSizeMax = 24f;
            _toastRoot.SetActive(false);
        }

        System.Collections.IEnumerator HideToastAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_toastRoot != null)
            {
                _toastRoot.SetActive(false);
            }
            _toastCoroutine = null;
        }
    }
}
