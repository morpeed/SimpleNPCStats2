using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SimpleNPCStats2.Common.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace SimpleNPCStats2.Common
{
    public class SNSSystem : ModSystem
    {
        public override void PostSetupRecipes()
        {
            On_NPC.UpdateNPC += CustomizedNPC.On_NPC_UpdateNPC;
            On_NPC.SetDefaultsFromNetId += CustomizedNPC.On_NPC_SetDefaultsFromNetId;
            On_NPC.SetDefaults += CustomizedNPC.On_NPC_SetDefaults;

            On_Projectile.Update += CustomizedNPCProjectile.On_Projectile_Update;
        }

        public override void Load()
        {
            IL_NPC.UpdateNPC_Inner += CustomizedNPC.IL_NPC_Movement;
            IL_NPC.UpdateNPC_BuffApplyDOTs += CustomizedNPC.IL_NPC_LifeRegen;
            //IL_NPC.NewNPC += CustomizedNPC.IL_NPC_NewNPC;
            IL_NPC.AI_006_Worms += IL_NPC_AI_006_Worms;
            //IL_NPC.VanillaAI_Inner += IL_NPC_VanillaAI_Inner; // Too big, don't work, go see NPCOverrides
            IL_NPC.AI_121_QueenSlime += IL_NPC_AI_121_QueenSlime;
            IL_NPC.AI_002_FloatingEye += IL_NPC_AI_002_FloatingEye;
            //IL_NPC.AI_003_Fighters += IL_NPC_AI_003_Fighters;// Again, too big, don't work, go see NPCOverrides

            IL_Projectile.UpdatePosition += CustomizedNPCProjectile.IL_Projectile_UpdatePosition;
        }

        private static void MultiplyNPCByScale(ILCursor cursor)
        {
            cursor.EmitLdarga(0);
            cursor.EmitDelegate((ref NPC npc) =>
            {
                if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                {
                    npc.scale *= result.Scale;
                }
            });
        }

        private static void IL_NPC_AI_002_FloatingEye(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                         i => i.MatchLdloc3(),
                         i => i.MatchLdcR4(1),
                         i => i.MatchLdcR4(1),
                         i => i.MatchLdarg0(),
                         i => i.MatchLdfld("Terraria.NPC", "scale"),
                         i => i.MatchSub(),
                         i => i.MatchAdd(),
                         i => i.MatchMul(),
                         i => i.MatchStloc3()
                        ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitLdloca(2);
                    cursor.EmitLdloca(3);
                    cursor.EmitDelegate((NPC npc, ref float xSpeed, ref float ySpeed) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            xSpeed = Math.Max(2, xSpeed);
                            ySpeed = Math.Max(0.75f, ySpeed);
                        }
                    });
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
        // Scales the distance between worm enemy segments accordingly
        private static void IL_NPC_AI_006_Worms(ILContext context)
        {
            try
            {
                ILCursor cursor;

                void ScaleDistanceValueMul()
                {
                    cursor.EmitLdarg0();
                    cursor.EmitDelegate((int distance, NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            return (int)(distance * result.Scale);
                        }
                        return 1f;
                    });
                }

                cursor = new ILCursor(context);

                // Wyvern
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(42),
                    i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index--;
                    ScaleDistanceValueMul();
                }

                // Phantasm Dragon
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(36),
                    i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index--;
                    ScaleDistanceValueMul();
                }

                // Eater of Worlds (non For the worthy)
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(89),
                    i => i.MatchConvR4(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld("Terraria.NPC", "scale"),
                    i => i.MatchMul(),
                    i => i.MatchConvI4(),
                    i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index -= 2;
                    cursor.EmitLdarg0();
                    cursor.EmitDelegate((NPC npc) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            return result.Scale;
                        }
                        return 1f;
                    });
                    cursor.EmitDiv();
                }

                // Tomb Crawler
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(89),
                     i => i.MatchLdcI4(6),
                     i => i.MatchSub(),
                     i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index -= 2;
                    ScaleDistanceValueMul();
                }

                // Crawltipede
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(89),
                     i => i.MatchLdcI4(6),
                     i => i.MatchAdd(),
                     i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index -= 2;
                    ScaleDistanceValueMul();
                }

                // Blood Eel
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(24),
                    i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index--;
                    ScaleDistanceValueMul();
                }

                // Eater of Worlds (For the worthy)
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(62),
                    i => i.MatchStloc(89)
                    ))
                {
                    cursor.Index--;
                    ScaleDistanceValueMul();
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
        private void IL_NPC_AI_121_QueenSlime(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);

                // Scale fix
                if (cursor.TryGotoNext(MoveType.After,
                         i => i.MatchLdarg0(),
                         i => i.MatchLdloc2(),
                         i => i.MatchStfld("Terraria.NPC", "scale")
                        ))
                {
                    MultiplyNPCByScale(cursor);
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        private static void IL_NPC_AI_003_Fighters(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(141),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "scale"),
                     i => i.MatchSub(),
                     i => i.MatchAdd(),
                     i => i.MatchMul(),
                     i => i.MatchStloc(141)
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitLdloca(141);
                    cursor.EmitDelegate((NPC npc, ref float speed) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            speed = Math.Max(0.75f, speed);
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(142),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "scale"),
                     i => i.MatchSub(),
                     i => i.MatchAdd(),
                     i => i.MatchMul(),
                     i => i.MatchStloc(142)
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitLdloca(142);
                    cursor.EmitDelegate((NPC npc, ref float speed) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            speed = Math.Max(0.75f, speed);
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(163),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "scale"),
                     i => i.MatchSub(),
                     i => i.MatchAdd(),
                     i => i.MatchMul(),
                     i => i.MatchStloc(163)
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitLdloca(163);
                    cursor.EmitDelegate((NPC npc, ref float speed) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            speed = Math.Max(0.5f, speed);
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdloc(166),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "scale"),
                     i => i.MatchSub(),
                     i => i.MatchAdd(),
                     i => i.MatchMul(),
                     i => i.MatchStloc(166)
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitLdloca(166);
                    cursor.EmitDelegate((NPC npc, ref float speed) =>
                    {
                        if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled && result.OverrideModifyAI)
                        {
                            speed = Math.Max(0.5f, speed);
                        }
                    });
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
        private static void IL_NPC_VanillaAI_Inner(ILContext context)
        {
            try
            {
                ILCursor cursor;

                // Scale fixes
                // King Slime
                cursor = new ILCursor(context);

                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdloc(327),
                     i => i.MatchNop(),
                     i => i.MatchNop(),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Firefly (lol)
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "ai"),
                     i => i.MatchLdcI4(out _),
                     i => i.MatchLdelemR4(),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Snail (lolol)
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "ai"),
                     i => i.MatchLdcI4(out _),
                     i => i.MatchLdelemR4(),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Duke Fishron bubble
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "ai"),
                     i => i.MatchLdcI4(out _),
                     i => i.MatchLdelemR4(),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Mini Stardust Cell I think
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(0.3f),
                     i => i.MatchLdloc(2170),
                     i => i.MatchNop(),
                     i => i.MatchNop(),
                     i => i.MatchMul(),
                     i => i.MatchAdd(),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Ancient Doom - the purple attack thing from Lunatic Cultist
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdloc(2261),
                     i => i.MatchNop(),
                     i => i.MatchNop(),
                     i => i.MatchLdloc(2262),
                     i => i.MatchNop(),
                     i => i.MatchNop(),
                     i => i.MatchLdloc(2265),
                     i => i.MatchNop(),
                     i => i.MatchNop(),
                     i => i.MatchCall("Microsoft.Xna.Framework.MathHelper", "Lerp"),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }

                // Old one's army portal, because yeah
                if (cursor.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg0(),
                     i => i.MatchLdcR4(1),
                     i => i.MatchLdcR4(0.05f),
                     i => i.MatchLdcR4(500),
                     i => i.MatchLdcR4(600),
                     i => i.MatchLdarg0(),
                     i => i.MatchLdfld("Terraria.NPC", "ai"),
                     i => i.MatchLdcI4(out _),
                     i => i.MatchLdelemR4(),
                     i => i.MatchLdcI4(out _),
                     i => i.MatchCall("Terraria.Utils", "GetLerpValue"),
                     i => i.MatchCall("Microsoft.Xna.Framework.MathHelper", "Lerp"),
                     i => i.MatchStfld("Terraria.NPC", "scale")
                    ))
                {
                    MultiplyNPCByScale(cursor);
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
    }
}
