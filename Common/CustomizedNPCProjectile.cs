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

namespace SimpleNPCStats2.Common
{
    public class CustomizedNPCProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override void Load()
        {
            On_Projectile.Update += On_Projectile_Update;
            IL_Projectile.Update += IL_Projectile_Update;
            IL_Projectile.UpdatePosition += IL_Projectile_UpdatePosition;
        }

        public float Scale { get; private set; } = 1;
        public float MovementSpeed { get; private set; } = 1;
        public float AISpeed { get; private set; } = 1;
        public float AISpeedCounter { get; private set; } = 0;

        private static void On_Projectile_Update(On_Projectile.orig_Update orig, Projectile self, int i)
        {
            if (self.active && self.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result) && result.Enabled)
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

        public bool Enabled { get; private set; }

        private TempStats? tempStats;
        public struct TempStats
        {
            public float scale;
            public int width;
            public int height;
            public Vector2 positionShift;

            public readonly void UpdateProjectile(Projectile projectile)
            {
                projectile.scale = scale;
                projectile.width = width;
                projectile.height = height;
            }

            public static TempStats Create(Projectile projectile)
            {
                var stats = new TempStats()
                {
                    scale = projectile.scale,
                    width = projectile.width,
                    height = projectile.height,
                };
                return stats;
            }
        }
        private static void IL_Projectile_Update(ILContext context)
        {
            try
            {
                ILCursor cursor;

                cursor = new ILCursor(context);
                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchCall<Projectile>("AI")
                    ))
                {
                    cursor.EmitLdarga(0);
                    cursor.EmitDelegate((ref Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result))
                        {
                            if (result.Enabled)
                            {
                                if (result.tempStats != null)
                                {
                                    result.tempStats.Value.UpdateProjectile(projectile);
                                    projectile.position += result.tempStats.Value.positionShift;
                                }
                            }
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCall<Projectile>("AI")
                    ))
                {
                    cursor.EmitLdarga(0);
                    cursor.EmitDelegate((ref Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result))
                        {
                            if (result.Enabled)
                            {
                                var tempStats = TempStats.Create(projectile);

                                var oldWidth = projectile.width;
                                var oldHeight = projectile.height;

                                projectile.scale *= result.Scale;
                                projectile.width = Math.Max((int)(projectile.width * result.Scale), 1);
                                projectile.height = Math.Max((int)(projectile.height * result.Scale), 1);

                                tempStats.positionShift = new Vector2((projectile.width - oldWidth) / 2f, (projectile.height - oldHeight) / 2f);
                                projectile.position -= tempStats.positionShift;

                                result.tempStats = tempStats;
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

        private static void IL_Projectile_UpdatePosition(ILContext context)
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
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStfld<Entity>("position")
                    ))
                {
                    cursor.EmitLdarga(0);
                    cursor.EmitLdarg1();
                    cursor.EmitDelegate((ref Projectile projectile, Vector2 wetVelocity) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result))
                        {
                            if (result.Enabled)
                            {
                                projectile.position += wetVelocity * (result.MovementSpeed - 1);
                            }
                        }
                    });
                }

                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("position"),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Entity>("velocity"),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStfld<Entity>("position")
                    ))
                {
                    cursor.EmitLdarga(0);
                    cursor.EmitDelegate((ref Projectile projectile) =>
                    {
                        if (projectile.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result))
                        {
                            if (result.Enabled)
                            {
                                projectile.position += projectile.velocity * (result.MovementSpeed - 1);
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

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_Parent { Entity: NPC npc })
            {
                if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.Enabled)
                {
                    Enabled = true;
                    Scale = result.Scale;
                    MovementSpeed = result.MovementSpeed;
                    AISpeed = result.AISpeed;
                }
            }
        }
    }
}
