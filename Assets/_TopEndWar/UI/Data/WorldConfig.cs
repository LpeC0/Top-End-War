using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class WorldConfig
    {
        public int worldId;
        public string worldName;
        public int stageCount;
        public int completedStages;
        public int currentStageId;
        public int bossStageId;
        public string layoutTemplateId;
    }
}
