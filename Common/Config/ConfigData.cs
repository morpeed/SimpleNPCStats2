using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using SimpleNPCStats2.Common.Config.UI;
using SimpleNPCStats2.Common.Core;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Globalization;
using Terraria.ModLoader.IO;

namespace SimpleNPCStats2.Common.Config
{
    [CustomModConfigItem(typeof(ConfigDataElement))]
    public class ConfigData
    {
        public List<NPCGroup> NPCGroups = new();

        public class NPCGroup
        {
            public HashSet<int> NPCs { get; private set; } = new HashSet<int>();
            public Dictionary<string, HashSet<string>> ModNPCs { get; private set; } = new Dictionary<string, HashSet<string>>();
            public StatSet stats = new StatSet();
            public string name;

            public NPCGroup Clone()
            {
                return new NPCGroup(name)
                {
                    NPCs = new HashSet<int>(NPCs),
                    ModNPCs = new Dictionary<string, HashSet<string>>(ModNPCs),
                    stats = stats.Clone()
                };
            }

            private HashSet<int> _npcsCache;
            private void ResetCache()
            {
                _npcsCache = null;
            }

            [OnSerializing]
            private void OnSerializing(StreamingContext context)
            {
                Fix();
            }
            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                Fix();
            }
            private void Fix()
            {
                NPCs.RemoveWhere(id => id <= NPCID.NegativeIDCount || id == 0 || id >= NPCID.Count);
            }

            public HashSet<int> GetNPCs()
            {
                if (_npcsCache != null)
                {
                    return _npcsCache;
                }

                HashSet<int> npcs = new HashSet<int>(NPCs);

                foreach (var kvp in ModNPCs)
                {
                    if (ModLoader.HasMod(kvp.Key))
                    {
                        foreach (var modNpc in kvp.Value)
                        {
                            var fullName = kvp.Key + "/" + modNpc;
                            if (ContentSamples.NpcNetIdsByPersistentIds.TryGetValue(fullName, out var value))
                            {
                                npcs.Add(value);
                            }
                        }
                    }
                }

                _npcsCache = [.. npcs];

                return _npcsCache;
            }
            public void AddNPC(int netId)
            {
                if (netId < NPCID.Count)
                {
                    if (NPCs.Add(netId))
                    {
                        ResetCache();
                    }
                }
                else
                {
                    var npc = NPCLoader.GetNPC(netId);
                    string key = npc.Mod.Name;

                    if (!ModNPCs.TryGetValue(key, out HashSet<string> value))
                    {
                        value = new HashSet<string>();
                        ModNPCs[key] = value;
                    }

                    if (value.Add(npc.Name))
                    {
                        ResetCache();
                    }
                }
            }
            public bool RemoveNPC(int netId)
            {
                if (netId < NPCID.Count)
                {
                    bool success = NPCs.Remove(netId);
                    if (success)
                    {
                        ResetCache();
                    }
                    return success;
                }
                else
                {
                    var npc = NPCLoader.GetNPC(netId);

                    string key = npc.Mod.Name;

                    if (ModNPCs.TryGetValue(key, out HashSet<string> value))
                    {
                        bool success = value.Remove(npc.Name);

                        if (success)
                        {
                            ResetCache();
                        }

                        if (value.Count == 0)
                        {
                            ModNPCs.Remove(key);
                        }

                        return success;
                    }

                    return false;
                }
            }

            public NPCGroup(string name = "")
            {
                this.name = name;
            }

            public class StatSet : TagSerializable
            {
                public struct Data<T> : TagSerializable where T : IConvertible
                {
                    public T baseValue;
                    public float multValue = 1;
                    public T flatValue;
                    public T overrideValue;

                    [JsonIgnore]
                    public readonly bool UsesOverride => Convert.ToSingle(overrideValue) != 0;

                    public Data() { }

                    public Data(T baseValue, float multValue, T flatValue, T overrideValue)
                    {
                        this.baseValue = baseValue;
                        this.multValue = multValue;
                        this.flatValue = flatValue;
                        this.overrideValue = overrideValue;
                    }

                    public readonly float GetValue(float input, bool useOverride = true)
                    {
                        if (useOverride)
                        {
                            var overrideValueFloat = Convert.ToSingle(overrideValue);
                            if (overrideValueFloat != 0)
                            {
                                return overrideValueFloat;
                            }
                        }
                        return (input + Convert.ToSingle(baseValue)) * Convert.ToSingle(multValue) + Convert.ToSingle(flatValue);
                    }

                    public override readonly string ToString()
                    {
                        return string.Join(", ", baseValue, multValue, flatValue, overrideValue);
                    }

                    // TagSerializable
                    public TagCompound SerializeData() => new TagCompound
                    {
                        [nameof(baseValue)] = baseValue,
                        [nameof(multValue)] = multValue,
                        [nameof(flatValue)] = flatValue,
                        [nameof(overrideValue)] = overrideValue
                    };

                    public static readonly Func<TagCompound, Data<T>> DESERIALIZER = Load;
                    public static Data<T> Load(TagCompound tag) => new Data<T>
                    {
                        baseValue = tag.Get<T>(nameof(baseValue)),
                        multValue = tag.GetFloat(nameof(multValue)),
                        flatValue = tag.Get<T>(nameof(flatValue)),
                        overrideValue = tag.Get<T>(nameof(overrideValue))
                    };
                }

                public Data<int> life;
                public Data<int> damage;
                public Data<int> defense;
                public Data<float> scale;
                public Data<float> movement;
                public Data<float> aiSpeed;
                public Data<float> gravity;
                public Data<int> regen;
                public Data<float> knockback;

                public float regenLifeMaxPercent;
                //public float spawnRateMultiplier;

                public StatSet()
                {
                    life = new();
                    damage = new();
                    defense = new();
                    scale = new();
                    movement = new();
                    aiSpeed = new();
                    gravity = new();
                    regen = new();
                    knockback = new();
                }

                public StatSet Clone()
                {
                    return new StatSet()
                    {
                        life = life,
                        damage = damage,
                        defense = defense,
                        scale = scale,
                        movement = movement,
                        aiSpeed = aiSpeed,
                        gravity = gravity,
                        regen = regen,
                        knockback = knockback,
                        regenLifeMaxPercent = regenLifeMaxPercent,
                        //spawnRateMultiplier = spawnRateMultiplier
                    };
                }

                // TagSerializable
                public TagCompound SerializeData() => new TagCompound
                {
                    [nameof(life)] = life,
                    [nameof(damage)] = damage,
                    [nameof(defense)] = defense,
                    [nameof(scale)] = scale,
                    [nameof(movement)] = movement,
                    [nameof(aiSpeed)] = aiSpeed,
                    [nameof(gravity)] = gravity,
                    [nameof(regen)] = regen,
                    [nameof(knockback)] = knockback,
                    [nameof(regenLifeMaxPercent)] = regenLifeMaxPercent,
                    //[nameof(spawnRateMultiplier)] = spawnRateMultiplier
                };

                public static readonly Func<TagCompound, StatSet> DESERIALIZER = Load;
                public static StatSet Load(TagCompound tag) => new StatSet
                {
                    life = tag.Get<Data<int>>(nameof(life)),
                    damage = tag.Get<Data<int>>(nameof(damage)),
                    defense = tag.Get<Data<int>>(nameof(defense)),
                    scale = tag.Get<Data<float>>(nameof(scale)),
                    movement = tag.Get<Data<float>>(nameof(movement)),
                    aiSpeed = tag.Get<Data<float>>(nameof(aiSpeed)),
                    gravity = tag.Get<Data<float>>(nameof(gravity)),
                    regen = tag.Get<Data<int>>(nameof(regen)),
                    knockback = tag.Get<Data<float>>(nameof(knockback)),
                    regenLifeMaxPercent = tag.Get<float>(nameof(regenLifeMaxPercent)),
                    //spawnRateMultiplier = tag.Get<float>(nameof(spawnRateMultiplier))
                };
            }
        }
    }
}
