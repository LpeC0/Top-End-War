using UnityEngine;

namespace TopEndWar.UI.Theme
{
    public static class UITheme
    {
        public static readonly Color DeepNavy = Hex(0x07101A);
        public static readonly Color NavyPanel = Hex(0x102236);
        public static readonly Color Gunmetal = Hex(0x182D42);
        public static readonly Color WarmCream = Hex(0xEFE1C4);
        public static readonly Color SoftCream = Hex(0xFFF2D8);
        public static readonly Color Sand = Hex(0xD9BF8F);
        public static readonly Color MutedGold = Hex(0xD7B77A);
        public static readonly Color ButtonGoldTop = Hex(0xEFBD63);
        public static readonly Color ButtonGoldBottom = Hex(0xC77C35);
        public static readonly Color Teal = Hex(0x70D0CB);
        public static readonly Color TealDark = Hex(0x2D5A64);
        public static readonly Color Amber = Hex(0xE5A65D);
        public static readonly Color Danger = Hex(0xE8735D);
        public static readonly Color DangerDark = Hex(0x7A2D24);
        public static readonly Color EpicPurple = Hex(0xB28AE2);

        public static readonly Color TextPrimary = SoftCream;
        public static readonly Color TextSecondary = new Color(0.82f, 0.83f, 0.86f, 1f);
        public static readonly Color TextDark = DeepNavy;

        public static Color Hex(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
