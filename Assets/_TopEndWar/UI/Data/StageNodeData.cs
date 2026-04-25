using System;
using UnityEngine;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class StageNodeData
    {
        public int stageId;
        public Vector2 anchoredPosition;
        public bool isBoss;
        public bool isCurrent;
        public bool isCompleted;
        public bool isLocked;
        public bool isUnlocked;
    }
}
