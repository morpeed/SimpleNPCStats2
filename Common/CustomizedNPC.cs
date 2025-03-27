using MonoMod.Cil;
using SimpleNPCStats2.Common.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tModPorter;
using Terraria.GameInput;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Utilities;
using Terraria.UI.Chat;
using Terraria.ModLoader.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using Terraria.IO;
using Newtonsoft.Json.Linq;
using MonoMod.Utils;
using Terraria.Net;

namespace SimpleNPCStats2.Common
{
    /// <summary>
    /// Check if <see cref="Enabled"/> is true before doing anything.
    /// </summary>
    public class CustomizedNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool Enabled => Stats != null;
        public ConfigData.NPCGroup.StatSet Stats { get; private set; }
        public float MovementSpeed { get; private set; }
        public float Gravity { get; private set; }
        public float Scale { get; private set; }
        public float AISpeed { get; private set; }
        public float AISpeedCounter { get; private set; }
        public bool OverrideModifyAI { get; private set; }
        public int TypeNetID { get; private set; }

        public static readonly Dictionary<int, (float? minScale, float? maxScale)> ScaleClampNPCIDs = new()
        {
            { NPCID.Golem, (null, 1.5f) },
            { NPCID.GolemFistLeft, (null, 1.5f) },
            { NPCID.GolemFistRight, (null, 1.5f) },
            { NPCID.GolemHead, (null, 1.5f) },
            { NPCID.GolemHeadFree, (null, 1.5f) } // Not needed technically, but for consistency
        };

        /// Disables movement speed modifier (value will still be saved, but not in use, so will be transferred to projectiles)
        private bool _useMovementSpeed;
        public static readonly HashSet<int> NoMovementSpeedNPCIDs =
        [
            NPCID.CultistBoss,
            NPCID.CultistBossClone,
            NPCID.GolemFistLeft,
            NPCID.GolemFistRight,
            NPCID.GolemHead
        ];

        // Static because GlobalNPC is created sometime during SetDefaults (I think)
        private static bool _fromNetID;
        public static void On_NPC_SetDefaultsFromNetId(On_NPC.orig_SetDefaultsFromNetId orig, NPC self, int id, NPCSpawnParams spawnparams)
        {
            _fromNetID = true;
            orig(self, id, spawnparams);
            _fromNetID = false;
            if (!Main.gameMenu)
            {
                if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
                {
                    if (!result._setupFromLoadData)
                    {
                        result.TypeNetID = id;
                        result.Setup(self);
                    }
                }
            }
        }
        public static void On_NPC_SetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams)
        {
            orig(self, Type, spawnparams);
            // Method returns early if Type < 0
            if (!Main.gameMenu && Type > 0 && !_fromNetID)
            {
                if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
                {
                    if (!result._setupFromLoadData)
                    {
                        result.TypeNetID = self.type;
                        result.Setup(self);
                    }
                }
            }
        }

        public bool Setup(NPC npc, ConfigData.NPCGroup.StatSet stats = null)
        {
            if (stats == null)
            {
                if (ConfigSystem.StaticNPCData.TryGetValue(TypeNetID, out var dataValue))
                {
                    Stats = dataValue;
                }
                else
                {
                    Stats = null;
                    return false;
                }
            }
            else
            {
                Stats = stats;
            }

            oldStatInfo = StatInfo.Create(npc);

            Gravity = Stats.GetGravityValue();
            AISpeed = Stats.GetAISpeedValue();
            AISpeedCounter = 1;
            MovementSpeed = Stats.GetMovementValue();
            _useMovementSpeed = !NoMovementSpeedNPCIDs.Contains(TypeNetID);

            npc.lifeMax = Math.Max(0, Stats.GetLifeValue(npc.lifeMax));
            npc.life = npc.lifeMax;

            npc.defDamage = Math.Max(0, Stats.GetDamageValue(npc.defDamage));
            npc.damage = npc.defDamage;

            npc.defDefense = Stats.GetDefenseValue(npc.defDefense);
            npc.defense = npc.defDefense;

            var oldScale = npc.scale;
            npc.scale *= Stats.GetScaleValue();
            if (ScaleClampNPCIDs.TryGetValue(TypeNetID, out var scaleClampValue))
            {
                if (scaleClampValue.minScale != null && npc.scale < scaleClampValue.minScale)
                {
                    npc.scale = scaleClampValue.minScale.Value;
                }
                else if (scaleClampValue.maxScale != null && npc.scale > scaleClampValue.maxScale)
                {
                    npc.scale = scaleClampValue.maxScale.Value;
                }
            }
            Scale = npc.scale / oldScale;
            npc.width = Math.Max(1, (int)(npc.width * Scale));
            npc.height = Math.Max(1, (int)(npc.height * Scale));

            npc.knockBackResist = Math.Max(0, this.Stats.GetKnockbackValue(npc.knockBackResist));

            OverrideModifyAI = ConfigSystemAdvanced.Instance.overrideModifyAI;

            newStatInfo = StatInfo.Create(npc);

            return true;
        }
        public void Write(NPC npc, BinaryWriter writer)
        {
            Stats.Write(writer);
            writer.Write(oldStatInfo.lifeMax);
            writer.Write(oldStatInfo.defDamage);
            writer.Write(newStatInfo.lifeMax);
            writer.Write(newStatInfo.defDamage);
            writer.Write(Gravity);
            writer.Write(AISpeed);
            writer.Write(MovementSpeed);
            writer.Write(Scale);
            writer.Write(OverrideModifyAI);
            writer.Write(npc.lifeMax);
            writer.Write(npc.life);
            writer.Write(npc.defDamage);
            writer.Write(npc.damage);
            writer.Write(npc.defDefense);
            writer.Write(npc.defense);
            writer.Write(npc.scale);
            writer.Write(npc.width);
            writer.Write(npc.height);
            writer.Write(npc.knockBackResist);
        }
        public void Read(NPC npc, BinaryReader reader)
        {
            Stats = ConfigData.NPCGroup.StatSet.Read(reader);
            oldStatInfo.lifeMax = reader.ReadInt32();
            oldStatInfo.defDamage = reader.ReadInt32();
            newStatInfo.lifeMax = reader.ReadInt32();
            newStatInfo.defDamage = reader.ReadInt32();
            Gravity = reader.ReadSingle();
            AISpeed = reader.ReadSingle();
            MovementSpeed = reader.ReadSingle();
            Scale = reader.ReadSingle();
            OverrideModifyAI = reader.ReadBoolean();
            npc.lifeMax = reader.ReadInt32();
            npc.life = reader.ReadInt32();
            npc.defDamage = reader.ReadInt32();
            npc.damage = reader.ReadInt32();
            npc.defDefense = reader.ReadInt32();
            npc.defense = reader.ReadInt32();
            npc.scale = reader.ReadSingle();
            npc.width = reader.ReadInt32();
            npc.height = reader.ReadInt32();
            npc.knockBackResist = reader.ReadSingle();

            _useMovementSpeed = !NoMovementSpeedNPCIDs.Contains(TypeNetID);
        }

        // AI Speed && Movement Speed
        public static void IL_NPC_UpdateNPC_Inner(ILContext context)
        {
            try
            {
                ILCursor cursor = new ILCursor(context);

                // Method gets inlined so done as IL edit instead of detour
                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall("Terraria.NPC", "AI")
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitDelegate((NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled)
                        {
                            result.AISpeedCounter += result.AISpeed;
                            while (result.AISpeedCounter >= 1)
                            {
                                result.AISpeedCounter--;
                                npc.AI();
                            }
                            return true;
                        }
                        return false;
                    });
                    var skipLabel = cursor.DefineLabel();
                    cursor.EmitBrtrue(skipLabel);

                    cursor.Index += 2;

                    cursor.MarkLabel(skipLabel);
                }

                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall("Terraria.NPC", "UpdateCollision")
                    ))
                {
                    cursor.EmitLdarga(0);
                    cursor.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result._useMovementSpeed)
                        {
                            npc.velocity *= result.MovementSpeed;
                        }
                    });

                    // After
                    cursor.Index += 2;

                    cursor.EmitLdarga(0);
                    cursor.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result._useMovementSpeed && result.MovementSpeed != 0)
                        {
                            npc.velocity /= result.MovementSpeed;
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld("Terraria.Entity", "position"),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld("Terraria.Entity", "velocity"),
                    // HERE
                    i => i.MatchCall("Microsoft.Xna.Framework.Vector2", "op_Addition"),
                    i => i.MatchStfld("Terraria.Entity", "position")
                    ))
                {
                    cursor.Index -= 2;

                    cursor.EmitLdarg0();
                    cursor.EmitDelegate((NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result._useMovementSpeed)
                        {
                            return result.MovementSpeed;
                        }
                        return 1;
                    });
                    cursor.EmitCall(typeof(Vector2).GetMethod("op_Multiply", BindingFlags.Static | BindingFlags.Public, null, [typeof(Vector2), typeof(float)], null));
                }

                cursor.UpdateInstructionOffsets();
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        // Life Regen
        private static bool _IL_LifeRegenModified;
        public static void IL_NPC_UpdateNPC_BuffApplyDOTs(ILContext context)
        {
            try
            {
                ILCursor cursor = new ILCursor(context);

                /*
                    Matching:
                    // NPCLoader.UpdateLifeRegen(this, ref num);
	                IL_068e: ldarg.0
	                IL_068f: ldloca.s 0
	                IL_0691: call void Terraria.ModLoader.NPCLoader::UpdateLifeRegen(class Terraria.NPC, int32&)

                    Modifies life regen after all other mods
                 */
                if (cursor.TryGotoNext(MoveType.After,
                        i => i.MatchLdarg0(),
                        i => i.MatchLdloca(0),
                        i => i.MatchCall("Terraria.ModLoader.NPCLoader", "UpdateLifeRegen")
                        ))
                {
                    cursor.EmitLdarga(0); // ref this
                    cursor.EmitLdloca(0); // num value / regen visual number                    
                    cursor.EmitDelegate((ref NPC npc, ref int regenNumber) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled && result.AISpeed > 0) // Won't be able to update regen if there's no AI
                            {
                                int newRegen = result.Stats.GetRegenValue(npc.lifeRegen, npc.lifeMax);

                                if (newRegen != npc.lifeRegen)
                                {
                                    npc.lifeRegen = newRegen * 2;
                                    regenNumber = Math.Abs(newRegen / 2);
                                    if (regenNumber == 0)
                                    {
                                        regenNumber = 1;
                                    }
                                    _IL_LifeRegenModified = true;
                                    return;
                                }
                            }
                        }
                        _IL_LifeRegenModified = false;
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,

                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("lifeRegenCount"),
                    i => i.MatchLdcI4(120),
                    i => i.MatchSub(),
                    i => i.MatchStfld<NPC>("lifeRegenCount")
                    ))
                {
                    cursor.Index -= 2;
                    cursor.EmitLdarg0();
                    cursor.EmitLdloc0();
                    cursor.EmitDelegate((NPC npc, int regenNumber) =>
                    {
                        if (_IL_LifeRegenModified)
                        {
                            return regenNumber;
                        }
                        return 1;
                    });
                    cursor.EmitMul();
                }
                else
                {
                    Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("life"),
                    i => i.MatchLdcI4(1),
                    i => i.MatchAdd(),
                    i => i.MatchStfld<NPC>("life")
                    ))
                {
                    cursor.Index -= 2;
                    cursor.EmitLdarg0();
                    cursor.EmitLdloc0();
                    cursor.EmitDelegate((NPC npc, int regenNumber) =>
                    {
                        if (_IL_LifeRegenModified)
                        {
                            int visualNumber = Math.Min(npc.lifeMax - npc.life, regenNumber);
                            npc.HealEffect(visualNumber);
                            return regenNumber;
                        }
                        return 1;
                    });
                    cursor.EmitMul();
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("lifeRegenCount"),
                    i => i.MatchLdcI4(120),
                    // HERE
                    i => i.MatchBge(out _)
                    ))
                {
                    cursor.Index--;
                    cursor.EmitLdarg0();
                    cursor.EmitLdloc0();
                    cursor.EmitDelegate((NPC npc, int regenNumber) =>
                    {
                        if (_IL_LifeRegenModified)
                        {
                            return regenNumber;
                        }
                        return 1;
                    });
                    cursor.EmitMul();
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        private bool _setupFromLoadData;
        public override void LoadData(NPC npc, TagCompound tag)
        {
            if (tag.TryGet<float>("xOff", out var value2))
            {
                npc.position += new Vector2(value2, (float)tag["yOff"]);
            }

            _setupFromLoadData = true;

            if (tag.TryGet<ConfigData.NPCGroup.StatSet>(nameof(Stats), out var value))
            {
                Setup(npc, value);
            }
            else
            {
                Setup(npc);
            }
        }
        public override void SaveData(NPC npc, TagCompound tag)
        {
            if (Stats != null)
            {
                tag[nameof(Stats)] = Stats;

                tag.Remove("xOff");
                tag.Remove("yOff");
            }
            else if (WorldGen.generatingWorld)
            {
                if (ConfigSystem.StaticNPCData.TryGetValue(npc.type, out var value))
                {
                    tag[nameof(Stats)] = value;

                    float scale = value.GetScaleValue();
                    if (scale != 1)
                    {
                        float newWidth = (int)(npc.width * scale);
                        float newHeight = (int)(npc.height * scale);

                        tag["xOff"] = -(newWidth - npc.width) / 2f;
                        tag["yOff"] = -(newHeight - npc.height) / 2f;
                    }
                }
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(AISpeedCounter);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            AISpeedCounter = binaryReader.ReadSingle();
        }

        public override bool PreAI(NPC npc)
        {
            if (Enabled)
            {
                npc.MaxFallSpeedMultiplier *= Gravity;
                npc.GravityMultiplier *= Gravity;
            }
            return true;
        }

        public override void ModifyHoverBoundingBox(NPC npc, ref Rectangle boundingBox)
        {
            if (Enabled)
            {
                var oldWidth = boundingBox.Width;
                boundingBox.Width = (int)(boundingBox.Width * Scale);
                boundingBox.X -= (boundingBox.Width - oldWidth) / 2;

                var oldHeight = boundingBox.Height;
                boundingBox.Height = (int)(boundingBox.Height * Scale);
                boundingBox.Y -= (boundingBox.Height - oldHeight);
            }
        }

        public static float GetScaleMultiplier(NPC npc)
        {
            if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
            {
                if (result.Enabled)
                {
                    return result.Scale;
                }
            }
            return 1f;
        }

        public StatInfo oldStatInfo;
        public StatInfo newStatInfo;
        public struct StatInfo
        {
            public int lifeMax;

            public int defDamage;

            public static StatInfo Create(NPC npc)
            {
                return new StatInfo()
                {
                    lifeMax = npc.lifeMax,
                    defDamage = npc.defDamage
                };
            }

            public override string ToString()
            {
                return string.Join(',', lifeMax, defDamage);
            }
        }
    }
}
