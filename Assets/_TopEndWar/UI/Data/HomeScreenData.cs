using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class HomeScreenData
    {
        public string currentWorldName;
        public int currentStageId;
        public string currentStageName;
        public int completedStages;
        public int totalStages;
        public int playerPower;
        public int targetPower;
        public string powerState;
        public bool upgradeRecommended;
        public int playerLevel;
        public int energy;
        public int gold;
        public int premiumCurrency;
        public int mailCount;
        public int totalRuns;
    }
}
