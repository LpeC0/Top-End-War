using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class StageDetailData
    {
        public int worldId;
        public int stageId;
        public string stageName;
        public int playerPower;
        public int targetPower;
        public string powerStateKey;
        public bool isBossStage;
        public bool hasFirstClearBonus;
        public string loadoutName;
        public string briefingText;
        public List<string> threatKeys = new List<string>();
        public List<string> enemyNames = new List<string>();
        public List<RewardItemData> rewards = new List<RewardItemData>();
        public List<RewardItemData> firstClearRewards = new List<RewardItemData>();
    }
}
