using TopEndWar.UI.Data;
using UnityEngine;

namespace TopEndWar.UI.Core
{
    public class UIActionRouter
    {
        readonly UIScreenManager _screenManager;
        readonly MockUIDataProvider _dataProvider;

        public UIActionRouter(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
        }

        public void ShowHome()
        {
            _screenManager.ShowHome();
        }

        public void ShowWorldMap()
        {
            _screenManager.ShowWorldMap();
        }

        public void ShowStageDetail(int stageId)
        {
            _screenManager.ShowStageDetail(stageId);
        }

        public void ShowCommander()
        {
            _screenManager.ShowCommander();
        }

        public void ShowResultVictory()
        {
            _screenManager.ShowResultVictory();
        }

        public void ShowResultDefeat()
        {
            _screenManager.ShowResultDefeat();
        }

        public void ShowComingSoon(string featureName)
        {
            _screenManager.ShowComingSoon(featureName);
        }

        public void GoBack()
        {
            _screenManager.GoBack();
        }

        public void ContinueCampaign()
        {
            // DATA-BINDING: Continue always resolves through current player progress instead of a hardcoded stage.
            _screenManager.ShowStageDetail(_dataProvider.CurrentProgress.currentStageId);
        }

        public void ClaimFreeReward()
        {
            Debug.Log("[UI] Free Reward clicked.");
            _screenManager.ShowToast("Reward claimed");
        }

        public void OpenDailyMissions()
        {
            ShowComingSoon("Daily Missions");
        }

        public void OpenEvents()
        {
            ShowComingSoon("Events");
        }

        public void OpenShop()
        {
            ShowComingSoon("Shop");
        }

        public void HandleBottomNav(string screenId)
        {
            switch (screenId)
            {
                case UIConstants.HomeScreenId:
                    ShowHome();
                    break;
                case UIConstants.WorldMapScreenId:
                    ShowWorldMap();
                    break;
                case UIConstants.CommanderScreenId:
                    ShowCommander();
                    break;
                case "events_placeholder":
                    OpenEvents();
                    break;
                case "shop_placeholder":
                    OpenShop();
                    break;
                default:
                    _screenManager.ShowToast($"{screenId} unavailable");
                    break;
            }
        }

        public void HandleWorldNode(StageNodeData node)
        {
            if (node.isLocked)
            {
                _screenManager.ShowToast("Stage locked");
                return;
            }

            _dataProvider.SelectStage(node.stageId);
            _screenManager.ShowStageDetail(node.stageId);
        }

        public void OpenChangeLoadout()
        {
            ShowCommander();
        }

        public void ApplyCommanderUpgrade()
        {
            Debug.Log("[UI] Commander upgrade requested.");
        }

        public void ApplyAutoEquip()
        {
            Debug.Log("[UI] Auto Equip requested.");
        }

        public void NextStageFromResult()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            int nextStageId = _dataProvider.SelectedStageId + 1;
            if (nextStageId > world.stageCount)
            {
                ShowWorldMap();
                return;
            }

            _dataProvider.SelectStage(nextStageId);
            _screenManager.ShowStageDetail(nextStageId);
        }

        public void RetryCurrentStage()
        {
            _screenManager.ShowStageDetail(_dataProvider.SelectedStageId);
        }
    }
}
