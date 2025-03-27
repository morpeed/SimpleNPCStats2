using SimpleNPCStats2.Common.Config.UI;
using SimpleNPCStats2.Common.Config.UI.NPCUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SimpleNPCStats2.Common.Config
{
    public class ConfigSystem : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;
        public static ConfigSystem Instance { get; private set; }

        public ConfigData data = new();

        public static ConfigData Data => Instance.data;

        // netID / data
        public static Dictionary<int, ConfigData.NPCGroup.StatSet> StaticNPCData { get; private set; }

        private void SaveStaticNPCData()
        {
            StaticNPCData = [];

            var npcGroups = data.NPCGroups;

            for (int i = npcGroups.Count - 1; i >= 0; i--)
            {
                var group = npcGroups[i];

                foreach (var netId in group.GetNPCs())
                {
                    StaticNPCData[netId] = group.stats;
                }
            }

            Mod.Logger.Debug($"Successfully saved {nameof(StaticNPCData)} with {StaticNPCData.Count} entries.");
        }

        public override void OnChanged()
        {
            SaveStaticNPCData();
        }

        public override void OnLoaded() => Instance = this;
    }

    public class ConfigSystemAdvanced : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;
        public static ConfigSystemAdvanced Instance { get; private set; }

        [Header("ProjectileHeader")]

        [DefaultValue(true)]
        public bool projectileRelativeDamage;

        [DefaultValue(true)]
        public bool projectileRelativeScale;

        [DefaultValue(true)]
        public bool projectileUseMovementSpeed;

        [DefaultValue(true)]
        public bool projectileUseAISpeed;


        [Header("MiscHeader")]

        [DefaultValue(true)]
        public bool overrideModifyAI;

        public override void OnLoaded() => Instance = this;
    }
}
