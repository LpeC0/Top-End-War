using TMPro;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class TopBarView : MonoBehaviour
    {
        TMP_Text _identityText;
        TMP_Text _resourceA;
        TMP_Text _resourceB;
        TMP_Text _resourceC;
        NotificationBadgeView _mailBadge;
        bool _isBuilt;

        public void Build()
        {
            if (_isBuilt)
            {
                return;
            }

            PanelBaseView panel = UIFactory.GetOrAdd<PanelBaseView>(gameObject);
            panel.Build(18f);
            HorizontalLayoutGroup row = UIFactory.AddHorizontalLayout(panel.ContentRoot.gameObject, 10f, TextAnchor.MiddleCenter, false, false);
            row.padding = new RectOffset(0, 0, 0, 0);

            GameObject identity = UIFactory.CreateUIObject("Identity", panel.ContentRoot);
            UIFactory.AddLayoutElement(identity, flexibleWidth: 1f, preferredHeight: 80f);
            UIFactory.AddHorizontalLayout(identity, 10f, TextAnchor.MiddleLeft, false, false);

            Image avatar = UIFactory.CreateUIObject("CommanderAvatar", identity.transform).AddComponent<Image>();
            UIFactory.AddLayoutElement(avatar.gameObject, preferredWidth: 72f, preferredHeight: 72f, minWidth: 72f, minHeight: 72f);
            UIArtLibrary art = UIArtLibrary.Instance;
            if (UIConstants.UseCommanderSprites)
            {
                UIArtLibrary.TryApply(avatar, art != null ? art.CommanderPortrait : null, UITheme.Gunmetal, "Commander_Portrait_01");
            }
            else
            {
                avatar.sprite = null;
                avatar.color = UITheme.Gunmetal;
            }

            GameObject identityTextStack = UIFactory.CreateUIObject("IdentityTextStack", identity.transform);
            UIFactory.AddLayoutElement(identityTextStack, flexibleWidth: 1f, preferredHeight: 80f);
            UIFactory.AddVerticalLayout(identityTextStack, 2f, TextAnchor.MiddleLeft, true, false);

            if (_identityText == null)
            {
                _identityText = UIFactory.CreateText("IdentityText", identityTextStack.transform, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
                UIFactory.ConfigureTextBlock(_identityText, 34f, true, 18f, 24f);
                TMP_Text identitySub = UIFactory.CreateText("IdentitySub", identityTextStack.transform, "FIELD COMMAND", 14, UITheme.TextSecondary, FontStyles.Normal);
                UIFactory.ConfigureTextBlock(identitySub, 24f);
            }

            _resourceA = CreateResourceLabel(panel.ContentRoot, "ResourceA", art != null ? art.IconEnergy : null, "Icon_Energy");
            _resourceB = CreateResourceLabel(panel.ContentRoot, "ResourceB", art != null ? art.IconGold : null, "Icon_Gold");
            _resourceC = CreateResourceLabel(panel.ContentRoot, "ResourceC", art != null ? art.IconGems : null, "Icon_Gems");

            GameObject mail = UIFactory.CreateUIObject("Mail", panel.ContentRoot);
            UIFactory.AddLayoutElement(mail, preferredWidth: 88f, preferredHeight: 76f, minWidth: 88f);
            Image mailImage = mail.AddComponent<Image>();
            mailImage.color = UITheme.Gunmetal;
            Image mailIcon = UIFactory.CreateUIObject("MailIcon", mail.transform).AddComponent<Image>();
            mailIcon.raycastTarget = false;
            bool hasMailIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(mailIcon, art != null ? art.IconMail : null, Color.clear, "Icon_Mail");
            mailIcon.enabled = hasMailIcon;
            UIFactory.Stretch(mailIcon.rectTransform, new Vector2(26f, 28f), new Vector2(-26f, -18f));
            TMP_Text mailText = UIFactory.CreateText("MailLabel", mail.transform, "MAIL", 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(mailText.rectTransform, new Vector2(4f, 48f), new Vector2(-4f, 2f));
            _mailBadge = UIFactory.CreateUIObject("Badge", mail.transform).AddComponent<NotificationBadgeView>();
            RectTransform badgeRect = _mailBadge.GetComponent<RectTransform>();
            UIFactory.SetAnchors(badgeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(28f, 28f), new Vector2(-6f, -6f));

            GameObject settings = UIFactory.CreateUIObject("Settings", panel.ContentRoot);
            UIFactory.AddLayoutElement(settings, preferredWidth: 96f, preferredHeight: 76f, minWidth: 96f);
            Image settingsImage = settings.AddComponent<Image>();
            settingsImage.color = UITheme.Gunmetal;
            Image settingsIcon = UIFactory.CreateUIObject("SettingsIcon", settings.transform).AddComponent<Image>();
            settingsIcon.raycastTarget = false;
            bool hasSettingsIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(settingsIcon, art != null ? art.IconSettings : null, Color.clear, "Icon_Settings");
            settingsIcon.enabled = hasSettingsIcon;
            UIFactory.Stretch(settingsIcon.rectTransform, new Vector2(30f, 26f), new Vector2(-30f, -18f));
            TMP_Text settingsText = UIFactory.CreateText("SettingsText", settings.transform, UILocalization.Get("topbar.settings", "SETTINGS"), 16, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(settingsText.rectTransform, new Vector2(4f, 48f), new Vector2(-4f, 2f));

            _isBuilt = true;
        }

        public void Bind(TopBarData data)
        {
            Build();
            _identityText.text = $"{data.commanderName}  LV.{data.playerLevel}";
            _resourceA.text = $"{UILocalization.Get("topbar.energy", "ENERGY")}  {data.energy}/{data.maxEnergy}";
            _resourceB.text = $"{UILocalization.Get("topbar.gold", "GOLD")}  {data.gold:N0}";
            _resourceC.text = data.showPremiumCurrency
                ? $"{UILocalization.Get("topbar.gems", "GEMS")}  {data.premiumCurrency:N0}"
                : $"{UILocalization.Get("topbar.mail", "MAIL")}  {data.mailCount}";

            _mailBadge.SetCount(data.mailCount);
        }

        TMP_Text CreateResourceLabel(Transform parent, string name, Sprite iconSprite, string assetName)
        {
            GameObject go = UIFactory.CreateUIObject(name, parent);
            UIFactory.AddLayoutElement(go, preferredWidth: 170f, preferredHeight: 68f, minWidth: 150f);
            Image background = go.AddComponent<Image>();
            background.color = UITheme.Gunmetal;
            Image icon = UIFactory.CreateUIObject("Icon", go.transform).AddComponent<Image>();
            icon.raycastTarget = false;
            bool hasIcon = UIConstants.UseIconSprites && UIArtLibrary.TryApply(icon, iconSprite, Color.clear, assetName);
            icon.enabled = hasIcon;
            UIFactory.Stretch(icon.rectTransform, new Vector2(10f, 16f), new Vector2(-128f, -16f));
            TMP_Text label = UIFactory.CreateText($"{name}Text", go.transform, string.Empty, 17, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 13f;
            label.fontSizeMax = 17f;
            UIFactory.Stretch(label.rectTransform, new Vector2(44f, 8f), new Vector2(-8f, -8f));
            return label;
        }
    }
}
