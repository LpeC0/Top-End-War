using System;
using System.Collections.Generic;
using TopEndWar.UI.Core;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class BottomNavView : MonoBehaviour
    {
        readonly Dictionary<string, PrimaryButtonView> _buttons = new Dictionary<string, PrimaryButtonView>();
        bool _isBuilt;

        public void Build(Action<string> onNavigate)
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(16f);
            HorizontalLayoutGroup row = UIFactory.AddHorizontalLayout(panel.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, true);
            row.padding = new RectOffset(0, 0, 0, 0);

            CreateNavButton(panel.ContentRoot, UIConstants.HomeScreenId, "nav.home", onNavigate);
            CreateNavButton(panel.ContentRoot, UIConstants.WorldMapScreenId, "nav.map", onNavigate);
            CreateNavButton(panel.ContentRoot, UIConstants.CommanderScreenId, "nav.commander", onNavigate);
            CreateNavButton(panel.ContentRoot, "events_placeholder", "nav.events", onNavigate);
            CreateNavButton(panel.ContentRoot, "shop_placeholder", "nav.shop", onNavigate);
            _isBuilt = true;
        }

        public void SetActiveScreen(string screenId)
        {
            foreach (KeyValuePair<string, PrimaryButtonView> entry in _buttons)
            {
                entry.Value.SetSelected(entry.Key == screenId);
            }
        }

        void CreateNavButton(Transform parent, string screenId, string key, Action<string> onNavigate)
        {
            if (_buttons.ContainsKey(screenId))
            {
                return;
            }

            GameObject go = UIFactory.CreateUIObject($"{screenId}_Button", parent);
            UIFactory.AddLayoutElement(go, preferredHeight: 72f, flexibleWidth: 1f, minHeight: 64f);
            PrimaryButtonView button = go.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, UILocalization.Get(key, key));
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseIconSprites && art != null)
            {
                button.SetIcon(art.GetNavIcon(screenId), ResolveIconAssetName(screenId));
            }

            button.SetOnClick(() => onNavigate?.Invoke(screenId));
            _buttons.Add(screenId, button);
        }

        string ResolveIconAssetName(string screenId)
        {
            switch (screenId)
            {
                case UIConstants.HomeScreenId:
                    return "Icon_Home";
                case UIConstants.WorldMapScreenId:
                    return "Icon_Map";
                case UIConstants.CommanderScreenId:
                    return "Icon_Commander";
                case "events_placeholder":
                    return "Icon_Events";
                case "shop_placeholder":
                    return "Icon_Shop";
                default:
                    return "BottomNavIcon";
            }
        }
    }
}
