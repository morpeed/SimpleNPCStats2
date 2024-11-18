using Microsoft.Xna.Framework;
using MonoMod.Cil;
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
            IL_NPC.AI_006_Worms += IL_NPC_AI_006_Worms;
            //IL_NPC.VanillaAI_Inner += IL_NPC_VanillaAI_Inner; // Too big, don't work, go see NPCOverrides
            IL_NPC.UpdateNPC_Inner += CustomizedNPC.IL_NPC_Movement;
            IL_NPC.UpdateNPC_BuffApplyDOTs += CustomizedNPC.IL_NPC_LifeRegen;
            IL_NPC.AI_121_QueenSlime += IL_NPC_AI_121_QueenSlime;

            IL_Projectile.UpdatePosition += CustomizedNPCProjectile.IL_Projectile_UpdatePosition;
        }

        /// <summary>
        /// Use this method to multiply the value on the stack with the customized NPC scale parameter.
        /// </summary>
        /// <param name="cursor"></param>
        private static void MultiplyValueByScale(ILCursor cursor)
        {
            cursor.EmitLdarg0(); // NPC instance
            cursor.EmitDelegate((NPC npc) =>
            {
                return CustomizedNPC.GetScaleMultiplier(npc);
            });
            cursor.EmitMul();
        }
        private static void DivideValueByScale(ILCursor cursor)
        {
            cursor.EmitLdarg0(); // NPC instance
            cursor.EmitDelegate((NPC npc) =>
            {
                return CustomizedNPC.GetScaleMultiplier(npc);
            });
            cursor.EmitDiv();
        }
        private static void MultiplyNPCByScale(ILCursor cursor)
        {
            cursor.EmitLdarga(0);
            cursor.EmitDelegate((ref NPC npc) =>
            {
                npc.scale *= CustomizedNPC.GetScaleMultiplier(npc);
            });
        }

        private static void IL_NPC_AI_006_Worms(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
                // Multiplying the variable that determines the distance between segments
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(12),
                    i => i.MatchLdloc(89),
                    i => i.MatchConvR4(),
                    // HERE
                    i => i.MatchSub(),
                    i => i.MatchLdloc(12),
                    i => i.MatchDiv(),
                    i => i.MatchStloc(12)
                    ))
                {
                    cursor.Index -= 4;
                    DivideValueByScale(cursor);
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
    }
}
