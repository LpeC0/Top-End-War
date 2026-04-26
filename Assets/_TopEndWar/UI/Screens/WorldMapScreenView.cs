using System.Collections.Generic;
using TMPro;
using TopEndWar.UI.Components;
using TopEndWar.UI.Core;
using TopEndWar.UI.Data;
using TopEndWar.UI.Localization;
using TopEndWar.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace TopEndWar.UI.Screens
{
    public class WorldMapScreenView : MonoBehaviour
    {
        UIScreenManager _screenManager;
        MockUIDataProvider _dataProvider;

        TMP_Text _headerTitle;
        TMP_Text _worldSummaryText;
        TMP_Text _progressText;
        TMP_Text _statusText;
        Image _routeBackdrop;
        Image _mapImage;
        GameObject _mapPlaceholder;
        RectTransform _nodeContainer;
        readonly List<GameObject> _spawnedNodeObjects = new List<GameObject>();

        public void Initialize(UIScreenManager screenManager, MockUIDataProvider dataProvider)
        {
            _screenManager = screenManager;
            _dataProvider = dataProvider;
            Build();
            RefreshView();
        }

        public void RefreshView()
        {
            TopEndWar.UI.Data.WorldConfig world = _dataProvider.GetCurrentWorld();
            _headerTitle.text = $"WORLD {world.worldId}";
            _worldSummaryText.text = $"{world.worldName}\nCURRENT {world.currentStageId:00} / TOTAL {world.stageCount:00}";
            _progressText.text = $"{UILocalization.Get("world.progress", "WORLD PROGRESS")}  {world.completedStages}/{world.stageCount}  |  BOSS  {world.bossStageId:00}";
            _statusText.text = $"Current route: stage {world.currentStageId:00} to boss {world.bossStageId:00}.";
            BindMapBackground(world);
            Canvas.ForceUpdateCanvases();

            List<StageNodeData> visibleNodes = GetVisibleNodes(BuildNodes(world), world);
            while (_spawnedNodeObjects.Count < visibleNodes.Count)
            {
                GameObject nodeGo = UIFactory.CreateUIObject("Node", _nodeContainer);
                _spawnedNodeObjects.Add(nodeGo);
            }

            for (int i = 0; i < _spawnedNodeObjects.Count; i++)
            {
                bool shouldBeActive = i < visibleNodes.Count;
                GameObject nodeGo = _spawnedNodeObjects[i];
                nodeGo.SetActive(shouldBeActive);
                if (!shouldBeActive)
                {
                    continue;
                }

                StageNodeData node = visibleNodes[i];
                nodeGo.name = $"Node_{node.stageId:00}";
                RectTransform rect = nodeGo.GetComponent<RectTransform>();
                float size = node.isCurrent ? 104f : node.isBoss ? 94f : node.isLocked ? 50f : 58f;
                UIFactory.SetAnchors(rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(size, size), node.anchoredPosition);
                WorldNodeView view = UIFactory.GetOrAdd<WorldNodeView>(nodeGo);
                view.Bind(node, () => HandleNodeClick(node));
                rect.SetAsLastSibling();
            }
        }

        public void DebugSelectWorld(int worldId)
        {
            _dataProvider.SelectWorldById(worldId);
            RefreshView();
        }

        void HandleNodeClick(StageNodeData node)
        {
            _screenManager.ActionRouter.HandleWorldNode(node);
            if (node.isLocked)
            {
                _statusText.text = UILocalization.Get("world.locked", "STAGE LOCKED");
            }
        }

        List<StageNodeData> BuildNodes(TopEndWar.UI.Data.WorldConfig world)
        {
            List<StageNodeData> nodes = new List<StageNodeData>();
            Rect rect = _nodeContainer != null ? _nodeContainer.rect : new Rect(0f, 0f, 960f, 1280f);
            float width = Mathf.Max(720f, rect.width);
            float height = Mathf.Max(860f, rect.height);
            int denominator = Mathf.Max(1, world.stageCount - 1);

            for (int i = 1; i <= world.stageCount; i++)
            {
                float progressT = (i - 1) / (float)denominator;
                Vector2 position = EvaluateLayout(world.layoutTemplateId, progressT, width, height);
                bool isCompleted = i <= world.completedStages;
                bool isCurrent = i == world.currentStageId;
                bool isBoss = i == world.bossStageId;
                bool isUnlocked = i <= world.currentStageId;

                nodes.Add(new StageNodeData
                {
                    stageId = i,
                    anchoredPosition = position,
                    isBoss = isBoss,
                    isCurrent = isCurrent,
                    isCompleted = isCompleted,
                    isLocked = !isUnlocked,
                    isUnlocked = isUnlocked
                });
            }

            return nodes;
        }

        List<StageNodeData> GetVisibleNodes(List<StageNodeData> allNodes, TopEndWar.UI.Data.WorldConfig world)
        {
            HashSet<int> keepIds = new HashSet<int>();
            keepIds.Add(1);
            keepIds.Add(world.bossStageId);

            int min = Mathf.Max(1, world.currentStageId - 4);
            int max = Mathf.Min(world.stageCount, world.currentStageId + 4);
            for (int i = min; i <= max; i++)
            {
                keepIds.Add(i);
            }

            List<StageNodeData> filtered = new List<StageNodeData>();
            foreach (StageNodeData node in allNodes)
            {
                if (keepIds.Contains(node.stageId))
                {
                    filtered.Add(node);
                }
            }

            return filtered;
        }

        Vector2 EvaluateLayout(string layoutTemplateId, float t, float width, float height)
        {
            if (layoutTemplateId == "arc")
            {
                return EvaluateWorldOnePath(t, width, height);
            }

            float x = Mathf.Lerp(width * 0.12f, width * 0.88f, t);
            float centerY = height * 0.48f;
            float amplitude = height * 0.18f;
            float y = centerY;

            if (layoutTemplateId == "zigzag")
            {
                y = centerY + Mathf.Sin(t * Mathf.PI * 4f) * amplitude;
            }
            else if (layoutTemplateId == "switchback")
            {
                y = Mathf.Lerp(height * 0.16f, height * 0.84f, Mathf.PingPong(t * 3f, 1f));
            }
            else
            {
                y = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
            }

            return new Vector2(x, y);
        }

        Vector2 EvaluateWorldOnePath(float t, float width, float height)
        {
            Vector2[] points =
            {
                new Vector2(0.50f, 0.92f),
                new Vector2(0.48f, 0.78f),
                new Vector2(0.55f, 0.63f),
                new Vector2(0.43f, 0.50f),
                new Vector2(0.55f, 0.38f),
                new Vector2(0.70f, 0.25f),
                new Vector2(0.73f, 0.12f)
            };

            float scaled = Mathf.Clamp01(t) * (points.Length - 1);
            int index = Mathf.Min(points.Length - 2, Mathf.FloorToInt(scaled));
            float localT = scaled - index;
            Vector2 normalized = Vector2.Lerp(points[index], points[index + 1], SmoothStep(localT));

            float horizontalInset = 56f;
            float topInset = 76f;
            float bottomInset = 92f;
            float usableWidth = Mathf.Max(1f, width - horizontalInset * 2f);
            float usableHeight = Mathf.Max(1f, height - topInset - bottomInset);
            float x = horizontalInset + normalized.x * usableWidth;
            float y = bottomInset + (1f - normalized.y) * usableHeight;
            return new Vector2(x, y);
        }

        float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        void Build()
        {
            if (_headerTitle != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);

            GameObject viewport = UIFactory.CreateUIObject("MapViewport", transform);
            _routeBackdrop = viewport.AddComponent<Image>();
            _routeBackdrop.color = UITheme.DeepNavy;
            UIFactory.Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject mapImageGo = UIFactory.CreateUIObject("MapImage", viewport.transform);
            _mapImage = mapImageGo.AddComponent<Image>();
            _mapImage.preserveAspect = true;
            _mapImage.color = Color.white;
            _mapImage.raycastTarget = false;
            RectTransform mapImageRect = mapImageGo.GetComponent<RectTransform>();
            mapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapImageRect.pivot = new Vector2(0.5f, 0.5f);
            mapImageRect.anchoredPosition = Vector2.zero;

            _mapPlaceholder = UIFactory.CreateUIObject("MapPlaceholder", viewport.transform);
            RectTransform placeholderRect = _mapPlaceholder.GetComponent<RectTransform>();
            UIFactory.Stretch(placeholderRect, Vector2.zero, Vector2.zero);
            Image placeholderImage = _mapPlaceholder.AddComponent<Image>();
            placeholderImage.color = new Color(UITheme.TealDark.r, UITheme.TealDark.g, UITheme.TealDark.b, 0.45f);
            TMP_Text placeholderText = UIFactory.CreateText("PlaceholderText", _mapPlaceholder.transform, "WORLD MAP ART\nPLACEHOLDER", 28, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(placeholderText.rectTransform, new Vector2(32f, 32f), new Vector2(-32f, -32f));
            placeholderText.enableAutoSizing = true;
            placeholderText.fontSizeMin = 18f;
            placeholderText.fontSizeMax = 28f;

            _nodeContainer = UIFactory.CreateUIObject("NodeContainer", viewport.transform).GetComponent<RectTransform>();
            UIFactory.Stretch(_nodeContainer, Vector2.zero, Vector2.zero);

            PanelBaseView header = UIFactory.CreateUIObject("WorldTitleOverlay", transform).AddComponent<PanelBaseView>();
            header.Build(14f, PanelVisualStyle.Dark);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(430f, 116f);
            headerRect.anchoredPosition = new Vector2(18f, -18f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(header.ContentRoot.gameObject, 2f, TextAnchor.UpperLeft, true, false);
            headerLayout.childForceExpandHeight = false;
            _headerTitle = UIFactory.CreateText("HeaderTitle", header.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold);
            _worldSummaryText = UIFactory.CreateText("WorldSummary", header.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_headerTitle, 30f, true, 18f, 24f);
            UIFactory.ConfigureTextBlock(_worldSummaryText, 56f, true, 14f, 18f);

            GameObject utility = UIFactory.CreateUIObject("UtilityOverlay", transform);
            RectTransform utilityRect = utility.GetComponent<RectTransform>();
            utilityRect.anchorMin = new Vector2(1f, 1f);
            utilityRect.anchorMax = new Vector2(1f, 1f);
            utilityRect.pivot = new Vector2(1f, 1f);
            utilityRect.sizeDelta = new Vector2(360f, 68f);
            utilityRect.anchoredPosition = new Vector2(-18f, -18f);
            HorizontalLayoutGroup utilityLayout = UIFactory.AddHorizontalLayout(utility, 10f, TextAnchor.MiddleRight, true, false);
            utilityLayout.childForceExpandHeight = false;
            CreateUtilityButton(utility.transform, "world.utility.mail");
            CreateUtilityButton(utility.transform, "world.utility.missions");

            PanelBaseView progress = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            progress.Build(12f, PanelVisualStyle.Dark);
            RectTransform progressRect = progress.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0f, 0f);
            progressRect.anchorMax = new Vector2(1f, 0f);
            progressRect.pivot = new Vector2(0.5f, 0f);
            progressRect.offsetMin = new Vector2(18f, 18f);
            progressRect.offsetMax = new Vector2(-18f, 100f);
            VerticalLayoutGroup progressLayout = UIFactory.AddVerticalLayout(progress.ContentRoot.gameObject, 2f, TextAnchor.MiddleCenter, true, false);
            progressLayout.childForceExpandHeight = false;
            _progressText = UIFactory.CreateText("ProgressText", progress.ContentRoot, string.Empty, 20, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_progressText, 30f, true, 15f, 20f);
            _statusText = UIFactory.CreateText("StatusText", progress.ContentRoot, string.Empty, 16, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 26f, true, 13f, 16f);
        }

        void CreateUtilityButton(Transform parent, string key)
        {
            GameObject go = UIFactory.CreateUIObject(key, parent);
            UIFactory.AddLayoutElement(go, flexibleWidth: 1f, preferredHeight: 58f, minHeight: 54f);
            PrimaryButtonView button = go.AddComponent<PrimaryButtonView>();
            button.Build(ButtonVisualStyle.Tab);
            button.SetLabelKey(key, key);
            button.SetOnClick(() =>
            {
                if (key == "world.utility.missions")
                {
                    _screenManager.ActionRouter.OpenDailyMissions();
                    _statusText.text = "Daily Missions coming soon";
                    return;
                }

                _statusText.text = $"{UILocalization.Get(key, key)} ready.";
            });
        }

        void BindMapBackground(TopEndWar.UI.Data.WorldConfig world)
        {
            Sprite sprite = LoadWorldMapSprite(world.worldId);
            bool hasSprite = sprite != null;
            _mapImage.sprite = sprite;
            _mapImage.enabled = hasSprite;
            if (hasSprite)
            {
                FitMapImageToViewport(sprite);
            }

            if (_mapPlaceholder != null)
            {
                _mapPlaceholder.SetActive(!hasSprite);
            }

            if (!hasSprite)
            {
                UIArtLibrary.WarnMissing($"World_{world.worldId:00}_Map_Viewport");
            }
        }

        Sprite LoadWorldMapSprite(int worldId)
        {
            if (!UIConstants.UseWorldMapSprite)
            {
                return null;
            }

            UIArtLibrary art = UIArtLibrary.Instance;
            if (art == null)
            {
                return null;
            }

            if (worldId == 1 && art.World01MapMaster != null)
            {
                return art.World01MapMaster;
            }

            return worldId == 1 ? art.World01MapViewport : null;
        }

        void FitMapImageToViewport(Sprite sprite)
        {
            if (_mapImage == null || sprite == null)
            {
                return;
            }

            RectTransform imageRect = _mapImage.rectTransform;
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null || sprite.rect.height <= 0f)
            {
                return;
            }

            float parentWidth = Mathf.Max(1f, parentRect.rect.width);
            float parentHeight = Mathf.Max(1f, parentRect.rect.height);
            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float parentAspect = parentWidth / parentHeight;
            float width = parentWidth;
            float height = parentHeight;

            if (spriteAspect > parentAspect)
            {
                height = parentHeight;
                width = height * spriteAspect;
            }
            else
            {
                width = parentWidth;
                height = width / spriteAspect;
            }

            imageRect.sizeDelta = new Vector2(width, height);
            imageRect.anchoredPosition = Vector2.zero;
        }
    }
}
