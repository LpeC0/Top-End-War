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
            _headerTitle.text = UILocalization.Get("world.header.title", "WORLD MAP");
            _worldSummaryText.text = $"WORLD {world.worldId}\n{world.worldName}\nCURRENT {world.currentStageId:00} / TOTAL {world.stageCount:00}";
            _progressText.text = $"{UILocalization.Get("world.progress", "WORLD PROGRESS")}  {world.completedStages}/{world.stageCount}  |  BOSS  {world.bossStageId:00}";
            _statusText.text = $"Route focus: stage {world.currentStageId:00} to boss {world.bossStageId:00}. Nearby nodes expanded for readability.";

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
                float size = node.isCurrent ? 104f : node.isBoss ? 92f : 64f;
                UIFactory.SetAnchors(rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(size, size), node.anchoredPosition);
                WorldNodeView view = UIFactory.GetOrAdd<WorldNodeView>(nodeGo);
                view.Bind(node, () => HandleNodeClick(node));
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
            float width = 860f;
            float height = 320f;
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
            float x = Mathf.Lerp(40f, width, t);
            float y;

            switch (layoutTemplateId)
            {
                case "zigzag":
                    y = 160f + Mathf.Sin(t * Mathf.PI * 4f) * 95f;
                    break;
                case "switchback":
                    y = 80f + Mathf.PingPong(t * 320f, 220f);
                    break;
                default:
                    y = 160f + Mathf.Sin(t * Mathf.PI * 2f) * 90f;
                    break;
            }

            return new Vector2(x, y);
        }

        void Build()
        {
            if (_headerTitle != null)
            {
                return;
            }

            RectTransform root = (RectTransform)transform;
            UIFactory.Stretch(root, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = UIFactory.AddVerticalLayout(gameObject, 16f, TextAnchor.UpperCenter, true, false);
            layout.childForceExpandHeight = false;

            PanelBaseView header = UIFactory.CreateUIObject("TopArea", transform).AddComponent<PanelBaseView>();
            header.Build(18f);
            UIFactory.AddLayoutElement(header.gameObject, preferredHeight: 140f, minHeight: 140f);
            VerticalLayoutGroup headerLayout = UIFactory.AddVerticalLayout(header.ContentRoot.gameObject, 8f, TextAnchor.UpperLeft, true, false);
            _headerTitle = UIFactory.CreateText("HeaderTitle", header.ContentRoot, string.Empty, 32, UITheme.SoftCream, FontStyles.Bold);
            _worldSummaryText = UIFactory.CreateText("WorldSummary", header.ContentRoot, string.Empty, 22, UITheme.TextSecondary, FontStyles.Bold);
            UIFactory.ConfigureTextBlock(_headerTitle, 36f, true, 20f, 32f);
            UIFactory.ConfigureTextBlock(_worldSummaryText, 78f, true, 16f, 22f);

            PanelBaseView mapPanel = UIFactory.CreateUIObject("ContentArea", transform).AddComponent<PanelBaseView>();
            mapPanel.Build(18f);
            UIFactory.AddLayoutElement(mapPanel.gameObject, preferredHeight: 560f, flexibleHeight: 1f, minHeight: 520f);
            VerticalLayoutGroup mapLayout = UIFactory.AddVerticalLayout(mapPanel.ContentRoot.gameObject, 12f, TextAnchor.UpperCenter, true, false);
            TMP_Text mapTitle = UIFactory.CreateText("MapTitle", mapPanel.ContentRoot, "ACTIVE ROUTE", 18, UITheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(mapTitle, 24f);

            GameObject viewport = UIFactory.CreateUIObject("Viewport", mapPanel.ContentRoot);
            _routeBackdrop = viewport.AddComponent<Image>();
            _routeBackdrop.color = UITheme.Gunmetal;
            UIFactory.AddLayoutElement(viewport, preferredHeight: 420f, minHeight: 380f);

            GameObject routeBand = UIFactory.CreateUIObject("RouteBand", viewport.transform);
            Image routeBandImage = routeBand.AddComponent<Image>();
            routeBandImage.color = new Color(UITheme.Teal.r, UITheme.Teal.g, UITheme.Teal.b, 0.18f);
            RectTransform routeBandRect = routeBand.GetComponent<RectTransform>();
            routeBandRect.anchorMin = new Vector2(0.08f, 0.48f);
            routeBandRect.anchorMax = new Vector2(0.92f, 0.56f);
            routeBandRect.offsetMin = Vector2.zero;
            routeBandRect.offsetMax = Vector2.zero;

            _nodeContainer = UIFactory.CreateUIObject("NodeContainer", viewport.transform).GetComponent<RectTransform>();
            UIFactory.Stretch(_nodeContainer, new Vector2(20f, 20f), new Vector2(-20f, -20f));

            PanelBaseView utility = UIFactory.CreateUIObject("UtilityRail", transform).AddComponent<PanelBaseView>();
            utility.Build(18f);
            UIFactory.AddLayoutElement(utility.gameObject, preferredHeight: 96f, minHeight: 96f);
            HorizontalLayoutGroup utilityLayout = UIFactory.AddHorizontalLayout(utility.ContentRoot.gameObject, 12f, TextAnchor.MiddleCenter, true, false);
            utilityLayout.childForceExpandHeight = false;
            CreateUtilityButton(utility.ContentRoot, "world.utility.mail");
            CreateUtilityButton(utility.ContentRoot, "world.utility.missions");

            PanelBaseView progress = UIFactory.CreateUIObject("BottomArea", transform).AddComponent<PanelBaseView>();
            progress.Build(18f);
            UIFactory.AddLayoutElement(progress.gameObject, preferredHeight: 100f, minHeight: 100f);
            _progressText = UIFactory.CreateText("ProgressText", progress.ContentRoot, string.Empty, 24, UITheme.SoftCream, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_progressText, 36f, true, 18f, 24f);
            _statusText = UIFactory.CreateText("StatusText", progress.ContentRoot, string.Empty, 18, UITheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.ConfigureTextBlock(_statusText, 36f, true, 15f, 18f);
        }

        void CreateUtilityButton(Transform parent, string key)
        {
            GameObject go = UIFactory.CreateUIObject(key, parent);
            UIFactory.AddLayoutElement(go, flexibleWidth: 1f, preferredHeight: 64f, minHeight: 60f);
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
    }
}
