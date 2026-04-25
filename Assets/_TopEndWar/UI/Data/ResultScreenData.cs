using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class ResultScreenData
    {
        public bool isVictory;
        public string stageName;
        public int stars;
        public string failureReason;
        public string recommendation;
        public bool hasFirstClearBonus;
        public List<string> performanceGoals = new List<string>();
        public List<RewardItemData> rewards = new List<RewardItemData>();
        public List<RewardItemData> firstClearRewards = new List<RewardItemData>();
    }
}
