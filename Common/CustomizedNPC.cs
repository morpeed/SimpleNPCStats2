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
        private bool _doMovementSpeed;
        public float Gravity { get; private set; }
        public float Scale { get; private set; }
        public float AISpeed { get; private set; }
        public float AISpeedCounter { get; private set; }
        private bool _AISpeedImmediateUpdate;

        public int TypeNetId { get; private set; }

        public static readonly Dictionary<int, (float? minScale, float? maxScale)> ScaleClampNPCIds = new()
        {
            { NPCID.Golem, (null, 1.5f) },
            { NPCID.GolemFistLeft, (null, 1.5f) },
            { NPCID.GolemFistRight, (null, 1.5f) },
            { NPCID.GolemHead, (null, 1.5f) },
            { NPCID.GolemHeadFree, (null, 1.5f) } // Not needed technically, but for consistency
        };

        // Disables movement speed modifier (value will still be saved, but not in use, so will be transferred to projectiles)
        public static readonly HashSet<int> NoMovementSpeedNPCs =
        [
            NPCID.CultistBoss,
            NPCID.CultistBossClone
        ];

        #region NPC Setup
        /*
        // Done as an IL edit to take place after all other mods' OnSpawn overrides
        private static void IL_NPC_NewNPC(ILContext context)
        {
            try
            {
                ILCursor cursor = new ILCursor(context);

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCall("Terraria.ModLoader.NPCLoader", "OnSpawn")
                    ))
                {
                    cursor.EmitLdloc0(); // Local variable for the NPCs whoAmI (Static method, no attached NPC instance)
                    cursor.EmitLdarg3(); // Argument for the type/netid of the NPC
                    cursor.EmitDelegate((int whoAmI, int typeNetId) =>
                    {
                        
                        Main.NewText(typeNetId);
                        var npc = Main.npc[whoAmI];
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            result.TypeNetId = typeNetId;
                            result.Setup(npc);
                        }
                        
                    });
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
        */

        // Static because GlobalNPC is created sometime during SetDefaults (I think)
        private static bool _fromNetId;
        public static void On_NPC_SetDefaultsFromNetId(On_NPC.orig_SetDefaultsFromNetId orig, NPC self, int id, NPCSpawnParams spawnparams)
        {
            _fromNetId = true;
            orig(self, id, spawnparams);
            _fromNetId = false;

            // Ensures it's done after content samples are populated etc (probably)
            if (!Main.gameMenu)
            {
                if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
                {
                    result.TypeNetId = id;
                    result.Setup(self);
                }
            }
        }

        public static void On_NPC_SetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams)
        {
            orig(self, Type, spawnparams);

            // Method returns early if Type < 0
            if (!Main.gameMenu && Type > 0)
            {
                if (!_fromNetId)
                {
                    if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
                    {
                        result.TypeNetId = self.type;
                        result.Setup(self);
                    }
                }
            }
        }

        private bool Setup(NPC npc)
        {
            /*
            void DebugNPC()
            {
                Main.NewText(string.Join(", ",
                    $"Scale {npc.scale}",
                    $"Width {npc.width}",
                    $"Height {npc.height}",
                    $"LifeMax {npc.lifeMax}",
                    $"Damage {npc.damage}",
                    $"Defense {npc.defense}"
                    ));
            }
            */

            if (ConfigSystem.StaticNPCData.TryGetValue(TypeNetId, out var dataValue))
            {
                //DebugNPC();
                Stats = dataValue;

                OldStatInfo = StatInfo.Create(npc);

                Gravity = Stats.gravity.GetValue(1);
                AISpeed = Stats.aiSpeed.GetValue(1);
                AISpeedCounter = -1;
                _AISpeedImmediateUpdate = true;
                MovementSpeed = Stats.movement.GetValue(1);
                _doMovementSpeed = !NoMovementSpeedNPCs.Contains(TypeNetId);

                npc.lifeMax = Math.Max(1, (int)Stats.life.GetValue(npc.lifeMax));
                npc.life = npc.lifeMax;

                npc.defDefense = (int)Stats.defense.GetValue(npc.defDefense);
                npc.defense = npc.defDefense;

                npc.defDamage = (int)Math.Max(0, Stats.damage.GetValue(npc.defDamage));
                npc.damage = npc.defDamage;

                var oldScale = npc.scale;
                npc.scale *= Stats.scale.GetValue(1);
                if (ScaleClampNPCIds.TryGetValue(TypeNetId, out var scaleClampValue))
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

                npc.knockBackResist = Stats.knockback.GetValue(npc.knockBackResist);

                NewStatInfo = StatInfo.Create(npc);

                //DebugNPC();
                return true;
            }
            else
            {
                Stats = null;
                return false;
            }
        }
        #endregion

        #region Life regen
        private static bool _lifeRegenModified;
        public static void IL_NPC_LifeRegen(ILContext context)
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
                                var newRegen = (int)result.Stats.regen.GetValue(npc.lifeRegen);
                                if (newRegen != npc.lifeRegen)
                                {
                                    npc.lifeRegen = newRegen * 2;
                                    regenNumber = Math.Abs(newRegen / 2);
                                    if (regenNumber == 0)
                                    {
                                        regenNumber = 1;
                                    }
                                    _lifeRegenModified = true;
                                    npc.lifeRegen = (int)(npc.lifeRegen / result.AISpeed);
                                    return;
                                }
                                npc.lifeRegen = (int)(npc.lifeRegen / result.AISpeed);
                            }
                        }
                        _lifeRegenModified = false;
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
                        if (_lifeRegenModified)
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
                        if (_lifeRegenModified)
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
                        if (_lifeRegenModified)
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
        #endregion

        #region AI Speed
        public static void On_NPC_UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            if (self.active && self.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled)
            {
                var immuneCopy = self.immune.ToArray();

                if (result._AISpeedImmediateUpdate)
                {
                    orig(self, i);
                    result._AISpeedImmediateUpdate = false;
                }

                if (result.AISpeed <= 0)
                {
                    return;
                }

                result.AISpeedCounter += result.AISpeed;
                while (result.AISpeedCounter >= 1)
                {
                    orig(self, i);
                    result.AISpeedCounter--;
                }

                for (int j = 0; j < immuneCopy.Length; j++)
                {
                    if (immuneCopy[j] > 0)
                    {
                        immuneCopy[j]--;
                    }
                }

                self.immune = immuneCopy;
            }
            else
            {
                orig(self, i);
            }
        }
        #endregion

        #region Movement Speed
        public static void IL_NPC_Movement(ILContext il)
        {
            try
            {
                ILCursor c;

                /*
                 * Movement Speed (for noTileCollide NPCs)
                 * Match before the first UpdateCollision call
                 * Multiply velocity by speed
                 */
                c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall<NPC>("UpdateCollision")    // IL_077e: call instance void Terraria.NPC::UpdateCollision()
                    ))
                {
                    c.EmitLdarga(0);                            // ref this
                    c.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled && result._doMovementSpeed)
                            {
                                npc.velocity *= result.MovementSpeed;
                            }
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("Failure to add pre-UpdateCollision delegate !!!!!");
                }

                /*
                 * Movement Speed (for noTileCollide NPCs)
                 * Match after the first UpdateCollision call
                 * Divide velocity by speed
                 */
                c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall<NPC>("UpdateCollision")    // IL_077e: call instance void Terraria.NPC::UpdateCollision()
                    ))
                {
                    c.EmitLdarga(0);                            // ref this
                    c.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled && result._doMovementSpeed && result.MovementSpeed != 0)
                            {
                                npc.velocity /= result.MovementSpeed;
                            }
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("Failure to add post-UpdateCollision delegate !!!!!");
                }

                /*
                 * Movement speed (for tile coliding NPCs)
                 * Match after position += velocity
                 * Adjust velocity to speed
                 */
                c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("position"),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("velocity"),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStfld<Entity>("position")
                    ))
                {
                    c.EmitLdarga(0);                             // this
                    c.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled && result._doMovementSpeed)
                            {
                                npc.position += npc.velocity * (result.MovementSpeed - 1);
                            }
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("Failure to add post base.position += base.velocity delegate !!!!!");
                }

                /*
                // Size
                c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall<NPC>("AI")
                    ))
                {
                    c.EmitLdarga(0);
                    c.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                // Key is removed upon NPC spawning, so this will fail on the first update (won't use size from dummy/old npc)
                                if (result.tempStats != null)
                                {
                                    result.tempStats.Value.UpdateNPC(npc);

                                    npc.position += result.tempStats.Value.positionShift;
                                }
                            }
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("Failure to add pre-AI delegate !!!!!");
                }

                c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall<NPC>("AI")
                    ))
                {
                    c.EmitLdarga(0);
                    c.EmitDelegate((ref NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                var tempStats = TempStats.Create(npc);

                                var oldWidth = npc.width;
                                var oldHeight = npc.height;
                                var lifePercent = npc.GetLifePercent();

                                if (result.UsesDynamicScaling)
                                {
                                    ScaleNPC(npc, result.GetScaleIncreaseClamped(npc));
                                }
                                npc.lifeMax = (int)result.Stats.life.GetValue(npc.lifeMax);
                                if (npc.lifeMax <= 0)
                                {
                                    npc.lifeMax = 1;
                                }
                                npc.defense = (int)result.Stats.defense.GetValue(npc.defense);
                                npc.damage = (int)result.Stats.damage.GetValue(npc.damage);

                                tempStats.positionShift = new Vector2((npc.width - oldWidth) / 2f, (npc.height - oldHeight) / 2f);
                                npc.position -= tempStats.positionShift;

                                result.tempStats = tempStats;
                            }
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("Failure to add post-AI delegate !!!!!");
                }
                */
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), il);
            }
        }
        #endregion

        /*
        public static void IL_NPC_NewNPC(ILContext context)
        {
            try
            {
                ILCursor cursor;

                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCall("Terraria.ModLoader.NPCLoader", "OnSpawn")
                    ))
                {
                    cursor.EmitLdloc0();
                    cursor.EmitDelegate((int npcWhoAmI) =>
                    {
                        var npc = Main.npc[npcWhoAmI];
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                result.NewStatInfo = StatInfo.Create(npc);
                                Main.NewText(result.OldStatInfo);
                                Main.NewText(result.NewStatInfo);
                                Main.NewText(result.NewStatInfo.damage / (float)result.OldStatInfo.damage);
                            }
                        }
                    });
                }
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
        */

        public override bool PreAI(NPC npc)
        {
            if (Enabled)
            {
                npc.MaxFallSpeedMultiplier *= Gravity;
                npc.GravityMultiplier *= Gravity;
                if (_doMovementSpeed)
                {
                    npc.MaxFallSpeedMultiplier /= MovementSpeed;
                    npc.GravityMultiplier /= MovementSpeed;
                }
            }
            return true;
        }

        public override void PostAI(NPC npc)
        {

        }

        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {

        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            
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
                return result.Scale;
            }
            return 1f;
        }

        public StatInfo OldStatInfo { get; private set; }
        public StatInfo NewStatInfo { get; private set; }
        public struct StatInfo
        {
            public int defDamage;
            public int life;

            public static StatInfo Create(NPC npc)
            {
                return new StatInfo()
                {
                    defDamage = npc.defDamage,
                    life = npc.lifeMax
                };
            }

            public override string ToString()
            {
                return string.Join(',', defDamage, life);
            }
        }


        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return;

            object[] draw = [
                OldStatInfo,
                NewStatInfo,
                npc.damage,
                NewStatInfo.defDamage / (float)OldStatInfo.defDamage
                ];

            for (int i = 0; i < draw.Length; i++)
            {
                string text = draw[i].ToString();
                float yOff = i * 20;

                Utils.DrawBorderString(spriteBatch, text, npc.Center + new Vector2(0, npc.height / 2 + 50 + yOff) - Main.screenPosition, Color.White, 1, 0.5f, 0.5f);
            }
        }
        /*
         
        public TempStats? tempStats;
        public struct TempStats
        {
            public float scale;
            public int width;
            public int height;
            public int lifeMax;
            public int defense;
            public Vector2 positionShift;

            public readonly void UpdateNPC(NPC npc)
            {
                npc.scale = this.scale;
                npc.width = this.width;
                npc.height = this.height;
                npc.lifeMax = this.lifeMax;
                npc.defense = this.defense;
            }

            public static TempStats Create(NPC npc)
            {
                var stats = new TempStats()
                {
                    scale = npc.scale,
                    width = npc.width,
                    height = npc.height,
                    lifeMax = npc.lifeMax,
                    defense = npc.defense,
                };
                return stats;
            }
        }

        private bool isSegmented;

        public static bool GetIsSegmented(NPC npc)
        {
            if (npc.type < NPCID.Count)
            {   
                return SegmentedNPCIds.Contains(npc.type);
            }
            else
            {
                return SegmentedModNPCs.Contains(npc.ModNPC.FullName);
            }
        }

        public static HashSet<int> SegmentedNPCIds =
        [
            NPCID.DevourerHead,
            NPCID.DevourerBody,
            NPCID.DevourerTail,
            NPCID.GiantWormHead,
            NPCID.GiantWormBody,
            NPCID.GiantWormTail,
            NPCID.EaterofWorldsHead,
            NPCID.EaterofWorldsBody,
            NPCID.EaterofWorldsTail,
            NPCID.WyvernHead,
            NPCID.WyvernLegs,
            NPCID.WyvernBody,
            NPCID.WyvernBody2,
            NPCID.WyvernBody3,
            NPCID.WyvernTail,
            NPCID.DiggerHead,
            NPCID.DiggerBody,
            NPCID.DiggerTail,
            NPCID.SeekerHead,
            NPCID.SeekerBody,
            NPCID.SeekerTail,
            NPCID.LeechHead,
            NPCID.LeechBody,
            NPCID.LeechTail,
            NPCID.TheDestroyer,
            NPCID.TheDestroyerBody,
            NPCID.TheDestroyerTail,
            NPCID.Golem,
            NPCID.GolemHead,
            NPCID.GolemFistLeft,
            NPCID.GolemFistRight,
            NPCID.StardustWormHead,
            NPCID.StardustWormBody,
            NPCID.StardustWormTail,
            NPCID.SolarCrawltipedeHead,
            NPCID.SolarCrawltipedeBody,
            NPCID.SolarCrawltipedeTail,
            NPCID.CultistDragonHead,
            NPCID.CultistDragonBody1,
            NPCID.CultistDragonBody2,
            NPCID.CultistDragonBody3,
            NPCID.CultistDragonBody4,
            NPCID.CultistDragonTail,
            NPCID.BloodEelHead,
            NPCID.BloodEelBody,
            NPCID.BloodEelTail,
        ];

        public static HashSet<string> SegmentedModNPCs =
        [
            "CalamityMod/EidolonWyrmHead",
            "CalamityMod/EidolonWyrmBody",
            "CalamityMod/EidolonWyrmBodyAlt",
            "CalamityMod/EidolonWyrmTail",
            "CalamityMod/GulperEelHead",
            "CalamityMod/GulperEelBody",
            "CalamityMod/GulperEelBodyAlt",
            "CalamityMod/GulperEelTail",
            "CalamityMod/OarfishHead",
            "CalamityMod/OarfishBody",
            "CalamityMod/OarfishTail",
            "CalamityMod/AquaticScourgeHead",
            "CalamityMod/AquaticScourgeBody",
            "CalamityMod/AquaticScourgeBodyAlt",
            "CalamityMod/AquaticScourgeTail",
            "CalamityMod/AstrumDeusHead",
            "CalamityMod/AstrumDeusBody",
            "CalamityMod/AstrumDeusTail",
            "CalamityMod/DesertNuisanceHead",
            "CalamityMod/DesertNuisanceHeadYoung",
            "CalamityMod/DesertNuisanceBody",
            "CalamityMod/DesertNuisanceBodyYoung",
            "CalamityMod/DesertNuisanceTail",
            "CalamityMod/DesertNuisanceTailYoung",
            "CalamityMod/DesertScourgeHead",
            "CalamityMod/DesertScourgeBody",
            "CalamityMod/DesertScourgeTail",
            "CalamityMod/CosmicGuardianHead",
            "CalamityMod/CosmicGuardianBody",
            "CalamityMod/CosmicGuardianTail",
            "CalamityMod/DevourerofGodsHead",
            "CalamityMod/DevourerofGodsBody",
            "CalamityMod/DevourerofGodsTail",
            "CalamityMod/ThanatosHead",
            "CalamityMod/ThanatosBody1",
            "CalamityMod/ThanatosBody2",
            "CalamityMod/ThanatosTail",
            "CalamityMod/ArmoredDiggerHead",
            "CalamityMod/ArmoredDiggerBody",
            "CalamityMod/ArmoredDiggerTail",
            "CalamityMod/PerforatorHeadLarge",
            "CalamityMod/PerforatorHeadMedium",
            "CalamityMod/PerforatorHeadSmall",
            "CalamityMod/PerforatorBodyLarge",
            "CalamityMod/PerforatorBodyMedium",
            "CalamityMod/PerforatorBodySmall",
            "CalamityMod/PerforatorTailLarge",
            "CalamityMod/PerforatorTailMedium",
            "CalamityMod/PerforatorTailSmall",
            "CalamityMod/PrimordialWyrmHead",
            "CalamityMod/PrimordialWyrmBody",
            "CalamityMod/PrimordialWyrmBodyAlt",
            "CalamityMod/PrimordialWyrmTail",
            "CalamityMod/RavagerHead",
            "CalamityMod/RavagerHead2",
            "CalamityMod/RavagerBody",
            "CalamityMod/ClawLeft",
            "CalamityMod/ClawRight",
            "CalamityMod/LegLeft",
            "CalamityMod/LegRight",
            "CalamityMod/StormWeaverHead",
            "CalamityMod/StormWeaverBody",
            "CalamityMod/StormWeaverTail",
            "CalamityMod/SeaSerpent1",
            "CalamityMod/SeaSerpent2",
            "CalamityMod/SeaSerpent3",
            "CalamityMod/SeaSerpent4",
            "CalamityMod/SeaSerpent5",
            "CalamityMod/SepulcherHead",
            "CalamityMod/SepulcherBody",
            "CalamityMod/SepulcherArm",
            "CalamityMod/SepulcherTail",
        ];
        */
    }
}
