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
        private bool _AISpeedImmediateUpdate;

        public static void On_Projectile_Update(On_Projectile.orig_Update orig, Projectile self, int i)
        {
            if (self.active && self.TryGetGlobalProjectile<CustomizedNPCProjectile>(out var result) && result.Enabled)
            {
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
            }
            else
            {
                orig(self, i);
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
                    Stats = result.Stats;
                    Scale = result.Scale;
                    MovementSpeed = result.MovementSpeed;
                    AISpeed = result.AISpeed;
                    _AISpeedImmediateUpdate = true;
                    AISpeedCounter = -1;

                    projectile.scale *= Scale;
                    projectile.width = Math.Max(1, (int)(projectile.width * Scale));
                    projectile.height = Math.Max(1, (int)(projectile.height * Scale));

                    float ogNPCDamage = ContentSamples.NpcsByNetId[result.TypeNetId].damage;
                    projectile.damage = (int)Math.Max(0, projectile.damage * (npc.damage / ogNPCDamage));
                }
            }
        }
    }
}
