using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public int currentWorldId;
        public int currentStageId;
        public int completedStages;
    }
}
