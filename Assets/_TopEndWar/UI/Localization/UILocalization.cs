using System.Collections.Generic;

namespace TopEndWar.UI.Localization
{
    public static class UILocalization
    {
        // LOCALIZATION: Minimal dictionary wrapper with fallback support.
        static readonly Dictionary<string, string> Entries = new Dictionary<string, string>
        {
            { "nav.home", "HOME" },
            { "nav.map", "MAP" },
            { "nav.commander", "COMMANDER" },
            { "nav.events", "EVENTS" },
            { "nav.shop", "SHOP" },
            { "topbar.energy", "ENERGY" },
            { "topbar.gold", "GOLD" },
            { "topbar.gems", "GEMS" },
            { "topbar.mail", "MAIL" },
            { "topbar.settings", "SETTINGS" },
            { "home.header.title", "HOME / HQ" },
            { "home.header.subtitle", "Frontline command, squad upkeep, and campaign control." },
            { "home.continue.cta", "CONTINUE CAMPAIGN" },
            { "home.quick.free_reward", "FREE REWARD" },
            { "home.quick.daily", "DAILY MISSIONS" },
            { "home.quick.upgrade", "UPGRADE" },
            { "home.quick.event", "EVENT" },
            { "home.claim.notice", "Claim notice available at HQ dispatch." },
            { "home.power.status", "POWER STATUS" },
            { "world.header.title", "WORLD MAP" },
            { "world.progress", "WORLD PROGRESS" },
            { "world.utility.mail", "MAIL" },
            { "world.utility.missions", "MISSIONS" },
            { "world.locked", "STAGE LOCKED" },
            { "world.preview.w1", "WORLD 1" },
            { "world.preview.w2", "WORLD 2" },
            { "world.preview.w5", "WORLD 5" },
            { "stage.header.title", "STAGE DETAIL" },
            { "stage.start_run", "START RUN" },
            { "stage.first_clear", "FIRST CLEAR BONUS" },
            { "stage.loadout", "ACTIVE LOADOUT" },
            { "stage.ready", "READY" },
            { "stage.risky", "RISKY" },
            { "stage.underpowered", "UNDERPOWERED" },
            { "stage.boss", "BOSS STAGE" },
            { "stage.section.enemies", "ENEMY PREVIEW" },
            { "stage.section.rewards", "REWARDS" },
            { "stage.section.threats", "THREAT TAGS" },
            { "commander.header.title", "COMMANDER / EQUIPMENT" },
            { "commander.loadout_tab", "LOADOUT" },
            { "commander.skills_tab", "SKILLS" },
            { "commander.stats_tab", "STATS" },
            { "commander.auto_equip", "AUTO EQUIP" },
            { "commander.upgrade", "UPGRADE" },
            { "result.victory", "STAGE CLEAR" },
            { "result.defeat", "DEFEAT" },
            { "result.next_stage", "NEXT STAGE" },
            { "result.retry_stage", "RETRY STAGE" },
            { "result.world_map", "WORLD MAP" },
            { "result.preview.victory", "PREVIEW VICTORY" },
            { "result.preview.defeat", "PREVIEW DEFEAT" },
            { "result.secondary.upgrade", "UPGRADE" },
            { "result.secondary.retry", "RETRY STAGE" },
            { "result.recommendation", "RECOMMENDATION" },
            { "equipment.weapon", "WEAPON" },
            { "equipment.armor", "ARMOR" },
            { "equipment.helmet", "HELMET" },
            { "equipment.boots", "BOOTS" },
            { "equipment.tech_core", "TECH CORE" },
            { "equipment.gear_box", "GEAR BOX" },
            { "equipment.drone", "DRONE" },
            { "equipment.support_gear", "SUPPORT GEAR" },
            { "equipment.emblem", "EMBLEM / CHIP" }
        };

        public static string Get(string key, string fallback = null)
        {
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out string value))
            {
                return value;
            }

            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }

        public static bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && Entries.ContainsKey(key);
        }
    }
}
