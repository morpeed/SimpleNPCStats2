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

namespace SimpleNPCStats2.Common
{
    /// <summary>
    /// This class is used to add NPC specific fixes, such as making vanilla AI worms scale their segments correctly.
    /// </summary>
    public class NPCFixes : ModSystem
    {
        public override void Load()
        {
            IL_NPC.AI_006_Worms += IL_NPC_AI_006_Worms;
            //IL_NPC.AI_037_Destroyer += IL_NPC_AI_037_Destroyer;
        }

        public static readonly Dictionary<int, (float? minScale, float? maxScale)> ScaleClampNPCIds = new()
        {
            { NPCID.Golem, (null, 1.5f) },
            { NPCID.GolemFistLeft, (null, 1.5f) },
            { NPCID.GolemFistRight, (null, 1.5f) },
            { NPCID.GolemHead, (null, 1.5f) },
            { NPCID.GolemHeadFree, (null, 1.5f) } // Not needed technically, but for consistency
        };

        public static readonly HashSet<int> DynamicScalingNPCIds = new()
        {
            NPCID.KingSlime,
            NPCID.QueenSlimeBoss
        };

        /// <summary>
        /// Use this method to multiply the value on the stack with the customized NPC scale parameter.
        /// </summary>
        /// <param name="cursor"></param>
        private static void MultiplyByScale(ILCursor cursor)
        {
            cursor.EmitLdarg0(); // NPC instance
            cursor.EmitDelegate((NPC npc) =>
            {
                return GetScaleMultiplier(npc);
            });
            cursor.EmitMul();
        }

        private static float GetScaleMultiplier(NPC npc)
        {
            if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
            {
                if (result.Enabled)
                {
                    return 1f * result.Scale;
                }
            }
            return 1f;
        }

        private static void IL_NPC_AI_006_Worms(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
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
                    MultiplyByScale(cursor);
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        private static void IL_NPC_AI_037_Destroyer(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcR4(44),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("scale"),
                    i => i.MatchMul(),
                    // HERE
                    i => i.MatchConvI4(),
                    i => i.MatchStloc(42)
                    ))
                {
                    cursor.Index -= 2;
                    MultiplyByScale(cursor);
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

                // Golem Head
                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchNop(),
                    i => i.MatchLdloc(943),
                    i => i.MatchNop(),
                    i => i.MatchNop(),
                    i => i.MatchLdcR4(57),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("scale"),
                    i => i.MatchMul(),
                    // HERE
                    i => i.MatchSub(),
                    i => i.MatchStloc(943),
                    i => i.MatchNop()
                    ))
                {
                    cursor.EmitDelegate(() =>
                    {
                        cursor.Index -= 3;
                        MultiplyByScale(cursor);
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchNop(),
                    i => i.MatchLdloc(942),
                    i => i.MatchNop(),
                    i => i.MatchNop(),
                    i => i.MatchLdcR4(3),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("scale"),
                    i => i.MatchMul(),
                    // HERE
                    i => i.MatchSub(),
                    i => i.MatchStloc(942),
                    i => i.MatchNop()
                    ))
                {
                    cursor.Index -= 3;
                    MultiplyByScale(cursor);
                }
                // End
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        private void IL_NPC_AI_047_GolemFist(ILContext context)
        {
            try
            {
                ILCursor cursor;
                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcR4(0),
                    i => i.MatchLdcR4(-9),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("scale"),
                    i => i.MatchMul()
                    ))
                {
                    MultiplyByScale(cursor);
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>("scale"),
                    i => i.MatchMul()
                    ))
                {
                    MultiplyByScale(cursor);
                }
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
    }
}
