using System;
using System.Collections.Generic;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class CommanderScreenData
    {
        public string commanderName;
        public int totalPower;
        public int hp;
        public int dps;
        public int defense;
        public string roleDescription;
        public List<EquipmentSlotData> slots = new List<EquipmentSlotData>();
        public List<string> squadMembers = new List<string>();
    }
}
