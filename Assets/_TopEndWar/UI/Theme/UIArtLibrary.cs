using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TopEndWar.UI.Theme
{
    [CreateAssetMenu(menuName = "Top End War/UI Art Library", fileName = "UIArtLibrary")]
    public class UIArtLibrary : ScriptableObject
    {
        const string AssetPath = "Assets/_TopEndWar/UI/Theme/UIArtLibrary.asset";

        static UIArtLibrary _instance;
        static readonly HashSet<string> MissingWarnings = new HashSet<string>();

        [Header("WorldMaps")]
        public Sprite World01MapViewport;
        public Sprite World01MapMaster;

        [Header("Buttons")]
        public Sprite PrimaryButton;
        public Sprite SecondaryButton;
        public Sprite TabButton;
        public Sprite BottomNavItem;
        public Sprite BottomNavItemActive;

        [Header("Panels")]
        public Sprite PanelDark;
        public Sprite PanelCream;
        public Sprite PanelHero;

        [Header("Icons")]
        public Sprite IconEnergy;
        public Sprite IconGold;
        public Sprite IconGems;
        public Sprite IconMail;
        public Sprite IconSettings;
        public Sprite IconHome;
        public Sprite IconMap;
        public Sprite IconCommander;
        public Sprite IconEvents;
        public Sprite IconShop;
        public Sprite IconMissions;
        public Sprite IconUpgrade;
        public Sprite IconFreeReward;
        public Sprite IconLocked;
        public Sprite IconBoss;
        public Sprite IconBack;

        [Header("Nodes")]
        public Sprite NodeNormal;
        public Sprite NodeCurrent;
        public Sprite NodeCompleted;
        public Sprite NodeLocked;
        public Sprite NodeBoss;

        [Header("Rewards")]
        public Sprite RewardGold;
        public Sprite RewardTechCore;
        public Sprite RewardGearBox;
        public Sprite RewardGems;
        public Sprite RewardParts;

        [Header("Commander")]
        public Sprite CommanderPortrait;
        public Sprite CommanderFull;

        public static UIArtLibrary Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

#if UNITY_EDITOR
                _instance = AssetDatabase.LoadAssetAtPath<UIArtLibrary>(AssetPath);
#endif
                return _instance;
            }
        }

        public static bool TryApply(Image image, Sprite sprite, Color fallbackColor, string assetName)
        {
            if (image == null)
            {
                return false;
            }

            if (sprite == null)
            {
                WarnMissing(assetName);
                image.sprite = null;
                image.color = fallbackColor;
                image.type = Image.Type.Simple;
                return false;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            return true;
        }

        public static void WarnMissing(string assetName)
        {
            if (string.IsNullOrEmpty(assetName) || MissingWarnings.Contains(assetName))
            {
                return;
            }

            MissingWarnings.Add(assetName);
            Debug.LogWarning($"[UI ART] Missing sprite: {assetName}");
        }

        public Sprite GetBottomNavSprite(bool active)
        {
            return active ? BottomNavItemActive : BottomNavItem;
        }

        public Sprite GetNavIcon(string screenId)
        {
            switch (screenId)
            {
                case Core.UIConstants.HomeScreenId:
                    return IconHome;
                case Core.UIConstants.WorldMapScreenId:
                    return IconMap;
                case Core.UIConstants.CommanderScreenId:
                    return IconCommander;
                case "events_placeholder":
                    return IconEvents;
                case "shop_placeholder":
                    return IconShop;
                default:
                    return null;
            }
        }

        public Sprite GetRewardSprite(string labelKey, string fallbackLabel, string accent)
        {
            string key = $"{labelKey} {fallbackLabel} {accent}".ToLowerInvariant();
            if (key.Contains("tech"))
            {
                return RewardTechCore;
            }

            if (key.Contains("gear"))
            {
                return RewardGearBox;
            }

            if (key.Contains("gem") || key.Contains("premium"))
            {
                return RewardGems;
            }

            if (key.Contains("part"))
            {
                return RewardParts;
            }

            return RewardGold;
        }

        public string GetRewardAssetName(string labelKey, string fallbackLabel, string accent)
        {
            string key = $"{labelKey} {fallbackLabel} {accent}".ToLowerInvariant();
            if (key.Contains("tech"))
            {
                return "Reward_TechCore";
            }

            if (key.Contains("gear"))
            {
                return "Reward_GearBox";
            }

            if (key.Contains("gem") || key.Contains("premium"))
            {
                return "Reward_Gems";
            }

            if (key.Contains("part"))
            {
                return "Reward_Parts";
            }

            return "Reward_Gold";
        }
    }
}
