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

namespace SimpleNPCStats2.Common
{
    public class CustomizedNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void Load()
        {
            // AISpeed
            On_NPC.UpdateNPC += On_NPC_AISpeed;

            // Movement speed
            IL_NPC.UpdateNPC_Inner += IL_NPC_Movement;

            // Resetting values for NPC transforming (such as mini stardust cells > big stardust cells, spiders on walls/floor)
            On_NPC.Transform += On_NPC_TransformFix;

            // Life regen after all other mods
            IL_NPC.UpdateNPC_BuffApplyDOTs += IL_NPC_LifeRegen;

            On_NPC.SetDefaults += On_NPC_SetDefaults;
            On_NPC.SetDefaultsFromNetId += On_NPC_SetDefaultsFromNetId;
        }

        private static void On_NPC_SetDefaultsFromNetId(On_NPC.orig_SetDefaultsFromNetId orig, NPC self, int id, NPCSpawnParams spawnparams)
        {
            if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
            {
                result._tempNetId = id;
            }
            orig(self, id, spawnparams);
        }

        private int _tempNetId;
        private static void On_NPC_SetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams)
        {
            orig(self, Type, spawnparams);
            if (SimpleNPCStats2.ModsLoaded)
            {
                if (Type > 0)
                {
                    if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
                    {
                        int type = result._tempNetId == 0 ? self.type : result._tempNetId;
                        result.Setup(self, type);
                        result._tempNetId = 0;
                    }
                }
            }
        }

        private bool _lifeRegenModified;
        private static void IL_NPC_LifeRegen(ILContext context)
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
                            if (result.Enabled)
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
                                    result._lifeRegenModified = true;
                                    npc.lifeRegen = (int)(npc.lifeRegen / result.AISpeed);
                                    return;
                                }

                                npc.lifeRegen = (int)(npc.lifeRegen / result.AISpeed);
                            }
                        }
                        result._lifeRegenModified = false;
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
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                if (result._lifeRegenModified)
                                {
                                    return regenNumber;
                                }
                            }
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
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                if (result._lifeRegenModified)
                                {
                                    npc.HealEffect(regenNumber);
                                    return regenNumber; 
                                }
                            }
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
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                        {
                            if (result.Enabled)
                            {
                                if (result._lifeRegenModified)
                                {
                                    return regenNumber; 
                                }
                            }
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

        private void On_NPC_TransformFix(On_NPC.orig_Transform orig, NPC self, int newType)
        {
            orig(self, newType);
            if (self.TryGetGlobalNPC<CustomizedNPC>(out var result))
            {
                result.Setup(self, self.type);
            }
        }

        // AI Speed
        public float AISpeedCounter { get; private set; }
        private static void On_NPC_AISpeed(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            if (self.active && self.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled)
            {
                result.AISpeedCounter += result.AISpeed;
                while (result.AISpeedCounter >= 1)
                {
                    orig(self, i);
                    result.AISpeedCounter--;
                }
            }
            else
            {
                orig(self, i);
            }
        }

        // Movement Speed

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
        private static void IL_NPC_Movement(ILContext il)
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
                            if (result.Enabled)
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
                            if (result.Enabled && result.MovementSpeed != 0)
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
                            if (result.Enabled)
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
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), il);
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (Enabled)
            {
                npc.MaxFallSpeedMultiplier *= Gravity;
                npc.GravityMultiplier *= Gravity;
                npc.MaxFallSpeedMultiplier /= MovementSpeed;
                npc.GravityMultiplier /= MovementSpeed;
            }
            return true;
        }

        public bool HasSetup { get; private set; }
        public bool Enabled => Stats != null;
        private ConfigData.NPCGroup.StatSet Stats;

        public float MovementSpeed { get; private set; } = 1f;
        public float Gravity { get; private set; } = 1f;
        public float Scale { get; private set; } = 1f;
        public (float? min, float? max)? ScaleClamp { get; private set; }
        public float AISpeed { get; private set; } = 1f;
        public bool UsesDynamicScaling { get; private set; }

        private bool Setup(NPC npc, int type)
        {
            if (ConfigSystem.StaticNPCData.TryGetValue(type, out var value))
            {
                HasSetup = true;

                Stats = value;

                Gravity = Stats.gravity.GetValue(1);
                AISpeed = Stats.aiSpeed.GetValue(1);
                MovementSpeed = Stats.movement.GetValue(1);
                Scale = Stats.scale.GetValue(1);

                if (NPCFixes.ScaleClampNPCIds.TryGetValue(type, out var id))
                {
                    ScaleClamp = (id.minScale, id.maxScale);
                }

                if (NPCFixes.DynamicScalingNPCIds.Contains(type))
                {
                    UsesDynamicScaling = true;
                }
                else
                {
                    ScaleNPC(npc, GetScaleIncreaseClamped(npc));
                }

                tempStats = null;
                return true;
            }
            else
            {
                Stats = null;
                return false;
            }
        }

        public float GetScaleIncreaseClamped(NPC npc)
        {
            var desiredScale = Scale;

            if (ScaleClamp != null)
            {
                if (ScaleClamp.Value.min != null)
                {
                    desiredScale = Math.Max(desiredScale, ScaleClamp.Value.min.Value);
                }
                if (ScaleClamp.Value.max != null)
                {
                    desiredScale = Math.Min(desiredScale, ScaleClamp.Value.max.Value);
                }
            }

            return desiredScale;
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (Enabled)
            {
                if (UsesDynamicScaling)
                {
                    var newHeight = (int)(npc.height * Scale);
                    npc.position.Y -= (newHeight - npc.height) / 2;
                }
            }
        }

        public static void ScaleNPC(NPC npc, float value)
        {
            npc.scale *= value;
            npc.width = Math.Max((int)(npc.width * value), 1);
            npc.height = Math.Max((int)(npc.height * value), 1);
        }

        /*
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
