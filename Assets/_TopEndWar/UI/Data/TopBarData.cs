using System;

namespace TopEndWar.UI.Data
{
    [Serializable]
    public class TopBarData
    {
        public string commanderName;
        public int playerLevel;
        public int energy;
        public int gold;
        public int premiumCurrency;
        public int mailCount;
        public bool showPremiumCurrency;
    }
}
