using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using MonoMod.Cil;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using SimpleNPCStats2.Common.Config;
using Newtonsoft.Json.Linq;
using Terraria.GameContent.Creative;
using Terraria.WorldBuilding;
using Terraria.ID;
using System.Reflection;

namespace SimpleNPCStats2.Common
{
    public class CustomizedNPCProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool Enabled => Stats != null;
        public ConfigData.NPCGroup.StatSet Stats { get; private set; }

        public float Scale { get; private set; }
        public float MovementSpeed { get; private set; }
        public float AISpeed { get; private set; }
        public float AISpeedCounter { get; private set; }

        
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_Parent { Entity: NPC npc })
            {
                if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled)
                {
                    Stats = result.Stats;
                    Scale = result.Scale;
                    MovementSpeed = result.MovementSpeed;
                    AISpeed = result.AISpeed;
                    AISpeedCounter = 1 - AISpeed;

                    projectile.scale *= Scale;
                    projectile.width = Math.Max(1, (int)(projectile.width * Scale));
                    projectile.height = Math.Max(1, (int)(projectile.height * Scale));

                    if (result.OldStatInfo.defDamage != 0) // Unknown what to do if there's an increase from 0, so do nothing
                    {
                        if (result.NewStatInfo.defDamage == 0) // Clamp damage to 0-1 if damage is decreased to 0
                        {
                            projectile.damage = Math.Min(projectile.damage, 1);
                        }
                        else
                        {
                            projectile.damage = Math.Max(0, (int)(projectile.damage * ((float)result.NewStatInfo.defDamage / result.OldStatInfo.defDamage)));
                        }
                    }
                }
            }
        }

        public static void IL_Projectile_Update(ILContext context)
        {
            try
            {
                ILCursor cursor = new ILCursor(context);

                // Method gets inlined so done as IL edit instead of detour
                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchCall("Terraria.Projectile", "AI")
                    ))
                {
                    cursor.EmitLdarg0();
                    cursor.EmitDelegate((Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result) && result.Enabled)
                        {
                            result.AISpeedCounter += result.AISpeed;
                            while (result.AISpeedCounter >= 1)
                            {
                                result.AISpeedCounter--;
                                projectile.AI();
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

                cursor.UpdateInstructionOffsets();
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }

        public static void IL_Projectile_UpdatePosition(ILContext context)
        {
            try
            {
                ILCursor cursor;

                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("position"),
                    i => i.MatchLdarg1(),
                    // HERE
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStfld<Entity>("position")
                    ))
                {
                    cursor.Index -= 2;
                    cursor.EmitLdarg(0);
                    cursor.EmitDelegate((Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result) && result.Enabled)
                        {
                            return result.MovementSpeed;
                        }
                        return 1;
                    });
                    cursor.EmitCall(typeof(Vector2).GetMethod("op_Multiply", BindingFlags.Static | BindingFlags.Public, null, [typeof(Vector2), typeof(float)], null));
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("position"),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("velocity"),
                    // HERE
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStfld<Entity>("position")
                    ))
                {
                    cursor.Index -= 2;
                    cursor.EmitLdarg(0);
                    cursor.EmitDelegate((Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result) && result.Enabled)
                        {
                            return result.MovementSpeed;
                        }
                        return 1;
                    });
                    cursor.EmitCall(typeof(Vector2).GetMethod("op_Multiply", BindingFlags.Static | BindingFlags.Public, null, [typeof(Vector2), typeof(float)], null));
                }
            }
            catch (Exception)
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<SimpleNPCStats2>(), context);
            }
        }
    }
}
