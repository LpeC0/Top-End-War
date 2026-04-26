using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public enum PanelVisualStyle
    {
        Auto,
        Dark,
        Cream,
        Hero,
        PlainDark
    }

    public class PanelBaseView : MonoBehaviour
    {
        public RectTransform ContentRoot { get; private set; }

        bool _isBuilt;

        public void Build(float padding = 18f, PanelVisualStyle style = PanelVisualStyle.Auto)
        {
            if (_isBuilt)
            {
                return;
            }

            // UI: Shared panel shell for the warm heroic diorama look.
            Image background = UIFactory.GetOrAdd<Image>(gameObject);
            ApplyPanelArt(background, style);

            Outline outline = UIFactory.GetOrAdd<Outline>(gameObject);
            outline.effectColor = UITheme.MutedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow shadow = UIFactory.GetOrAdd<Shadow>(gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -4f);

            GameObject content = UIFactory.CreateUIObject("Content", transform);
            ContentRoot = content.GetComponent<RectTransform>();
            UIFactory.Stretch(ContentRoot, new Vector2(padding, padding), new Vector2(-padding, -padding));

            _isBuilt = true;
        }

        void ApplyPanelArt(Image background, PanelVisualStyle style)
        {
            if (!UIConstants.UsePanelSprites)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = ResolveFallbackColor(style);
                return;
            }

            PanelVisualStyle resolved = style == PanelVisualStyle.Auto ? ResolveStyleFromName() : style;
            if (resolved == PanelVisualStyle.PlainDark)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = ResolveFallbackColor(resolved);
                return;
            }

            Sprite sprite = null;
            string assetName = "UI_Panel_Dark_01";
            Color fallback = UITheme.NavyPanel;
            UIArtLibrary art = UIArtLibrary.Instance;

            if (art != null)
            {
                switch (resolved)
                {
                    case PanelVisualStyle.Cream:
                        sprite = art.PanelCream;
                        assetName = "UI_Panel_Cream_01";
                        fallback = UITheme.WarmCream;
                        break;
                    case PanelVisualStyle.Hero:
                        sprite = art.PanelHero;
                        assetName = "UI_Panel_Hero_01";
                        fallback = UITheme.NavyPanel;
                        break;
                    default:
                        sprite = art.PanelDark;
                        break;
                }
            }

            UIArtLibrary.TryApply(background, sprite, fallback, assetName);
        }

        Color ResolveFallbackColor(PanelVisualStyle style)
        {
            PanelVisualStyle resolved = style == PanelVisualStyle.Auto ? ResolveStyleFromName() : style;
            switch (resolved)
            {
                case PanelVisualStyle.Hero:
                    return UITheme.NavyPanel;
                case PanelVisualStyle.Cream:
                case PanelVisualStyle.PlainDark:
                    return UITheme.Gunmetal;
                default:
                    return UITheme.NavyPanel;
            }
        }

        PanelVisualStyle ResolveStyleFromName()
        {
            string objectName = gameObject.name.ToLowerInvariant();
            if (objectName.Contains("toparea") || objectName.Contains("campaign") || objectName.Contains("preview") || objectName.Contains("visual"))
            {
                return PanelVisualStyle.Hero;
            }

            if (objectName.Contains("slot") || objectName.Contains("enemycard") || objectName.Contains("toast"))
            {
                return PanelVisualStyle.PlainDark;
            }

            return PanelVisualStyle.Dark;
        }
    }
}
