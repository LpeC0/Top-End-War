using System.Collections.Generic;
using TopEndWar.UI.Localization;
using UnityEngine;

namespace TopEndWar.UI.Data
{
    public class MockUIDataProvider : MonoBehaviour
    {
        // DATA-BINDING: UI reads current state from this provider so screens stay decoupled from gameplay scene code.
        readonly List<WorldConfig> _worlds = new List<WorldConfig>();
        readonly PlayerProgress _playerProgress = new PlayerProgress();

        int _selectedWorldIndex;
        int _selectedStageId;

        public bool ResultPreviewVictory { get; private set; } = true;
        public int SelectedStageId => _selectedStageId;
        public PlayerProgress CurrentProgress => _playerProgress;

        void Awake()
        {
            EnsureSeeded();
        }

        public HomeScreenData GetHomeScreenData()
        {
            EnsureSeeded();
            SaveManager save = SaveManager.Instance;
            WorldConfig world = GetCurrentWorld();

            return new HomeScreenData
            {
                currentWorldName = world.worldName,
                currentStageId = _playerProgress.currentStageId,
                currentStageName = GetStageName(_playerProgress.currentStageId),
                completedStages = world.completedStages,
                totalStages = world.stageCount,
                playerPower = 28450,
                targetPower = 32000,
                powerState = "stage.underpowered",
                upgradeRecommended = true,
                playerLevel = 18,
                energy = 72,
                gold = save != null ? Mathf.Max(14500, save.HighScoreCP) : 14500,
                premiumCurrency = 320,
                mailCount = 3,
                totalRuns = save != null ? save.TotalRuns : 17
            };
        }

        public TopBarData GetTopBarData()
        {
            EnsureSeeded();
            HomeScreenData home = GetHomeScreenData();
            return new TopBarData
            {
                commanderName = "Cmdr. Voss",
                playerLevel = home.playerLevel,
                energy = home.energy,
                gold = home.gold,
                premiumCurrency = home.premiumCurrency,
                mailCount = home.mailCount,
                showPremiumCurrency = true
            };
        }

        public IReadOnlyList<WorldConfig> GetWorlds()
        {
            EnsureSeeded();
            return _worlds;
        }

        public WorldConfig GetCurrentWorld()
        {
            EnsureSeeded();
            return _worlds[Mathf.Clamp(_selectedWorldIndex, 0, _worlds.Count - 1)];
        }

        public void SelectWorldById(int worldId)
        {
            EnsureSeeded();
            for (int i = 0; i < _worlds.Count; i++)
            {
                if (_worlds[i].worldId == worldId)
                {
                    _selectedWorldIndex = i;
                    _playerProgress.currentWorldId = _worlds[i].worldId;
                    _playerProgress.currentStageId = _worlds[i].currentStageId;
                    _playerProgress.completedStages = _worlds[i].completedStages;
                    _selectedStageId = _playerProgress.currentStageId;
                    return;
                }
            }
        }

        public void SelectStage(int stageId)
        {
            EnsureSeeded();
            _selectedStageId = Mathf.Clamp(stageId, 1, GetCurrentWorld().stageCount);
        }

        public StageDetailData GetStageDetailData()
        {
            EnsureSeeded();
            WorldConfig world = GetCurrentWorld();
            int stageId = Mathf.Clamp(_selectedStageId, 1, world.stageCount);
            bool isBossStage = stageId == world.bossStageId;
            int targetPower = 27000 + (stageId * 450);
            int playerPower = 28450;

            return new StageDetailData
            {
                worldId = world.worldId,
                stageId = stageId,
                stageName = GetStageName(stageId),
                playerPower = playerPower,
                targetPower = targetPower,
                powerStateKey = playerPower >= targetPower ? "stage.ready" : stageId < world.currentStageId ? "stage.risky" : "stage.underpowered",
                isBossStage = isBossStage,
                hasFirstClearBonus = stageId >= world.currentStageId,
                loadoutName = "Assault Grid / Mk-II",
                briefingText = isBossStage ? "Fortified enemy command node. Expect armor and artillery pressure." : "Advance through the canyon approach and secure the outpost lane.",
                threatKeys = new List<string> { "Armored", "Swarm", isBossStage ? "Boss" : "Mid Range" },
                enemyNames = isBossStage ? new List<string> { "Shield Trooper", "Rocket Crew", "Boss Walker" } : new List<string> { "Rifle Squad", "Turret Nest", "Scout Bike" },
                rewards = new List<RewardItemData>
                {
                    new RewardItemData { labelKey = "topbar.gold", fallbackLabel = "Gold", amount = 1250 + (stageId * 20), accent = "gold" },
                    new RewardItemData { labelKey = "equipment.tech_core", fallbackLabel = "Tech Core", amount = isBossStage ? 3 : 1, accent = "teal" }
                },
                firstClearRewards = new List<RewardItemData>
                {
                    new RewardItemData { labelKey = "equipment.gear_box", fallbackLabel = "Gear Box", amount = 1, accent = "purple" },
                    new RewardItemData { labelKey = "topbar.gems", fallbackLabel = "Gems", amount = 40, accent = "teal" }
                }
            };
        }

        public CommanderScreenData GetCommanderScreenData()
        {
            EnsureSeeded();
            return new CommanderScreenData
            {
                commanderName = "Commander Hale",
                totalPower = 28450,
                hp = 12800,
                dps = 3410,
                defense = 1760,
                roleDescription = "Assault leader tuned for mid-range pressure and squad sustain.",
                slots = new List<EquipmentSlotData>
                {
                    new EquipmentSlotData { slotKey = "equipment.weapon", itemName = "VX Assault Rifle", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.armor", itemName = "Frontier Plate", state = "upgradeable" },
                    new EquipmentSlotData { slotKey = "equipment.helmet", itemName = "Recon Helm", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.boots", itemName = "Rapid Boots", state = "equipped" },
                    new EquipmentSlotData { slotKey = "equipment.tech_core", itemName = "Mk-IV Core", state = "upgradeable" },
                    new EquipmentSlotData { slotKey = "equipment.gear_box", itemName = "Field Relay", state = "empty" },
                    new EquipmentSlotData { slotKey = "equipment.drone", itemName = "Unlock at HQ Lv.20", state = "locked" },
                    new EquipmentSlotData { slotKey = "equipment.support_gear", itemName = "Unlock at HQ Lv.24", state = "locked" },
                    new EquipmentSlotData { slotKey = "equipment.emblem", itemName = "Unlock at HQ Lv.28", state = "locked" }
                },
                squadMembers = new List<string> { "Alpha Squad", "Bulwark Team", "Medic Pair", "Drone Crew" }
            };
        }

        public ResultScreenData GetResultScreenData()
        {
            EnsureSeeded();
            StageDetailData stage = GetStageDetailData();
            return ResultPreviewVictory
                ? new ResultScreenData
                {
                    isVictory = true,
                    stageName = stage.stageName,
                    stars = 3,
                    recommendation = "Push the next stage or upgrade armor for a safer clear.",
                    hasFirstClearBonus = stage.hasFirstClearBonus,
                    performanceGoals = new List<string> { "No retreat used", "Commander HP above 50%", "Elite wave cleared fast" },
                    rewards = new List<RewardItemData>(stage.rewards),
                    firstClearRewards = new List<RewardItemData>(stage.firstClearRewards)
                }
                : new ResultScreenData
                {
                    isVictory = false,
                    stageName = stage.stageName,
                    stars = 0,
                    failureReason = "Armor line collapsed during the rocket volley.",
                    recommendation = "Upgrade armor and tech core before retrying this lane.",
                    hasFirstClearBonus = false,
                    rewards = new List<RewardItemData>
                    {
                        new RewardItemData { labelKey = "topbar.gold", fallbackLabel = "Gold", amount = 420, accent = "gold" }
                    }
                };
        }

        public void SetResultPreview(bool victory)
        {
            ResultPreviewVictory = victory;
        }

        public string GetStageName(int stageId)
        {
            EnsureSeeded();
            if (stageId == 12)
            {
                return "Canyon Outpost";
            }

            if (stageId == GetCurrentWorld().bossStageId)
            {
                return "Citadel Breach";
            }

            return $"Stage {stageId:00}";
        }

        void SeedData()
        {
            _worlds.Clear();
            _worlds.Add(new WorldConfig
            {
                worldId = 1,
                worldName = "Frontier Conflict",
                stageCount = 35,
                completedStages = 11,
                currentStageId = 12,
                bossStageId = 35,
                layoutTemplateId = "arc"
            });
            _worlds.Add(new WorldConfig
            {
                worldId = 2,
                worldName = "Iron Tundra",
                stageCount = 24,
                completedStages = 9,
                currentStageId = 10,
                bossStageId = 24,
                layoutTemplateId = "zigzag"
            });
            _worlds.Add(new WorldConfig
            {
                worldId = 5,
                worldName = "Ashen Ring",
                stageCount = 45,
                completedStages = 18,
                currentStageId = 19,
                bossStageId = 45,
                layoutTemplateId = "switchback"
            });

            SelectWorldById(1);
        }

        void EnsureSeeded()
        {
            if (_worlds.Count == 0)
            {
                SeedData();
            }
        }
    }
}
