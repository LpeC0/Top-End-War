using TopEndWar.UI.Core;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Components
{
    public class PanelBaseView : MonoBehaviour
    {
        public RectTransform ContentRoot { get; private set; }

        bool _isBuilt;

        public void Build(float padding = 18f)
        {
            if (_isBuilt)
            {
                return;
            }

            // UI: Shared panel shell for the warm heroic diorama look.
            Image background = UIFactory.GetOrAdd<Image>(gameObject);
            background.color = UITheme.NavyPanel;

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
    }
}
