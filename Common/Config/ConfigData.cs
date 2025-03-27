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
using System.IO;
using Stubble.Core.Classes;

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
                [Obsolete]
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
                //public float regenLifeMaxPercent;
                [OnDeserialized]
                private void OnDeserialized(StreamingContext context)
                {
                    static bool IsDefault<T>(Data<T> data) where T : IConvertible
                    {
                        return Convert.ToSingle(data.baseValue) == 0 && Convert.ToSingle(data.multValue) == 0 & Convert.ToSingle(data.flatValue) == 0 && Convert.ToSingle(data.overrideValue) == 0;
                    }
                    
                    if (!IsDefault(life))
                    {
                        lifeBase = life.baseValue;
                        lifeMultiplier = life.multValue;
                        lifeFlat = life.flatValue;
                        lifeOverride = life.overrideValue;
                    }
                    if (!IsDefault(damage))
                    {
                        damageBase = damage.baseValue;
                        damageMultiplier = damage.multValue;
                        damageFlat = damage.flatValue;
                        damageOverride = damage.overrideValue;
                    }
                    if (!IsDefault(defense))
                    {
                        defenseBase = defense.baseValue;
                        defenseMultiplier = defense.multValue;
                        defenseFlat = defense.flatValue;
                        defenseOverride = defense.overrideValue;
                    }
                    if (!IsDefault(scale))
                    {
                        scaleMultiplier = scale.multValue;
                    }
                    if (!IsDefault(movement))
                    {
                        movementMultiplier = movement.multValue;
                    }
                    if (!IsDefault(aiSpeed))
                    {
                        aiSpeedMultiplier = aiSpeed.multValue;
                    }
                    if (!IsDefault(gravity))
                    {
                        gravityMultiplier = gravity.multValue;
                    }
                    if (!IsDefault(regen))
                    {
                        regenBase = regen.baseValue;
                        regenMultiplier = regen.multValue;
                        regenFlat = regen.flatValue;
                        regenOverride = regen.overrideValue;
                    }
                    if (!IsDefault(knockback))
                    {
                        knockbackBase = knockback.baseValue;
                        knockbackMultiplier = knockback.multValue;
                        knockbackFlat = knockback.flatValue;
                        knockbackOverride = knockback.overrideValue;
                    }
                }
                [OnSerializing]
                private void OnSerializing(StreamingContext context)
                {
                    life = default;
                    damage = default;
                    defense = default;
                    scale = default;
                    movement = default;
                    aiSpeed = default;
                    gravity = default;
                    regen = default;
                    knockback = default;
                }

                // These have been remade for simplicity's sake if modifications are needed to a specific stat in the future. Makes this class a lot more verbose though.
                #region Life
                public int lifeBase;
                public float lifeMultiplier = 1f;
                public int lifeFlat;
                public int lifeOverride;
                public int GetLifeValue(int life) => lifeOverride != 0 ? lifeOverride : (int)(((life + lifeBase) * lifeMultiplier) + lifeFlat);
                #endregion

                #region Damage
                public int damageBase;
                public float damageMultiplier = 1f;
                public int damageFlat;
                public int damageOverride;
                public int GetDamageValue(int damage) => damageOverride != 0 ? damageOverride : (int)(((damage + damageBase) * damageMultiplier) + damageFlat);
                #endregion

                #region Defense
                public int defenseBase;
                public float defenseMultiplier = 1f;
                public int defenseFlat;
                public int defenseOverride;
                public int GetDefenseValue(int defense) => defenseOverride != 0 ? defenseOverride : (int)(((defense + defenseBase) * defenseMultiplier) + defenseFlat);
                #endregion

                #region Scale
                public float scaleMultiplier = 1f;
                public float GetScaleValue() => scaleMultiplier;
                #endregion

                #region Movement
                public float movementMultiplier = 1f;
                public float GetMovementValue() => movementMultiplier;
                #endregion

                #region AISpeed
                public float aiSpeedMultiplier = 1f;
                public float GetAISpeedValue() => aiSpeedMultiplier;
                #endregion

                #region Gravity
                public float gravityMultiplier = 1f;
                public float GetGravityValue() => gravityMultiplier;
                #endregion

                #region Regen
                public int regenBase;
                public float regenMultiplier = 1f;
                public int regenFlat;
                public int regenOverride;
                public float regenLifeMaxPercent;
                public int GetRegenValue(int regen, int lifeMax) => regenOverride != 0 ? regenOverride : (int)(((regen + regenBase) * regenMultiplier) + regenFlat + (lifeMax * regenLifeMaxPercent));
                #endregion

                #region Knockback
                public float knockbackBase;
                public float knockbackMultiplier = 1f;
                public float knockbackFlat;
                public float knockbackOverride;
                public float GetKnockbackValue(float knockback) => knockbackOverride != 0 ? knockbackOverride : ((knockback + knockbackBase) * knockbackMultiplier) + knockbackFlat;
                #endregion

                public override bool Equals(object obj)
                {
                    if (obj is StatSet other)
                    {
                        return lifeBase == other.lifeBase &&
                               lifeMultiplier == other.lifeMultiplier &&
                               lifeFlat == other.lifeFlat &&
                               lifeOverride == other.lifeOverride &&

                               damageBase == other.damageBase &&
                               damageMultiplier == other.damageMultiplier &&
                               damageFlat == other.damageFlat &&
                               damageOverride == other.damageOverride &&

                               defenseBase == other.defenseBase &&
                               defenseMultiplier == other.defenseMultiplier &&
                               defenseFlat == other.defenseFlat &&
                               defenseOverride == other.defenseOverride &&

                               scaleMultiplier == other.scaleMultiplier &&

                               movementMultiplier == other.movementMultiplier &&

                               aiSpeedMultiplier == other.aiSpeedMultiplier &&

                               gravityMultiplier == other.gravityMultiplier &&

                               regenBase == other.regenBase &&
                               regenMultiplier == other.regenMultiplier &&
                               regenFlat == other.regenFlat &&
                               regenOverride == other.regenOverride &&
                               regenLifeMaxPercent == other.regenLifeMaxPercent &&

                               knockbackBase == other.knockbackBase &&
                               knockbackMultiplier == other.knockbackMultiplier &&
                               knockbackFlat == other.knockbackFlat &&
                               knockbackOverride == other.knockbackOverride;
                    }
                    return false;
                }


                public void Write(BinaryWriter writer)
                {
                    writer.Write(lifeBase);
                    writer.Write(lifeMultiplier);
                    writer.Write(lifeFlat);
                    writer.Write(lifeOverride);

                    writer.Write(damageBase);
                    writer.Write(damageMultiplier);
                    writer.Write(damageFlat);
                    writer.Write(damageOverride);

                    writer.Write(defenseBase);
                    writer.Write(defenseMultiplier);
                    writer.Write(defenseFlat);
                    writer.Write(defenseOverride);

                    writer.Write(scaleMultiplier);

                    writer.Write(movementMultiplier);

                    writer.Write(aiSpeedMultiplier);

                    writer.Write(gravityMultiplier);

                    writer.Write(regenBase);
                    writer.Write(regenMultiplier);
                    writer.Write(regenFlat);
                    writer.Write(regenOverride);
                    writer.Write(regenLifeMaxPercent);

                    writer.Write(knockbackBase);
                    writer.Write(knockbackMultiplier);
                    writer.Write(knockbackFlat);
                    writer.Write(knockbackOverride);
                }
                public static StatSet Read(BinaryReader reader)
                {
                    return new StatSet()
                    {
                        lifeBase = reader.ReadInt32(),
                        lifeMultiplier = reader.ReadSingle(),
                        lifeFlat = reader.ReadInt32(),
                        lifeOverride = reader.ReadInt32(),

                        damageBase = reader.ReadInt32(),
                        damageMultiplier = reader.ReadSingle(),
                        damageFlat = reader.ReadInt32(),
                        damageOverride = reader.ReadInt32(),

                        defenseBase = reader.ReadInt32(),
                        defenseMultiplier = reader.ReadSingle(),
                        defenseFlat = reader.ReadInt32(),
                        defenseOverride = reader.ReadInt32(),

                        scaleMultiplier = reader.ReadSingle(),

                        movementMultiplier = reader.ReadSingle(),

                        aiSpeedMultiplier = reader.ReadSingle(),

                        gravityMultiplier = reader.ReadSingle(),

                        regenBase = reader.ReadInt32(),
                        regenMultiplier = reader.ReadSingle(),
                        regenFlat = reader.ReadInt32(),
                        regenOverride = reader.ReadInt32(),
                        regenLifeMaxPercent = reader.ReadSingle(),

                        knockbackBase = reader.ReadSingle(),
                        knockbackMultiplier = reader.ReadSingle(),
                        knockbackFlat = reader.ReadSingle(),
                        knockbackOverride = reader.ReadSingle()
                    };
                }

                public override int GetHashCode()
                {
                    HashCode hash = new HashCode();
                    hash.Add(lifeBase);
                    hash.Add(lifeMultiplier);
                    hash.Add(lifeFlat);
                    hash.Add(lifeOverride);

                    hash.Add(damageBase);
                    hash.Add(damageMultiplier);
                    hash.Add(damageFlat);
                    hash.Add(damageOverride);

                    hash.Add(defenseBase);
                    hash.Add(defenseMultiplier);
                    hash.Add(defenseFlat);
                    hash.Add(defenseOverride);

                    hash.Add(scaleMultiplier);
                    hash.Add(movementMultiplier);
                    hash.Add(aiSpeedMultiplier);
                    hash.Add(gravityMultiplier);

                    hash.Add(regenBase);
                    hash.Add(regenMultiplier);
                    hash.Add(regenFlat);
                    hash.Add(regenOverride);
                    hash.Add(regenLifeMaxPercent);

                    hash.Add(knockbackBase);
                    hash.Add(knockbackMultiplier);
                    hash.Add(knockbackFlat);
                    hash.Add(knockbackOverride);

                    return hash.ToHashCode();
                }

                public StatSet Clone()
                {
                    return new StatSet()
                    {
                        lifeBase = lifeBase,
                        lifeMultiplier = lifeMultiplier,
                        lifeFlat = lifeFlat,
                        lifeOverride = lifeOverride,

                        damageBase = damageBase,
                        damageMultiplier = damageMultiplier,
                        damageFlat = damageFlat,
                        damageOverride = damageOverride,

                        defenseBase = defenseBase,
                        defenseMultiplier = defenseMultiplier,
                        defenseFlat = defenseFlat,
                        defenseOverride = defenseOverride,

                        scaleMultiplier = scaleMultiplier,

                        movementMultiplier = movementMultiplier,

                        aiSpeedMultiplier = aiSpeedMultiplier,

                        gravityMultiplier = gravityMultiplier,

                        regenBase = regenBase,
                        regenMultiplier = regenMultiplier,
                        regenFlat = regenFlat,
                        regenLifeMaxPercent = regenLifeMaxPercent,
                        regenOverride = regenOverride,

                        knockbackBase = knockbackBase,
                        knockbackMultiplier = knockbackMultiplier,
                        knockbackFlat = knockbackFlat,
                        knockbackOverride = knockbackOverride
                    };
                }

                // TagSerializable
                public TagCompound SerializeData()
                {
                    var tag = new TagCompound()
                    {
                        [nameof(lifeBase)] = lifeBase,
                        [nameof(lifeMultiplier)] = lifeMultiplier,
                        [nameof(lifeFlat)] = lifeFlat,
                        [nameof(lifeOverride)] = lifeOverride,

                        [nameof(damageBase)] = damageBase,
                        [nameof(damageMultiplier)] = damageMultiplier,
                        [nameof(damageFlat)] = damageFlat,
                        [nameof(damageOverride)] = damageOverride,

                        [nameof(defenseBase)] = defenseBase,
                        [nameof(defenseMultiplier)] = defenseMultiplier,
                        [nameof(defenseFlat)] = defenseFlat,
                        [nameof(defenseOverride)] = defenseOverride,

                        [nameof(scaleMultiplier)] = scaleMultiplier,

                        [nameof(movementMultiplier)] = movementMultiplier,

                        [nameof(aiSpeedMultiplier)] = aiSpeedMultiplier,

                        [nameof(gravityMultiplier)] = gravityMultiplier,

                        [nameof(regenBase)] = regenBase,
                        [nameof(regenMultiplier)] = regenMultiplier,
                        [nameof(regenFlat)] = regenFlat,
                        [nameof(regenLifeMaxPercent)] = regenLifeMaxPercent,
                        [nameof(regenOverride)] = regenOverride,

                        [nameof(knockbackBase)] = knockbackBase,
                        [nameof(knockbackMultiplier)] = knockbackMultiplier,
                        [nameof(knockbackFlat)] = knockbackFlat,
                        [nameof(knockbackOverride)] = knockbackOverride
                    };

                    return tag;
                }

                public static readonly Func<TagCompound, StatSet> DESERIALIZER = Load;
                public static StatSet Load(TagCompound tag)
                {
                    var statSet = new StatSet()
                    {
                        lifeBase = tag.Get<int>(nameof(lifeBase)),
                        lifeMultiplier = tag.Get<float>(nameof(lifeMultiplier)),
                        lifeFlat = tag.Get<int>(nameof(lifeFlat)),
                        lifeOverride = tag.Get<int>(nameof(lifeOverride)),

                        damageBase = tag.Get<int>(nameof(damageBase)),
                        damageMultiplier = tag.Get<float>(nameof(damageMultiplier)),
                        damageFlat = tag.Get<int>(nameof(damageFlat)),
                        damageOverride = tag.Get<int>(nameof(damageOverride)),

                        defenseBase = tag.Get<int>(nameof(defenseBase)),
                        defenseMultiplier = tag.Get<float>(nameof(defenseMultiplier)),
                        defenseFlat = tag.Get<int>(nameof(defenseFlat)),
                        defenseOverride = tag.Get<int>(nameof(defenseOverride)),

                        scaleMultiplier = tag.Get<float>(nameof(scaleMultiplier)),

                        movementMultiplier = tag.Get<float>(nameof(movementMultiplier)),

                        aiSpeedMultiplier = tag.Get<float>(nameof(aiSpeedMultiplier)),

                        gravityMultiplier = tag.Get<float>(nameof(gravityMultiplier)),

                        regenBase = tag.Get<int>(nameof(regenBase)),
                        regenMultiplier = tag.Get<float>(nameof(regenMultiplier)),
                        regenFlat = tag.Get<int>(nameof(regenFlat)),
                        regenLifeMaxPercent = tag.Get<float>(nameof(regenLifeMaxPercent)),
                        regenOverride = tag.Get<int>(nameof(regenOverride)),

                        knockbackBase = tag.Get<float>(nameof(knockbackBase)),
                        knockbackMultiplier = tag.Get<float>(nameof(knockbackMultiplier)),
                        knockbackFlat = tag.Get<float>(nameof(knockbackFlat)),
                        knockbackOverride = tag.Get<float>(nameof(knockbackOverride))
                    };

                    if (tag.TryGet<Data<int>>("life", out var life))
                    {
                        statSet.lifeBase = life.baseValue;
                        statSet.lifeMultiplier = life.multValue;
                        statSet.lifeFlat = life.flatValue;
                        statSet.lifeOverride = life.overrideValue;
                        tag.Remove("life");
                    }
                    if (tag.TryGet<Data<int>>("damage", out var damage))
                    {
                        statSet.damageBase = damage.baseValue;
                        statSet.damageMultiplier = damage.multValue;
                        statSet.damageFlat = damage.flatValue;
                        statSet.damageOverride = damage.overrideValue;
                    }
                    if (tag.TryGet<Data<int>>("defense", out var defense))
                    {
                        statSet.defenseBase = defense.baseValue;
                        statSet.defenseMultiplier = defense.multValue;
                        statSet.defenseFlat = defense.flatValue;
                        statSet.defenseOverride = defense.overrideValue;
                    }
                    if (tag.TryGet<Data<float>>("scale", out var scale))
                    {
                        statSet.scaleMultiplier = scale.multValue;
                    }
                    if (tag.TryGet<Data<float>>("movement", out var movement))
                    {
                        statSet.movementMultiplier = movement.multValue;
                    }
                    if (tag.TryGet<Data<float>>("aiSpeed", out var aiSpeed))
                    {
                        statSet.aiSpeedMultiplier = aiSpeed.multValue;
                    }
                    if (tag.TryGet<Data<float>>("gravity", out var gravity))
                    {
                        statSet.gravityMultiplier = gravity.multValue;
                    }
                    if (tag.TryGet<Data<int>>("regen", out var regen))
                    {
                        statSet.regenBase = regen.baseValue;
                        statSet.regenMultiplier = regen.multValue;
                        statSet.regenFlat = regen.flatValue;
                        statSet.regenOverride = regen.overrideValue;
                        // regenLifeMaxPercent has had no modifications to it so it's fine
                    }
                    if (tag.TryGet<Data<float>>("knockback", out var knockback))
                    {
                        statSet.knockbackBase = knockback.baseValue;
                        statSet.knockbackMultiplier = knockback.multValue;
                        statSet.knockbackFlat = knockback.flatValue;
                        statSet.knockbackOverride = knockback.overrideValue;
                    }

                    return statSet;
                }
            }
        }
    }
}
