using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SimpleNPCStats2.Common
{
    /// <summary>
    /// IL modifications on large methods (i.e. vanilla AI) do not function, this finnicky workaround is required instead.
    /// Fighters, King Slime, Firefly, Snail, Duke Fishron Bubble, Small Star Cell, Ancient Doom, DD2 Portal
    /// </summary>
    internal class NPCOverrides : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (npc.aiStyle == NPCAIStyleID.Fighter || npc.aiStyle == NPCAIStyleID.KingSlime || npc.aiStyle == NPCAIStyleID.DukeFishronBubble || npc.aiStyle == NPCAIStyleID.SmallStarCell || npc.aiStyle == NPCAIStyleID.AncientDoom || npc.aiStyle == NPCAIStyleID.DD2MysteriousPortal)
            {
                _overrideAiStyle = npc.aiStyle;
                npc.aiStyle = -1;
            }
        }

        private int _overrideAiStyle = -1;

        public override void AI(NPC npc)
        {  
            if (_overrideAiStyle == -1)
            {
                return;
            }

            switch (_overrideAiStyle)
            {
                case NPCAIStyleID.Fighter:
                    AI_003_Fighters(npc);
                    break;

                case NPCAIStyleID.KingSlime:
                    AI_015_KingSlime(npc);
                    break;

                case NPCAIStyleID.DukeFishronBubble:
                    AI_070_DukeFishronBubble(npc);
                    break;

                case NPCAIStyleID.SmallStarCell:
                    AI_95_SmallStarCell(npc);
                    break;

                case NPCAIStyleID.AncientDoom:
                    AI_101_AncientDoom(npc);
                    break;

                case NPCAIStyleID.DD2MysteriousPortal:
                    AI_106_DD2MysteriousPortal(npc);
                    break;
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return true;
            }

            if (npc.aiStyle == NPCAIStyleID.Snail && npc.type == NPCID.Snail) // Snail scale fix
            {
                if (npc.ai[3] == 0)
                {
                    npc.ai[3] = Main.rand.Next(80, 111) * 0.01f * CustomizedNPC.GetScaleMultiplier(npc);
                    npc.netUpdate = true;
                }
            }
            else if (npc.aiStyle == NPCAIStyleID.Firefly) // Firefly scale fix
            {
                if (npc.ai[3] == 0)
                {
                    npc.ai[3] = Main.rand.Next(75, 111) * 0.01f * CustomizedNPC.GetScaleMultiplier(npc);
                    npc.netUpdate = true;
                }
            }

            return true;
        }

        private static void AI_003_Fighters(NPC npc)
        {
            bool AI_003_Gnomes_ShouldTurnToStone()
            {
                if (Main.remixWorld)
                {
                    return npc.position.Y / 16f > (float)(Main.maxTilesY - 350);
                }
                if (Main.dayTime)
                {
                    return WorldGen.InAPlaceWithWind(npc.position, npc.width, npc.height);
                }
                return false;
            }

            void CountKillForBannersAndDropThem()
            {
                int num = Item.NPCtoBanner(npc.BannerID());
                if (num <= 0 || npc.ExcludedFromDeathTally())
                {
                    return;
                }
                NPC.killCount[num]++;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.NPCKillCountDeathTally, -1, -1, null, num);
                }
                int num2 = ItemID.Sets.KillsToBanner[Item.BannerToItem(num)];
                if (NPC.killCount[num] % num2 == 0 && num > 0)
                {
                    int num3 = Item.BannerToNPC(num);
                    int num4 = npc.lastInteraction;
                    if (!Main.player[num4].active || Main.player[num4].dead)
                    {
                        num4 = npc.FindClosestPlayer();
                    }
                    NetworkText networkText = NetworkText.FromKey("Game.EnemiesDefeatedAnnouncement", NPC.killCount[num], NetworkText.FromKey(Lang.GetNPCName(num3).Key));
                    if (num4 >= 0 && num4 < 255)
                    {
                        networkText = NetworkText.FromKey("Game.EnemiesDefeatedByAnnouncement", Main.player[num4].name, NPC.killCount[num], NetworkText.FromKey(Lang.GetNPCName(num3).Key));
                    }
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        Main.NewText(networkText.ToString(), 250, 250, 0);
                    }
                    else if (Main.netMode == NetmodeID.Server)
                    {
                        ChatHelper.BroadcastChatMessage(networkText, new Color(250, 250, 0));
                    }
                    int num5 = Item.BannerToItem(num);
                    Vector2 vector = npc.position;
                    if (num4 >= 0 && num4 < 255)
                    {
                        vector = Main.player[num4].position;
                    }
                    Item.NewItem(npc.GetSource_Loot(), (int)vector.X, (int)vector.Y, npc.width, npc.height, num5);
                }
            }

            if (Main.player[npc.target].position.Y + (float)Main.player[npc.target].height == npc.position.Y + (float)npc.height)
            {
                npc.directionY = -1;
            }
            bool flag = false;
            if (npc.type == NPCID.Gnome && AI_003_Gnomes_ShouldTurnToStone())
            {
                int num = (int)(npc.Center.X / 16f);
                int num110 = (int)(npc.Bottom.Y / 16f);
                npc.position += npc.netOffset;
                int num127 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDustLighted, 0f, 0f, 254, Color.White, 0.5f);
                Main.dust[num127].velocity *= 0.2f;
                npc.position -= npc.netOffset;
                if (WorldGen.SolidTileAllowBottomSlope(num, num110))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        npc.position += npc.netOffset;
                        int num138 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDustLighted, 0f, 0f, 254, Color.White, 0.5f);
                        Main.dust[num138].velocity *= 0.2f;
                        npc.position -= npc.netOffset;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient && TileObject.CanPlace(num, num110 - 1, 567, 0, npc.direction, out var _, onlyCheck: true) && WorldGen.PlaceTile(num, num110 - 1, 567, mute: false, forced: false, -1, Main.rand.Next(5)))
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendTileSquare(-1, num, num110 - 2, 1, 2);
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (npc.IsNPCValidForBestiaryKillCredit())
                            {
                                Main.BestiaryTracker.Kills.RegisterKill(npc);
                            }
                            CountKillForBannersAndDropThem();
                        }
                        npc.life = 0;
                        npc.active = false;
                        AchievementsHelper.NotifyProgressionEvent(24);
                        return;
                    }
                }
            }
            if (npc.type == NPCID.Psycho)
            {
                int num148 = 200;
                if (npc.ai[2] == 0f)
                {
                    npc.alpha = num148;
                    npc.TargetClosest();
                    if (!Main.player[npc.target].dead && (Main.player[npc.target].Center - npc.Center).Length() < 170f)
                    {
                        npc.ai[2] = -16f;
                    }
                    if (npc.velocity.X != 0f || npc.velocity.Y < 0f || npc.velocity.Y > 2f || npc.justHit)
                    {
                        npc.ai[2] = -16f;
                    }
                    return;
                }
                if (npc.ai[2] < 0f)
                {
                    if (npc.alpha > 0)
                    {
                        npc.alpha -= num148 / 16;
                        if (npc.alpha < 0)
                        {
                            npc.alpha = 0;
                        }
                    }
                    npc.ai[2] += 1f;
                    if (npc.ai[2] == 0f)
                    {
                        npc.ai[2] = 1f;
                        npc.velocity.X = npc.direction * 2;
                    }
                    return;
                }
                npc.alpha = 0;
            }
            if (npc.type == NPCID.SwampThing)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(240))
                {
                    npc.ai[2] = Main.rand.Next(-480, -60);
                    npc.netUpdate = true;
                }
                if (npc.ai[2] < 0f)
                {
                    npc.TargetClosest();
                    if (npc.justHit)
                    {
                        npc.ai[2] = 0f;
                    }
                    if (Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    {
                        npc.ai[2] = 0f;
                    }
                }
                if (npc.ai[2] < 0f)
                {
                    npc.velocity.X *= 0.9f;
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    npc.ai[2] += 1f;
                    if (npc.ai[2] == 0f)
                    {
                        npc.velocity.X = (float)npc.direction * 0.1f;
                    }
                    return;
                }
            }
            if (npc.type == NPCID.CreatureFromTheDeep)
            {
                if (npc.wet)
                {
                    npc.knockBackResist = 0f;
                    npc.ai[3] = -0.10101f;
                    npc.noGravity = true;
                    Vector2 center = npc.Center;
                    npc.width = 34;
                    npc.height = 24;
                    npc.position.X = center.X - (float)(npc.width / 2);
                    npc.position.Y = center.Y - (float)(npc.height / 2);
                    npc.TargetClosest();
                    if (npc.collideX)
                    {
                        npc.velocity.X = 0f - npc.oldVelocity.X;
                    }
                    if (npc.velocity.X < 0f)
                    {
                        npc.direction = -1;
                    }
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].Center, 1, 1))
                    {
                        Vector2 vector = Main.player[npc.target].Center - npc.Center;
                        vector.Normalize();
                        vector *= 5f;
                        npc.velocity = (npc.velocity * 19f + vector) / 20f;
                        return;
                    }
                    float num158 = 5f;
                    if (npc.velocity.Y > 0f)
                    {
                        num158 = 3f;
                    }
                    if (npc.velocity.Y < 0f)
                    {
                        num158 = 8f;
                    }
                    Vector2 vector10 = new Vector2(npc.direction, -1f);
                    vector10.Normalize();
                    vector10 *= num158;
                    if (num158 < 5f)
                    {
                        npc.velocity = (npc.velocity * 24f + vector10) / 25f;
                    }
                    else
                    {
                        npc.velocity = (npc.velocity * 9f + vector10) / 10f;
                    }
                    return;
                }
                npc.knockBackResist = 0.4f * Main.GameModeInfo.KnockbackToEnemiesMultiplier;
                npc.noGravity = false;
                Vector2 center2 = npc.Center;
                npc.width = 18;
                npc.height = 40;
                npc.position.X = center2.X - (float)(npc.width / 2);
                npc.position.Y = center2.Y - (float)(npc.height / 2);
                if (npc.ai[3] == -0.10101f)
                {
                    npc.ai[3] = 0f;
                    float num169 = npc.velocity.Length();
                    num169 *= 2f;
                    if (num169 > 10f)
                    {
                        num169 = 10f;
                    }
                    npc.velocity.Normalize();
                    npc.velocity *= num169;
                    if (npc.velocity.X < 0f)
                    {
                        npc.direction = -1;
                    }
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    npc.spriteDirection = npc.direction;
                }
            }
            if (npc.type == NPCID.ZombieMerman)
            {
                if (npc.alpha == 255)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                    npc.velocity.Y = -6f;
                    npc.netUpdate = true;
                    for (int j = 0; j < 35; j++)
                    {
                        Dust dust6 = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood);
                        dust6.velocity *= 1f;
                        dust6.scale = 1f + Main.rand.NextFloat() * 0.5f;
                        dust6.fadeIn = 1.5f + Main.rand.NextFloat() * 0.5f;
                        dust6.velocity += npc.velocity * 0.5f;
                    }
                }
                npc.alpha -= 15;
                if (npc.alpha < 0)
                {
                    npc.alpha = 0;
                }
                npc.position += npc.netOffset;
                if (npc.alpha != 0)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Dust dust7 = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood);
                        dust7.velocity *= 1f;
                        dust7.scale = 1f + Main.rand.NextFloat() * 0.5f;
                        dust7.fadeIn = 1.5f + Main.rand.NextFloat() * 0.5f;
                        dust7.velocity += npc.velocity * 0.3f;
                    }
                }
                if (Main.rand.NextBool(3))
                {
                    Dust dust8 = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood);
                    dust8.velocity *= 0f;
                    dust8.alpha = 120;
                    dust8.scale = 0.7f + Main.rand.NextFloat() * 0.5f;
                    dust8.velocity += npc.velocity * 0.3f;
                }
                npc.position -= npc.netOffset;
                if (npc.wet)
                {
                    npc.knockBackResist = 0f;
                    npc.ai[3] = -0.10101f;
                    npc.noGravity = true;
                    Vector2 center3 = npc.Center;
                    npc.position.X = center3.X - (float)(npc.width / 2);
                    npc.position.Y = center3.Y - (float)(npc.height / 2);
                    npc.TargetClosest();
                    if (npc.collideX)
                    {
                        npc.velocity.X = 0f - npc.oldVelocity.X;
                    }
                    if (npc.velocity.X < 0f)
                    {
                        npc.direction = -1;
                    }
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].Center, 1, 1))
                    {
                        Vector2 vector21 = Main.player[npc.target].Center - npc.Center;
                        vector21.Normalize();
                        float num180 = 1f;
                        num180 += Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) / 40f;
                        num180 = MathHelper.Clamp(num180, 5f, 20f);
                        vector21 *= num180;
                        if (npc.velocity.Y > 0f)
                        {
                            npc.velocity = (npc.velocity * 29f + vector21) / 30f;
                        }
                        else
                        {
                            npc.velocity = (npc.velocity * 4f + vector21) / 5f;
                        }
                        return;
                    }
                    float num191 = 5f;
                    if (npc.velocity.Y > 0f)
                    {
                        num191 = 3f;
                    }
                    if (npc.velocity.Y < 0f)
                    {
                        num191 = 8f;
                    }
                    Vector2 vector31 = new Vector2(npc.direction, -1f);
                    vector31.Normalize();
                    vector31 *= num191;
                    if (num191 < 5f)
                    {
                        npc.velocity = (npc.velocity * 24f + vector31) / 25f;
                    }
                    else
                    {
                        npc.velocity = (npc.velocity * 9f + vector31) / 10f;
                    }
                    return;
                }
                npc.noGravity = false;
                Vector2 center4 = npc.Center;
                npc.position.X = center4.X - (float)(npc.width / 2);
                npc.position.Y = center4.Y - (float)(npc.height / 2);
                if (npc.ai[3] == -0.10101f)
                {
                    npc.ai[3] = 0f;
                    float num2 = npc.velocity.Length();
                    num2 *= 2f;
                    if (num2 > 15f)
                    {
                        num2 = 15f;
                    }
                    npc.velocity.Normalize();
                    npc.velocity *= num2;
                    if (npc.velocity.X < 0f)
                    {
                        npc.direction = -1;
                    }
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    npc.spriteDirection = npc.direction;
                }
            }
            if (npc.type == NPCID.CultistArcherBlue || npc.type == NPCID.CultistArcherWhite)
            {
                if (npc.ai[3] < 0f)
                {
                    npc.directionY = -1;
                    flag = false;
                    npc.damage = 0;
                    npc.velocity.X *= 0.93f;
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    int num13 = (int)(0f - npc.ai[3] - 1f);
                    int num24 = Math.Sign(Main.npc[num13].Center.X - npc.Center.X);
                    if (num24 != npc.direction)
                    {
                        npc.velocity.X = 0f;
                        npc.direction = num24;
                        npc.netUpdate = true;
                    }
                    if (npc.justHit && Main.netMode != NetmodeID.MultiplayerClient && Main.npc[num13].localAI[0] == 0f)
                    {
                        Main.npc[num13].localAI[0] = 1f;
                    }
                    if (npc.ai[0] < 1000f)
                    {
                        npc.ai[0] = 1000f;
                    }
                    if ((npc.ai[0] += 1f) >= 1300f)
                    {
                        npc.ai[0] = 1000f;
                        npc.netUpdate = true;
                    }
                    return;
                }
                if (npc.ai[0] >= 1000f)
                {
                    npc.ai[0] = 0f;
                }
                npc.damage = npc.defDamage;
            }
            if (npc.type == NPCID.MartianOfficer && npc.ai[2] == 0f && npc.localAI[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int num34 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, 384, npc.whoAmI);
                npc.ai[2] = num34 + 1;
                npc.localAI[0] = -1f;
                npc.netUpdate = true;
                Main.npc[num34].ai[0] = npc.whoAmI;
                Main.npc[num34].netUpdate = true;
            }
            if (npc.type == NPCID.MartianOfficer)
            {
                int num45 = (int)npc.ai[2] - 1;
                if (num45 != -1 && Main.npc[num45].active && Main.npc[num45].type == NPCID.ForceBubble)
                {
                    npc.dontTakeDamage = true;
                }
                else
                {
                    npc.dontTakeDamage = false;
                    npc.ai[2] = 0f;
                    if (npc.localAI[0] == -1f)
                    {
                        npc.localAI[0] = 180f;
                    }
                    if (npc.localAI[0] > 0f)
                    {
                        npc.localAI[0] -= 1f;
                    }
                }
            }
            if (npc.type == NPCID.GraniteGolem)
            {
                int num56 = 300;
                int num67 = 120;
                npc.dontTakeDamage = false;
                if (npc.ai[2] < 0f)
                {
                    npc.dontTakeDamage = true;
                    npc.ai[2] += 1f;
                    npc.velocity.X *= 0.9f;
                    if ((double)Math.Abs(npc.velocity.X) < 0.001)
                    {
                        npc.velocity.X = 0.001f * (float)npc.direction;
                    }
                    if (Math.Abs(npc.velocity.Y) > 1f)
                    {
                        npc.ai[2] += 10f;
                    }
                    if (npc.ai[2] >= 0f)
                    {
                        npc.netUpdate = true;
                        npc.velocity.X += (float)npc.direction * 0.3f;
                    }
                    return;
                }
                if (npc.ai[2] < (float)num56)
                {
                    if (npc.justHit)
                    {
                        npc.ai[2] += 15f;
                    }
                    npc.ai[2] += 1f;
                }
                else if (npc.velocity.Y == 0f)
                {
                    npc.ai[2] = -num67;
                    npc.netUpdate = true;
                }
            }
            if (npc.type == NPCID.RockGolem)
            {
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.TargetClosest(npc.ai[2] > 0f);
                }
                Player player = Main.player[npc.target];
                bool flag12 = !player.dead && player.active && npc.Center.Distance(player.Center) < 320f;
                int num78 = 100;
                int num89 = 32;
                if (npc.ai[2] == 0f)
                {
                    npc.ai[3] = 65f;
                    if (flag12 && Collision.CanHit(player, npc))
                    {
                        npc.ai[2] = num78;
                        npc.ai[3] = 0f;
                        npc.velocity.X = (float)npc.direction * 0.01f;
                        npc.netUpdate = true;
                    }
                }
                else
                {
                    if (npc.ai[2] < (float)num78)
                    {
                        npc.ai[2] += 1f;
                        npc.velocity.X *= 0.9f;
                        if ((double)Math.Abs(npc.velocity.X) < 0.001)
                        {
                            npc.velocity.X = 0f;
                        }
                        if (Math.Abs(npc.velocity.Y) > 1f)
                        {
                            npc.ai[2] = 0f;
                        }
                        if (npc.ai[2] == (float)(num78 - num89 / 2) && Main.netMode != NetmodeID.MultiplayerClient && !player.Hitbox.Intersects(npc.Hitbox) && Collision.CanHit(player, npc))
                        {
                            float num99 = 8f;
                            Vector2 center5 = npc.Center;
                            Vector2 vector32 = npc.DirectionTo(Main.player[npc.target].Center) * num99;
                            if (vector32.HasNaNs())
                            {
                                vector32 = new Vector2((float)npc.direction * num99, 0f);
                            }
                            int num111 = 20;
                            Vector2 v = vector32 + Utils.RandomVector2(Main.rand, -0.8f, 0.8f);
                            v = v.SafeNormalize(Vector2.Zero);
                            v *= num99;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), center5.X, center5.Y, v.X, v.Y, 909, num111, 1f, Main.myPlayer);
                        }
                        if (npc.ai[2] >= (float)num78)
                        {
                            npc.ai[2] = num78;
                            npc.ai[3] = 0f;
                            npc.velocity.X = (float)npc.direction * 0.01f;
                            npc.netUpdate = true;
                        }
                        return;
                    }
                    if (npc.velocity.Y == 0f && flag12 && (player.Hitbox.Intersects(npc.Hitbox) || Collision.CanHit(player, npc)))
                    {
                        npc.ai[2] = num78 - num89;
                        npc.netUpdate = true;
                    }
                }
            }
            if (npc.type == NPCID.Medusa)
            {
                int num118 = 180;
                int num119 = 300;
                int num120 = 180;
                int num121 = 60;
                int num122 = 20;
                if (npc.life < npc.lifeMax / 3)
                {
                    num118 = 120;
                    num119 = 240;
                    num120 = 240;
                    num121 = 90;
                }
                if (npc.ai[2] > 0f)
                {
                    npc.ai[2] -= 1f;
                }
                else if (npc.ai[2] == 0f)
                {
                    if (((Main.player[npc.target].Center.X < npc.Center.X && npc.direction < 0) || (Main.player[npc.target].Center.X > npc.Center.X && npc.direction > 0)) && npc.velocity.Y == 0f && npc.Distance(Main.player[npc.target].Center) < 900f && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    {
                        npc.ai[2] = -num120 - num122;
                        npc.netUpdate = true;
                    }
                }
                else
                {
                    if (npc.ai[2] < 0f && npc.ai[2] < (float)(-num120))
                    {
                        npc.position += npc.netOffset;
                        npc.velocity.X *= 0.9f;
                        if (npc.velocity.Y < -2f || npc.velocity.Y > 4f || npc.justHit)
                        {
                            npc.ai[2] = num118;
                        }
                        else
                        {
                            npc.ai[2] += 1f;
                            if (npc.ai[2] == 0f)
                            {
                                npc.ai[2] = num119;
                            }
                        }
                        float num123 = npc.ai[2] + (float)num120 + (float)num122;
                        if (num123 == 1f)
                        {
                            SoundEngine.PlaySound(SoundID.NPCDeath17, npc.position);
                        }
                        if (num123 < (float)num122)
                        {
                            Vector2 vector33 = npc.Top + new Vector2(npc.spriteDirection * 6, 6f);
                            float num124 = MathHelper.Lerp(20f, 30f, (num123 * 3f + 50f) / 182f);
                            Main.rand.NextFloat();
                            for (float num125 = 0f; num125 < 2f; num125 += 1f)
                            {
                                Vector2 vector34 = Vector2.UnitY.RotatedByRandom(6.2831854820251465) * (Main.rand.NextFloat() * 0.5f + 0.5f);
                                Dust obj = Main.dust[Dust.NewDust(vector33, 0, 0, DustID.GoldFlame)];
                                obj.position = vector33 + vector34 * num124;
                                obj.noGravity = true;
                                obj.velocity = vector34 * 2f;
                                obj.scale = 0.5f + Main.rand.NextFloat() * 0.5f;
                            }
                        }
                        Lighting.AddLight(npc.Center, 0.9f, 0.75f, 0.1f);
                        npc.position -= npc.netOffset;
                        return;
                    }
                    if (npc.ai[2] < 0f && npc.ai[2] >= (float)(-num120))
                    {
                        npc.position += npc.netOffset;
                        Lighting.AddLight(npc.Center, 0.9f, 0.75f, 0.1f);
                        npc.velocity.X *= 0.9f;
                        if (npc.velocity.Y < -2f || npc.velocity.Y > 4f || npc.justHit)
                        {
                            npc.ai[2] = num118;
                        }
                        else
                        {
                            npc.ai[2] += 1f;
                            if (npc.ai[2] == 0f)
                            {
                                npc.ai[2] = num119;
                            }
                        }
                        float num126 = npc.ai[2] + (float)num120;
                        if (num126 < 180f && (Main.rand.NextBool(3) || npc.ai[2] % 3f == 0f))
                        {
                            Vector2 vector35 = npc.Top + new Vector2(npc.spriteDirection * 10, 10f);
                            float num128 = MathHelper.Lerp(20f, 30f, (num126 * 3f + 50f) / 182f);
                            Main.rand.NextFloat();
                            for (float num129 = 0f; num129 < 1f; num129 += 1f)
                            {
                                Vector2 vector36 = Vector2.UnitY.RotatedByRandom(6.2831854820251465) * (Main.rand.NextFloat() * 0.5f + 0.5f);
                                Dust obj2 = Main.dust[Dust.NewDust(vector35, 0, 0, DustID.GoldFlame)];
                                obj2.position = vector35 + vector36 * num128;
                                obj2.noGravity = true;
                                obj2.velocity = vector36 * 4f;
                                obj2.scale = 0.5f + Main.rand.NextFloat();
                            }
                        }
                        npc.position -= npc.netOffset;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            return;
                        }
                        Player player2 = Main.player[Main.myPlayer];
                        _ = Main.myPlayer;
                        if (player2.dead || !player2.active || player2.FindBuffIndex(156) != -1)
                        {
                            return;
                        }
                        Vector2 vector2 = player2.Center - npc.Center;
                        if (!(vector2.Length() < 700f))
                        {
                            return;
                        }
                        bool flag21 = vector2.Length() < 30f;
                        if (!flag21)
                        {
                            float x = ((float)Math.PI / 4f).ToRotationVector2().X;
                            Vector2 vector3 = Vector2.Normalize(vector2);
                            if (vector3.X > x || vector3.X < 0f - x)
                            {
                                flag21 = true;
                            }
                        }
                        if (((player2.Center.X < npc.Center.X && npc.direction < 0 && player2.direction > 0) || (player2.Center.X > npc.Center.X && npc.direction > 0 && player2.direction < 0)) && flag21 && (Collision.CanHitLine(npc.Center, 1, 1, player2.Center, 1, 1) || Collision.CanHitLine(npc.Center - Vector2.UnitY * 16f, 1, 1, player2.Center, 1, 1) || Collision.CanHitLine(npc.Center + Vector2.UnitY * 8f, 1, 1, player2.Center, 1, 1)) && !player2.creativeGodMode)
                        {
                            player2.AddBuff(156, num121 + (int)npc.ai[2] * -1);
                        }
                        return;
                    }
                }
            }
            if (npc.type == NPCID.GoblinSummoner)
            {
                if (npc.ai[3] < 0f)
                {
                    npc.knockBackResist = 0f;
                    npc.defense = (int)((double)npc.defDefense * 1.1);
                    npc.noGravity = true;
                    npc.noTileCollide = true;
                    if (npc.velocity.X < 0f)
                    {
                        npc.direction = -1;
                    }
                    else if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                    npc.rotation = npc.velocity.X * 0.1f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.localAI[3] += 1f;
                        if (npc.localAI[3] > (float)Main.rand.Next(20, 180))
                        {
                            npc.localAI[3] = 0f;
                            Vector2 center6 = npc.Center;
                            center6 += npc.velocity;
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)center6.X, (int)center6.Y, 30);
                        }
                    }
                }
                else
                {
                    npc.localAI[3] = 0f;
                    npc.knockBackResist = 0.35f * Main.GameModeInfo.KnockbackToEnemiesMultiplier;
                    npc.rotation *= 0.9f;
                    npc.defense = npc.defDefense;
                    npc.noGravity = false;
                    npc.noTileCollide = false;
                }
                if (npc.ai[3] == 1f)
                {
                    npc.knockBackResist = 0f;
                    npc.defense += 10;
                }
                if (npc.ai[3] == -1f)
                {
                    npc.TargetClosest();
                    float num130 = 8f;
                    float num131 = 40f;
                    Vector2 vector4 = Main.player[npc.target].Center - npc.Center;
                    float num132 = vector4.Length();
                    num130 += num132 / 200f;
                    vector4.Normalize();
                    vector4 *= num130;
                    npc.velocity = (npc.velocity * (num131 - 1f) + vector4) / num131;
                    if (num132 < 500f && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        npc.ai[3] = 0f;
                        npc.ai[2] = 0f;
                    }
                    return;
                }
                if (npc.ai[3] == -2f)
                {
                    npc.velocity.Y -= 0.2f;
                    if (npc.velocity.Y < -10f)
                    {
                        npc.velocity.Y = -10f;
                    }
                    if (Main.player[npc.target].Center.Y - npc.Center.Y > 200f)
                    {
                        npc.TargetClosest();
                        npc.ai[3] = -3f;
                        if (Main.player[npc.target].Center.X > npc.Center.X)
                        {
                            npc.ai[2] = 1f;
                        }
                        else
                        {
                            npc.ai[2] = -1f;
                        }
                    }
                    npc.velocity.X *= 0.99f;
                    return;
                }
                if (npc.ai[3] == -3f)
                {
                    if (npc.direction == 0)
                    {
                        npc.TargetClosest();
                    }
                    if (npc.ai[2] == 0f)
                    {
                        npc.ai[2] = npc.direction;
                    }
                    npc.velocity.Y *= 0.9f;
                    npc.velocity.X += npc.ai[2] * 0.3f;
                    if (npc.velocity.X > 10f)
                    {
                        npc.velocity.X = 10f;
                    }
                    if (npc.velocity.X < -10f)
                    {
                        npc.velocity.X = -10f;
                    }
                    float num133 = Main.player[npc.target].Center.X - npc.Center.X;
                    if ((npc.ai[2] < 0f && num133 > 300f) || (npc.ai[2] > 0f && num133 < -300f))
                    {
                        npc.ai[3] = -4f;
                        npc.ai[2] = 0f;
                    }
                    else if (Math.Abs(num133) > 800f)
                    {
                        npc.ai[3] = -1f;
                        npc.ai[2] = 0f;
                    }
                    return;
                }
                if (npc.ai[3] == -4f)
                {
                    npc.ai[2] += 1f;
                    npc.velocity.Y += 0.1f;
                    if (npc.velocity.Length() > 4f)
                    {
                        npc.velocity *= 0.9f;
                    }
                    int num134 = (int)npc.Center.X / 16;
                    int num135 = (int)(npc.position.Y + (float)npc.height + 12f) / 16;
                    bool flag22 = false;
                    for (int l = num134 - 1; l <= num134 + 1; l++)
                    {
                        if (Main.tile[l, num135] == null)
                        {
                            Main.tile[num134, num135].CopyFrom(default);
                        }
                        if (Main.tile[l, num135].HasTile && Main.tileSolid[Main.tile[l, num135].TileType])
                        {
                            flag22 = true;
                        }
                    }
                    if (flag22 && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        npc.ai[3] = 0f;
                        npc.ai[2] = 0f;
                    }
                    else if (npc.ai[2] > 300f || npc.Center.Y > Main.player[npc.target].Center.Y + 200f)
                    {
                        npc.ai[3] = -1f;
                        npc.ai[2] = 0f;
                    }
                }
                else
                {
                    if (npc.ai[3] == 1f)
                    {
                        Vector2 center7 = npc.Center;
                        center7.Y -= 70f;
                        npc.velocity.X *= 0.8f;
                        npc.ai[2] += 1f;
                        if (npc.ai[2] == 60f)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)center7.X, (int)center7.Y + 18, 472);
                            }
                        }
                        else if (npc.ai[2] >= 90f)
                        {
                            npc.ai[3] = -2f;
                            npc.ai[2] = 0f;
                        }
                        for (int m = 0; m < 2; m++)
                        {
                            Vector2 vector37 = center7;
                            Vector2 vector5 = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                            vector5.Normalize();
                            vector5 *= (float)Main.rand.Next(0, 100) * 0.1f;
                            Vector2 vector38 = vector37 + vector5;
                            vector5.Normalize();
                            vector5 *= (float)Main.rand.Next(50, 90) * 0.1f;
                            int num136 = Dust.NewDust(vector38, 1, 1, DustID.Shadowflame);
                            Main.dust[num136].velocity = -vector5 * 0.3f;
                            Main.dust[num136].alpha = 100;
                            if (Main.rand.NextBool(2))
                            {
                                Main.dust[num136].noGravity = true;
                                Main.dust[num136].scale += 0.3f;
                            }
                        }
                        return;
                    }
                    npc.ai[2] += 1f;
                    int num137 = 10;
                    if (npc.velocity.Y == 0f && NPC.CountNPCS(472) < num137)
                    {
                        if (npc.ai[2] >= 180f)
                        {
                            npc.ai[2] = 0f;
                            npc.ai[3] = 1f;
                        }
                    }
                    else
                    {
                        if (NPC.CountNPCS(472) >= num137)
                        {
                            npc.ai[2] += 1f;
                        }
                        if (npc.ai[2] >= 360f)
                        {
                            npc.ai[2] = 0f;
                            npc.ai[3] = -2f;
                            npc.velocity.Y -= 3f;
                        }
                    }
                    if (npc.target >= 0 && !Main.player[npc.target].dead && (Main.player[npc.target].Center - npc.Center).Length() > 800f)
                    {
                        npc.ai[3] = -1f;
                        npc.ai[2] = 0f;
                    }
                }
                if (Main.player[npc.target].dead)
                {
                    npc.TargetClosest();
                    if (Main.player[npc.target].dead)
                    {
                        npc.EncourageDespawn(1);
                    }
                }
            }
            if (npc.type == NPCID.SolarSolenian)
            {
                npc.reflectsProjectiles = false;
                npc.takenDamageMultiplier = 1f;
                int num139 = 6;
                int num140 = 10;
                float num141 = 16f;
                if (npc.ai[2] > 0f)
                {
                    npc.ai[2] -= 1f;
                }
                if (npc.ai[2] == 0f)
                {
                    if (((Main.player[npc.target].Center.X < npc.Center.X && npc.direction < 0) || (Main.player[npc.target].Center.X > npc.Center.X && npc.direction > 0)) && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    {
                        npc.ai[2] = -1f;
                        npc.netUpdate = true;
                        npc.TargetClosest();
                    }
                }
                else
                {
                    if (npc.ai[2] < 0f && npc.ai[2] > (float)(-num139))
                    {
                        npc.ai[2] -= 1f;
                        npc.velocity.X *= 0.9f;
                        return;
                    }
                    if (npc.ai[2] == (float)(-num139))
                    {
                        npc.ai[2] -= 1f;
                        npc.TargetClosest();
                        Vector2 vector6 = npc.DirectionTo(Main.player[npc.target].Top + new Vector2(0f, -30f));
                        if (vector6.HasNaNs())
                        {
                            vector6 = Vector2.Normalize(new Vector2(npc.spriteDirection, -1f));
                        }
                        npc.velocity = vector6 * num141;
                        npc.netUpdate = true;
                        return;
                    }
                    if (npc.ai[2] < (float)(-num139))
                    {
                        npc.ai[2] -= 1f;
                        if (npc.velocity.Y == 0f)
                        {
                            npc.ai[2] = 60f;
                        }
                        else if (npc.ai[2] < (float)(-num139 - num140))
                        {
                            npc.velocity.Y += 0.15f;
                            if (npc.velocity.Y > 24f)
                            {
                                npc.velocity.Y = 24f;
                            }
                        }
                        npc.reflectsProjectiles = true;
                        npc.takenDamageMultiplier = 3f;
                        if (npc.justHit)
                        {
                            npc.ai[2] = 60f;
                            npc.netUpdate = true;
                        }
                        return;
                    }
                }
            }
            if (npc.type == NPCID.SolarDrakomire)
            {
                int num142 = 42;
                int num143 = 18;
                if (npc.justHit)
                {
                    npc.ai[2] = 120f;
                    npc.netUpdate = true;
                }
                if (npc.ai[2] > 0f)
                {
                    npc.ai[2] -= 1f;
                }
                if (npc.ai[2] == 0f)
                {
                    int num144 = 0;
                    for (int n = 0; n < 200; n++)
                    {
                        if (Main.npc[n].active && Main.npc[n].type == NPCID.SolarFlare)
                        {
                            num144++;
                        }
                    }
                    if (num144 > 6)
                    {
                        npc.ai[2] = 90f;
                    }
                    else if (((Main.player[npc.target].Center.X < npc.Center.X && npc.direction < 0) || (Main.player[npc.target].Center.X > npc.Center.X && npc.direction > 0)) && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    {
                        npc.ai[2] = -1f;
                        npc.netUpdate = true;
                        npc.TargetClosest();
                    }
                }
                else if (npc.ai[2] < 0f && npc.ai[2] > (float)(-num142))
                {
                    npc.ai[2] -= 1f;
                    if (npc.ai[2] == (float)(-num142))
                    {
                        npc.ai[2] = 180 + 30 * Main.rand.Next(10);
                    }
                    npc.velocity.X *= 0.8f;
                    if (npc.ai[2] == (float)(-num143) || npc.ai[2] == (float)(-num143 - 8) || npc.ai[2] == (float)(-num143 - 16))
                    {
                        npc.position += npc.netOffset;
                        for (int num145 = 0; num145 < 20; num145++)
                        {
                            Vector2 vector7 = npc.Center + Vector2.UnitX * npc.spriteDirection * 40f;
                            Dust obj3 = Main.dust[Dust.NewDust(vector7, 0, 0, DustID.SolarFlare)];
                            Vector2 vector8 = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                            obj3.position = vector7 + vector8 * 4f;
                            obj3.velocity = vector8 * 2f + Vector2.UnitX * Main.rand.NextFloat() * npc.spriteDirection * 3f;
                            obj3.scale = 0.3f + vector8.X * (float)(-npc.spriteDirection);
                            obj3.fadeIn = 0.7f;
                            obj3.noGravity = true;
                        }
                        npc.position -= npc.netOffset;
                        if (npc.velocity.X > -0.5f && npc.velocity.X < 0.5f)
                        {
                            npc.velocity.X = 0f;
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + npc.spriteDirection * 45, (int)npc.Center.Y + 8, 516, 0, 0f, 0f, 0f, 0f, npc.target);
                        }
                    }
                    return;
                }
            }
            if (npc.type == NPCID.VortexLarva)
            {
                npc.localAI[0] += 1f;
                if (npc.localAI[0] >= 300f)
                {
                    int num202 = (int)npc.Center.X / 16 - 1;
                    int num146 = (int)npc.Center.Y / 16 - 1;
                    if (!Collision.SolidTiles(num202, num202 + 2, num146, num146 + 1) && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Transform(427);
                        npc.life = npc.lifeMax;
                        npc.localAI[0] = 0f;
                        return;
                    }
                }
                int num147 = 0;
                num147 = ((npc.localAI[0] < 60f) ? 16 : ((npc.localAI[0] < 120f) ? 8 : ((npc.localAI[0] < 180f) ? 4 : ((npc.localAI[0] < 240f) ? 2 : ((!(npc.localAI[0] < 300f)) ? 1 : 1)))));
                if (Main.rand.NextBool(num147))
                {
                    npc.position += npc.netOffset;
                    Dust dust4 = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex)];
                    dust4.noGravity = true;
                    dust4.scale = 1f;
                    dust4.noLight = true;
                    dust4.velocity = npc.DirectionFrom(dust4.position) * dust4.velocity.Length();
                    dust4.position -= dust4.velocity * 5f;
                    dust4.position.X += npc.direction * 6;
                    dust4.position.Y += 4f;
                    npc.position -= npc.netOffset;
                }
            }
            if (npc.type == NPCID.VortexHornet)
            {
                npc.localAI[0] += 1f;
                npc.localAI[0] += Math.Abs(npc.velocity.X) / 2f;
                if (npc.localAI[0] >= 1200f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num203 = (int)npc.Center.X / 16 - 2;
                    int num149 = (int)npc.Center.Y / 16 - 3;
                    if (!Collision.SolidTiles(num203, num203 + 4, num149, num149 + 4))
                    {
                        npc.Transform(426);
                        npc.life = npc.lifeMax;
                        npc.localAI[0] = 0f;
                        return;
                    }
                }
                int num150 = 0;
                num150 = ((npc.localAI[0] < 360f) ? 32 : ((npc.localAI[0] < 720f) ? 16 : ((npc.localAI[0] < 1080f) ? 6 : ((npc.localAI[0] < 1440f) ? 2 : ((!(npc.localAI[0] < 1800f)) ? 1 : 1)))));
                if (Main.rand.NextBool(num150))
                {
                    npc.position += npc.netOffset;
                    Dust obj4 = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex)];
                    obj4.noGravity = true;
                    obj4.scale = 1f;
                    obj4.noLight = true;
                    npc.position -= npc.netOffset;
                }
            }
            if (npc.type == NPCID.TorchZombie)
            {
                npc.position += npc.netOffset;
                int num151 = (int)(npc.position.Y + 6f) / 16;
                if (npc.spriteDirection < 0)
                {
                    int num152 = (int)(npc.Center.X - 22f) / 16;
                    Tile tileSafely = Framing.GetTileSafely(num152, num151);
                    Tile tileSafely2 = Framing.GetTileSafely(num152 + 1, num151);
                    if (WorldGen.InWorld(num152, num151) && tileSafely2.LiquidAmount == 0 && tileSafely.LiquidAmount == 0)
                    {
                        Lighting.AddLight(num152, num151, 1f, 0.95f, 0.8f);
                        if (Main.rand.NextBool(30))
                        {
                            Dust.NewDust(new Vector2(npc.Center.X - 22f, npc.position.Y + 6f), 1, 1, DustID.Torch);
                        }
                    }
                }
                else
                {
                    int num153 = (int)(npc.Center.X + 14f) / 16;
                    Tile tileSafely3 = Framing.GetTileSafely(num153, num151);
                    Tile tileSafely4 = Framing.GetTileSafely(num153 - 1, num151);
                    if (WorldGen.InWorld(num153, num151) && tileSafely4.LiquidAmount == 0 && tileSafely3.LiquidAmount == 0)
                    {
                        Lighting.AddLight(num153, num151, 1f, 0.95f, 0.8f);
                        if (Main.rand.NextBool(30))
                        {
                            Dust.NewDust(new Vector2(npc.Center.X + 14f, npc.position.Y + 6f), 1, 1, DustID.Torch);
                        }
                    }
                }
                npc.position -= npc.netOffset;
            }
            else if (npc.type == NPCID.ArmedTorchZombie)
            {
                npc.position += npc.netOffset;
                if (!npc.wet)
                {
                    if (npc.spriteDirection < 0)
                    {
                        Lighting.AddLight(new Vector2(npc.Center.X - 36f, npc.position.Y + 24f), 1f, 0.95f, 0.8f);
                        if (npc.ai[2] == 0f && Main.rand.NextBool(30))
                        {
                            Dust.NewDust(new Vector2(npc.Center.X - 36f, npc.position.Y + 24f), 1, 1, DustID.Torch);
                        }
                    }
                    else
                    {
                        Lighting.AddLight(new Vector2(npc.Center.X + 28f, npc.position.Y + 24f), 1f, 0.95f, 0.8f);
                        if (npc.ai[2] == 0f && Main.rand.NextBool(30))
                        {
                            Dust.NewDust(new Vector2(npc.Center.X + 28f, npc.position.Y + 24f), 1, 1, DustID.Torch);
                        }
                    }
                }
                npc.position -= npc.netOffset;
            }
            bool flag23 = false;
            bool flag24 = false;
            if (npc.velocity.X == 0f)
            {
                flag24 = true;
            }
            if (npc.justHit)
            {
                flag24 = false;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.Lihzahrd && (double)npc.life <= (double)npc.lifeMax * 0.55)
            {
                npc.Transform(199);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.Nutcracker && (double)npc.life <= (double)npc.lifeMax * 0.55)
            {
                npc.Transform(349);
            }
            int num154 = 60;
            if (npc.type == NPCID.ChaosElemental)
            {
                num154 = 180;
                if (npc.ai[3] == -120f)
                {
                    npc.velocity *= 0f;
                    npc.ai[3] = 0f;
                    npc.position += npc.netOffset;
                    SoundEngine.PlaySound(in SoundID.Item8, npc.position);
                    Vector2 vector9 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num155 = npc.oldPos[2].X + (float)npc.width * 0.5f - vector9.X;
                    float num156 = npc.oldPos[2].Y + (float)npc.height * 0.5f - vector9.Y;
                    float num157 = (float)Math.Sqrt(num155 * num155 + num156 * num156);
                    num157 = 2f / num157;
                    num155 *= num157;
                    num156 *= num157;
                    for (int num159 = 0; num159 < 20; num159++)
                    {
                        int num160 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.UndergroundHallowedEnemies, num155, num156, 200, default(Color), 2f);
                        Main.dust[num160].noGravity = true;
                        Main.dust[num160].velocity.X *= 2f;
                    }
                    for (int num161 = 0; num161 < 20; num161++)
                    {
                        int num162 = Dust.NewDust(npc.oldPos[2], npc.width, npc.height, DustID.UndergroundHallowedEnemies, 0f - num155, 0f - num156, 200, default(Color), 2f);
                        Main.dust[num162].noGravity = true;
                        Main.dust[num162].velocity.X *= 2f;
                    }
                    npc.position -= npc.netOffset;
                }
            }
            bool flag25 = false;
            bool flag26 = true;
            if (npc.type == NPCID.Yeti || npc.type == NPCID.CorruptBunny || npc.type == NPCID.Crab || npc.type == NPCID.Clown || npc.type == NPCID.SkeletonArcher || npc.type == NPCID.GoblinArcher || npc.type == NPCID.ChaosElemental || npc.type == NPCID.BlackRecluse || npc.type == NPCID.WallCreeper || npc.type == NPCID.BloodCrawler || npc.type == NPCID.CorruptPenguin || npc.type == NPCID.LihzahrdCrawler || npc.type == NPCID.IcyMerman || npc.type == NPCID.PirateDeadeye || npc.type == NPCID.PirateCrossbower || npc.type == NPCID.PirateCaptain || npc.type == NPCID.CochinealBeetle || npc.type == NPCID.CyanBeetle || npc.type == NPCID.LacBeetle || npc.type == NPCID.SeaSnail || npc.type == NPCID.FlyingSnake || npc.type == NPCID.IceGolem || npc.type == NPCID.Eyezor || npc.type == NPCID.AnomuraFungus || npc.type == NPCID.MushiLadybug || npc.type == NPCID.Paladin || npc.type == NPCID.SkeletonSniper || npc.type == NPCID.TacticalSkeleton || npc.type == NPCID.SkeletonCommando || npc.type == NPCID.Scarecrow1 || npc.type == NPCID.Scarecrow2 || npc.type == NPCID.Scarecrow3 || npc.type == NPCID.Scarecrow4 || npc.type == NPCID.Scarecrow5 || npc.type == NPCID.Nutcracker || npc.type == NPCID.NutcrackerSpinning || npc.type == NPCID.ElfArcher || npc.type == NPCID.Krampus || npc.type == NPCID.CultistArcherBlue || (npc.type >= NPCID.ArmedZombie && npc.type <= NPCID.ArmedZombieCenx) || npc.type == NPCID.ArmedTorchZombie || npc.type == NPCID.CultistArcherWhite || npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner || npc.type == NPCID.MartianOfficer || npc.type == NPCID.MartianEngineer || npc.type == NPCID.Scutlix || (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) || npc.type == NPCID.Psycho || npc.type == NPCID.CrimsonBunny || npc.type == NPCID.SwampThing || npc.type == NPCID.ThePossessed || npc.type == NPCID.DrManFly || npc.type == NPCID.GoblinSummoner || npc.type == NPCID.CrimsonPenguin || npc.type == NPCID.Medusa || npc.type == NPCID.GreekSkeleton || npc.type == NPCID.GraniteGolem || npc.type == NPCID.StardustSoldier || npc.type == NPCID.NebulaSoldier || npc.type == NPCID.StardustSpiderBig || (npc.type >= NPCID.Crawdad && npc.type <= NPCID.Salamander9) || npc.type == NPCID.VortexRifleman || npc.type == NPCID.VortexHornet || npc.type == NPCID.VortexHornetQueen || npc.type == NPCID.VortexLarva || npc.type == NPCID.WalkingAntlion || npc.type == NPCID.GiantWalkingAntlion || npc.type == NPCID.SolarDrakomire || npc.type == NPCID.SolarSolenian || npc.type == NPCID.MartianWalker || (npc.type >= NPCID.DesertGhoul && npc.type <= NPCID.DesertGhoulHallow) || npc.type == NPCID.DesertLamiaLight || npc.type == NPCID.DesertLamiaDark || npc.type == NPCID.DesertScorpionWalk || npc.type == NPCID.DesertBeast || npc.type == NPCID.LarvaeAntlion || npc.type == NPCID.Gnome || npc.type == NPCID.RockGolem)
            {
                flag26 = false;
            }
            bool flag27 = false;
            int num163 = npc.type;
            if (num163 == 425 || num163 == 471)
            {
                flag27 = true;
            }
            bool flag2 = true;
            switch (npc.type)
            {
                case 110:
                case 111:
                case 206:
                case 214:
                case 215:
                case 216:
                case 291:
                case 292:
                case 293:
                case 350:
                case 379:
                case 380:
                case 381:
                case 382:
                case 409:
                case 411:
                case 424:
                case 426:
                case 466:
                case 498:
                case 499:
                case 500:
                case 501:
                case 502:
                case 503:
                case 504:
                case 505:
                case 506:
                case 520:
                    if (npc.ai[2] > 0f)
                    {
                        flag2 = false;
                    }
                    break;
            }
            if (!flag27 && flag2)
            {
                if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
                {
                    flag25 = true;
                }
                if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)num154 || flag25)
                {
                    npc.ai[3] += 1f;
                }
                else if ((double)Math.Abs(npc.velocity.X) > 0.9 && npc.ai[3] > 0f)
                {
                    npc.ai[3] -= 1f;
                }
                if (npc.ai[3] > (float)(num154 * 10))
                {
                    npc.ai[3] = 0f;
                }
                if (npc.justHit)
                {
                    npc.ai[3] = 0f;
                }
                if (npc.ai[3] == (float)num154)
                {
                    npc.netUpdate = true;
                }
                if (Main.player[npc.target].Hitbox.Intersects(npc.Hitbox))
                {
                    npc.ai[3] = 0f;
                }
            }
            if (npc.type == NPCID.Nailhead && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.localAI[3] > 0f)
                {
                    npc.localAI[3] -= 1f;
                }
                if (npc.justHit && npc.localAI[3] <= 0f && Main.rand.NextBool(3))
                {
                    npc.localAI[3] = 30f;
                    int num164 = Main.rand.Next(3, 6);
                    int[] array = new int[num164];
                    int num165 = 0;
                    for (int num166 = 0; num166 < 255; num166++)
                    {
                        if (Main.player[num166].active && !Main.player[num166].dead && Collision.CanHitLine(npc.position, npc.width, npc.height, Main.player[num166].position, Main.player[num166].width, Main.player[num166].height))
                        {
                            array[num165] = num166;
                            num165++;
                            if (num165 == num164)
                            {
                                break;
                            }
                        }
                    }
                    if (num165 > 1)
                    {
                        for (int num167 = 0; num167 < 100; num167++)
                        {
                            int num168 = Main.rand.Next(num165);
                            int num170;
                            for (num170 = num168; num170 == num168; num170 = Main.rand.Next(num165))
                            {
                            }
                            int num171 = array[num168];
                            array[num168] = array[num170];
                            array[num170] = num171;
                        }
                    }
                    Vector2 vector11 = new Vector2(-1f, -1f);
                    for (int num172 = 0; num172 < num165; num172++)
                    {
                        Vector2 vector12 = Main.npc[array[num172]].Center - npc.Center;
                        vector12.Normalize();
                        vector11 += vector12;
                    }
                    vector11.Normalize();
                    for (int num173 = 0; num173 < num164; num173++)
                    {
                        float num174 = Main.rand.Next(8, 13);
                        Vector2 vector13 = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                        vector13.Normalize();
                        if (num165 > 0)
                        {
                            vector13 += vector11;
                            vector13.Normalize();
                        }
                        vector13 *= num174;
                        if (num165 > 0)
                        {
                            num165--;
                            vector13 = Main.player[array[num165]].Center - npc.Center;
                            vector13.Normalize();
                            vector13 *= num174;
                        }
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.position.Y + (float)(npc.width / 4), vector13.X, vector13.Y, 498, (int)((double)npc.damage * 0.15), 1f, Main.myPlayer);
                    }
                }
            }
            if (npc.type == NPCID.Butcher)
            {
                if (npc.velocity.Y < 0f - npc.gravity || npc.velocity.Y > npc.gravity)
                {
                    npc.knockBackResist = 0f;
                }
                else
                {
                    npc.knockBackResist = 0.25f * Main.GameModeInfo.KnockbackToEnemiesMultiplier;
                }
            }
            if (npc.type == NPCID.ThePossessed)
            {
                npc.knockBackResist = 0.45f * Main.GameModeInfo.KnockbackToEnemiesMultiplier;
                if (npc.ai[2] == 1f)
                {
                    npc.knockBackResist = 0f;
                }
                bool flag3 = false;
                int num175 = (int)npc.Center.X / 16;
                int num176 = (int)npc.Center.Y / 16;
                for (int num177 = num175 - 1; num177 <= num175 + 1; num177++)
                {
                    for (int num178 = num176 - 1; num178 <= num176 + 1; num178++)
                    {
                        if (Main.tile[num177, num178] != null && Main.tile[num177, num178].WallType > 0)
                        {
                            flag3 = true;
                            break;
                        }
                    }
                    if (flag3)
                    {
                        break;
                    }
                }
                if (npc.ai[2] == 0f && flag3)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        flag = true;
                        npc.velocity.Y = -4.6f;
                        npc.velocity.X *= 1.3f;
                    }
                    else if (npc.velocity.Y > 0f && !Main.player[npc.target].dead)
                    {
                        npc.ai[2] = 1f;
                    }
                }
                if (flag3 && npc.ai[2] == 1f && !Main.player[npc.target].dead && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                {
                    Vector2 vector14 = Main.player[npc.target].Center - npc.Center;
                    float num179 = vector14.Length();
                    vector14.Normalize();
                    vector14 *= 4.5f + num179 / 300f;
                    npc.velocity = (npc.velocity * 29f + vector14) / 30f;
                    npc.noGravity = true;
                    npc.ai[2] = 1f;
                    return;
                }
                npc.noGravity = false;
                npc.ai[2] = 0f;
            }
            if (npc.type == NPCID.Fritz && npc.velocity.Y == 0f && (Main.player[npc.target].Center - npc.Center).Length() < 150f && Math.Abs(npc.velocity.X) > 3f && ((npc.velocity.X < 0f && npc.Center.X > Main.player[npc.target].Center.X) || (npc.velocity.X > 0f && npc.Center.X < Main.player[npc.target].Center.X)))
            {
                flag = true;
                npc.velocity.X *= 1.75f;
                npc.velocity.Y -= 4.5f;
                if (npc.Center.Y - Main.player[npc.target].Center.Y > 20f)
                {
                    npc.velocity.Y -= 0.5f;
                }
                if (npc.Center.Y - Main.player[npc.target].Center.Y > 40f)
                {
                    npc.velocity.Y -= 1f;
                }
                if (npc.Center.Y - Main.player[npc.target].Center.Y > 80f)
                {
                    npc.velocity.Y -= 1.5f;
                }
                if (npc.Center.Y - Main.player[npc.target].Center.Y > 100f)
                {
                    npc.velocity.Y -= 1.5f;
                }
                if (Math.Abs(npc.velocity.X) > 7f)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X = -7f;
                    }
                    else
                    {
                        npc.velocity.X = 7f;
                    }
                }
            }
            if (npc.type == NPCID.Gnome && npc.target < 255)
            {
                if (!Main.remixWorld && !Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    npc.ai[3] = num154;
                    npc.directionY = -1;
                    if (npc.type == NPCID.Gnome && !AI_003_Gnomes_ShouldTurnToStone() && (npc.Center - Main.player[npc.target].Center).Length() > 500f)
                    {
                        npc.velocity.X *= 0.95f;
                        if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                        {
                            npc.velocity.X = 0f;
                        }
                        return;
                    }
                }
                else if (Main.player[npc.target].Center.Y > npc.Center.Y - 128f)
                {
                    npc.ai[3] = 0f;
                }
            }
            if (npc.ai[3] < (float)num154 && NPC.DespawnEncouragement_AIStyle3_Fighters_NotDiscouraged(npc.type, npc.position, npc))
            {
                if (npc.shimmerTransparency < 1f)
                {
                    if ((npc.type == NPCID.Zombie || npc.type == NPCID.ArmedTorchZombie || npc.type == NPCID.TorchZombie || npc.type == NPCID.ZombieXmas || npc.type == NPCID.ZombieSweater || npc.type == NPCID.Skeleton || (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) || npc.type == NPCID.AngryBones || npc.type == NPCID.AngryBonesBig || npc.type == NPCID.AngryBonesBigMuscle || npc.type == NPCID.AngryBonesBigHelmet || npc.type == NPCID.ArmoredSkeleton || npc.type == NPCID.SkeletonArcher || npc.type == NPCID.BaldZombie || npc.type == NPCID.UndeadViking || npc.type == NPCID.ZombieEskimo || npc.type == NPCID.Frankenstein || npc.type == NPCID.PincushionZombie || npc.type == NPCID.SlimedZombie || npc.type == NPCID.SwampZombie || npc.type == NPCID.TwiggyZombie || npc.type == NPCID.ArmoredViking || npc.type == NPCID.FemaleZombie || npc.type == NPCID.HeadacheSkeleton || npc.type == NPCID.MisassembledSkeleton || npc.type == NPCID.PantlessSkeleton || npc.type == NPCID.ZombieRaincoat || npc.type == NPCID.SkeletonSniper || npc.type == NPCID.TacticalSkeleton || npc.type == NPCID.SkeletonCommando || npc.type == NPCID.ZombieSuperman || npc.type == NPCID.ZombiePixie || npc.type == NPCID.ZombieDoctor || npc.type == NPCID.GreekSkeleton || npc.type == NPCID.MaggotZombie || npc.type == NPCID.SporeSkeleton) && Main.rand.NextBool(1000))
                    {
                        SoundEngine.PlaySound(SoundID.ZombieMoan, npc.position);
                    }
                    if ((npc.type == NPCID.BloodZombie || npc.type == NPCID.ZombieMerman) && Main.rand.NextBool(800))
                    {
                        SoundEngine.PlaySound(SoundID.BloodZombie, npc.position);
                    }
                    if ((npc.type == NPCID.Mummy || npc.type == NPCID.DarkMummy || npc.type == NPCID.LightMummy || npc.type == NPCID.BloodMummy) && Main.rand.NextBool(500))
                    {
                        SoundEngine.PlaySound(SoundID.Mummy, npc.position);
                    }
                    if (npc.type == NPCID.Vampire && Main.rand.NextBool(500))
                    {
                        SoundEngine.PlaySound(SoundID.Zombie7);
                    }
                    if (npc.type == NPCID.Frankenstein && Main.rand.NextBool(500))
                    {
                        SoundEngine.PlaySound(SoundID.Zombie6, npc.position);
                    }
                    if (npc.type == NPCID.FaceMonster && Main.rand.NextBool(500))
                    {
                        SoundEngine.PlaySound(SoundID.Zombie8, npc.position);
                    }
                    if (npc.type >= NPCID.RustyArmoredBonesAxe && npc.type <= NPCID.HellArmoredBonesSword && Main.rand.NextBool(1000))
                    {
                        SoundEngine.PlaySound(SoundID.ZombieMoan, npc.position);
                    }
                }
                npc.TargetClosest();
                if (npc.directionY > 0 && Main.player[npc.target].Center.Y <= npc.Bottom.Y)
                {
                    npc.directionY = -1;
                }
            }
            else if (!(npc.ai[2] > 0f) || !NPC.DespawnEncouragement_AIStyle3_Fighters_CanBeBusyWithAction(npc.type))
            {
                if (Main.IsItDay() && (double)(npc.position.Y / 16f) < Main.worldSurface && npc.type != NPCID.Gnome && npc.type != NPCID.RockGolem)
                {
                    npc.EncourageDespawn(10);
                }
                if (npc.velocity.X == 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.ai[0] += 1f;
                        if (npc.ai[0] >= 2f)
                        {
                            npc.direction *= -1;
                            npc.spriteDirection = npc.direction;
                            npc.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    npc.ai[0] = 0f;
                }
                if (npc.direction == 0)
                {
                    npc.direction = 1;
                }
            }
            if (npc.type == NPCID.Vampire || npc.type == NPCID.NutcrackerSpinning)
            {
                if (npc.type == NPCID.Vampire && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
                {
                    npc.velocity.X *= 0.95f;
                }
                if (npc.velocity.X < -6f || npc.velocity.X > 6f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 6f && npc.direction == 1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.99f;
                    }
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > 6f)
                    {
                        npc.velocity.X = 6f;
                    }
                }
                else if (npc.velocity.X > -6f && npc.direction == -1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.99f;
                    }
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < -6f)
                    {
                        npc.velocity.X = -6f;
                    }
                }
            }
            else if (npc.type == NPCID.LihzahrdCrawler)
            {
                if (npc.velocity.X < -4f || npc.velocity.X > 4f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 4f && npc.direction == 1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.8f;
                    }
                    npc.velocity.X += 0.1f;
                    if (npc.velocity.X > 4f)
                    {
                        npc.velocity.X = 4f;
                    }
                }
                else if (npc.velocity.X > -4f && npc.direction == -1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.8f;
                    }
                    npc.velocity.X -= 0.1f;
                    if (npc.velocity.X < -4f)
                    {
                        npc.velocity.X = -4f;
                    }
                }
            }
            else if (npc.type == NPCID.ChaosElemental || npc.type == NPCID.SwampThing || npc.type == NPCID.PirateCorsair || npc.type == NPCID.MushiLadybug || npc.type == NPCID.DesertLamiaLight || npc.type == NPCID.DesertLamiaDark)
            {
                if (npc.velocity.X < -3f || npc.velocity.X > 3f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 3f && npc.direction == 1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.99f;
                    }
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > 3f)
                    {
                        npc.velocity.X = 3f;
                    }
                }
                else if (npc.velocity.X > -3f && npc.direction == -1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.99f;
                    }
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < -3f)
                    {
                        npc.velocity.X = -3f;
                    }
                }
            }
            else if (npc.type == NPCID.CreatureFromTheDeep || npc.type == NPCID.GoblinThief || npc.type == NPCID.ArmoredSkeleton || npc.type == NPCID.Werewolf || npc.type == NPCID.BlackRecluse || npc.type == NPCID.Frankenstein || npc.type == NPCID.Nymph || npc.type == NPCID.ArmoredViking || npc.type == NPCID.PirateDeckhand || npc.type == NPCID.AnomuraFungus || npc.type == NPCID.Splinterling || npc.type == NPCID.Yeti || npc.type == NPCID.Nutcracker || npc.type == NPCID.Krampus || (npc.type >= NPCID.DesertGhoul && npc.type <= NPCID.DesertGhoulHallow) || npc.type == NPCID.DesertScorpionWalk || npc.type == NPCID.JungleCreeper)
            {
                if (npc.velocity.X < -2f || npc.velocity.X > 2f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 2f && npc.direction == 1)
                {
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > 2f)
                    {
                        npc.velocity.X = 2f;
                    }
                }
                else if (npc.velocity.X > -2f && npc.direction == -1)
                {
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < -2f)
                    {
                        npc.velocity.X = -2f;
                    }
                }
            }
            else if (npc.type == NPCID.Clown)
            {
                if (npc.velocity.X < -2f || npc.velocity.X > 2f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 2f && npc.direction == 1)
                {
                    npc.velocity.X += 0.04f;
                    if (npc.velocity.X > 2f)
                    {
                        npc.velocity.X = 2f;
                    }
                }
                else if (npc.velocity.X > -2f && npc.direction == -1)
                {
                    npc.velocity.X -= 0.04f;
                    if (npc.velocity.X < -2f)
                    {
                        npc.velocity.X = -2f;
                    }
                }
            }
            else if (npc.type == NPCID.Skeleton || npc.type == NPCID.GoblinPeon || npc.type == NPCID.AngryBones || npc.type == NPCID.AngryBonesBig || npc.type == NPCID.AngryBonesBigMuscle || npc.type == NPCID.AngryBonesBigHelmet || npc.type == NPCID.CorruptBunny || npc.type == NPCID.GoblinScout || npc.type == NPCID.PossessedArmor || npc.type == NPCID.WallCreeper || npc.type == NPCID.BloodCrawler || npc.type == NPCID.UndeadViking || npc.type == NPCID.CorruptPenguin || npc.type == NPCID.SnowFlinx || npc.type == NPCID.Lihzahrd || npc.type == NPCID.HeadacheSkeleton || npc.type == NPCID.MisassembledSkeleton || npc.type == NPCID.PantlessSkeleton || npc.type == NPCID.CochinealBeetle || npc.type == NPCID.CyanBeetle || npc.type == NPCID.LacBeetle || npc.type == NPCID.FlyingSnake || npc.type == NPCID.FaceMonster || npc.type == NPCID.ZombieMushroom || npc.type == NPCID.ZombieElf || npc.type == NPCID.ZombieElfBeard || npc.type == NPCID.ZombieElfGirl || npc.type == NPCID.GingerbreadMan || npc.type == NPCID.GrayGrunt || npc.type == NPCID.GigaZapper || npc.type == NPCID.Fritz || npc.type == NPCID.Nailhead || npc.type == NPCID.Psycho || npc.type == NPCID.CrimsonBunny || npc.type == NPCID.ThePossessed || npc.type == NPCID.CrimsonPenguin || npc.type == NPCID.Medusa || npc.type == NPCID.GraniteGolem || npc.type == NPCID.VortexRifleman || npc.type == NPCID.VortexSoldier || npc.type == NPCID.ZombieMerman || npc.type == NPCID.RockGolem || npc.type == NPCID.SporeSkeleton)
            {
                float num181 = 1.5f;
                if (npc.type == NPCID.FaceMonster && Main.remixWorld)
                {
                    num181 = 3.75f;
                }
                else if (npc.type == NPCID.AngryBonesBig)
                {
                    num181 = 2f;
                }
                else if (npc.type == NPCID.AngryBonesBigMuscle)
                {
                    num181 = 1.75f;
                }
                else if (npc.type == NPCID.AngryBonesBigHelmet)
                {
                    num181 = 1.25f;
                }
                else if (npc.type == NPCID.HeadacheSkeleton)
                {
                    num181 = 1.1f;
                }
                else if (npc.type == NPCID.MisassembledSkeleton)
                {
                    num181 = 0.9f;
                }
                else if (npc.type == NPCID.PantlessSkeleton)
                {
                    num181 = 1.2f;
                }
                else if (npc.type == NPCID.ZombieElf)
                {
                    num181 = 1.75f;
                }
                else if (npc.type == NPCID.ZombieElfBeard)
                {
                    num181 = 1.25f;
                }
                else if (npc.type == NPCID.ZombieElfGirl)
                {
                    num181 = 2f;
                }
                else if (npc.type == NPCID.GrayGrunt)
                {
                    num181 = 1.8f;
                }
                else if (npc.type == NPCID.GigaZapper)
                {
                    num181 = 2.25f;
                }
                else if (npc.type == NPCID.Fritz)
                {
                    num181 = 4f;
                }
                else if (npc.type == NPCID.Nailhead)
                {
                    num181 = 0.75f;
                }
                else if (npc.type == NPCID.Psycho)
                {
                    num181 = 3.75f;
                }
                else if (npc.type == NPCID.ThePossessed)
                {
                    num181 = 3.25f;
                }
                else if (npc.type == NPCID.Medusa)
                {
                    num181 = 1.5f + (1f - (float)npc.life / (float)npc.lifeMax) * 2f;
                }
                else if (npc.type == NPCID.VortexRifleman)
                {
                    num181 = 6f;
                }
                else if (npc.type == NPCID.VortexSoldier)
                {
                    num181 = 4f;
                }
                else if (npc.type == NPCID.RockGolem)
                {
                    num181 = 0.9f;
                }
                else if (npc.type == NPCID.ZombieMerman)
                {
                    num181 = 1.5f + (1f - (float)npc.life / (float)npc.lifeMax) * 3.5f;
                }
                if (npc.type == NPCID.Skeleton || npc.type == NPCID.HeadacheSkeleton || npc.type == NPCID.MisassembledSkeleton || npc.type == NPCID.PantlessSkeleton || npc.type == NPCID.GingerbreadMan || npc.type == NPCID.SporeSkeleton)
                {
                    num181 *= 1f + (1f - npc.scale);
                    // --------------------------------------------------------------- HERE
                    num181 = Math.Max(num181, 0.75f);
                    // --------------------------------------------------------------- HERE
                }
                if (npc.velocity.X < 0f - num181 || npc.velocity.X > num181)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num181 && npc.direction == 1)
                {
                    if (npc.type == NPCID.Psycho && npc.velocity.X < -2f)
                    {
                        npc.velocity.X *= 0.9f;
                    }
                    if (npc.type == NPCID.ZombieMerman && npc.velocity.Y == 0f && npc.velocity.X < -1f)
                    {
                        npc.velocity.X *= 0.9f;
                    }
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > num181)
                    {
                        npc.velocity.X = num181;
                    }
                }
                else if (npc.velocity.X > 0f - num181 && npc.direction == -1)
                {
                    if (npc.type == NPCID.Psycho && npc.velocity.X > 2f)
                    {
                        npc.velocity.X *= 0.9f;
                    }
                    if (npc.type == NPCID.ZombieMerman && npc.velocity.Y == 0f && npc.velocity.X > 1f)
                    {
                        npc.velocity.X *= 0.9f;
                    }
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < 0f - num181)
                    {
                        npc.velocity.X = 0f - num181;
                    }
                }
                if (npc.velocity.Y == 0f && npc.type == NPCID.Fritz && ((npc.direction > 0 && npc.velocity.X < 0f) || (npc.direction < 0 && npc.velocity.X > 0f)))
                {
                    npc.velocity.X *= 0.9f;
                }
            }
            else if (npc.type >= NPCID.RustyArmoredBonesAxe && npc.type <= NPCID.HellArmoredBonesSword)
            {
                float num182 = 1.5f;
                if (npc.type == NPCID.RustyArmoredBonesAxe)
                {
                    num182 = 2f;
                }
                if (npc.type == NPCID.RustyArmoredBonesFlail)
                {
                    num182 = 1f;
                }
                if (npc.type == NPCID.RustyArmoredBonesSword)
                {
                    num182 = 1.5f;
                }
                if (npc.type == NPCID.RustyArmoredBonesSwordNoArmor)
                {
                    num182 = 3f;
                }
                if (npc.type == NPCID.BlueArmoredBones)
                {
                    num182 = 1.25f;
                }
                if (npc.type == NPCID.BlueArmoredBonesMace)
                {
                    num182 = 3f;
                }
                if (npc.type == NPCID.BlueArmoredBonesNoPants)
                {
                    num182 = 3.25f;
                }
                if (npc.type == NPCID.BlueArmoredBonesSword)
                {
                    num182 = 2f;
                }
                if (npc.type == NPCID.HellArmoredBones)
                {
                    num182 = 2.75f;
                }
                if (npc.type == NPCID.HellArmoredBonesSpikeShield)
                {
                    num182 = 1.8f;
                }
                if (npc.type == NPCID.HellArmoredBonesMace)
                {
                    num182 = 1.3f;
                }
                if (npc.type == NPCID.HellArmoredBonesSword)
                {
                    num182 = 2.5f;
                }
                num182 *= 1f + (1f - npc.scale);
                // --------------------------------------------------------------- HERE
                num182 = Math.Max(num182, 0.75f);
                // --------------------------------------------------------------- HERE
                if (npc.velocity.X < 0f - num182 || npc.velocity.X > num182)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num182 && npc.direction == 1)
                {
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > num182)
                    {
                        npc.velocity.X = num182;
                    }
                }
                else if (npc.velocity.X > 0f - num182 && npc.direction == -1)
                {
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < 0f - num182)
                    {
                        npc.velocity.X = 0f - num182;
                    }
                }
            }
            else if (npc.type >= NPCID.Scarecrow1 && npc.type <= NPCID.Scarecrow10)
            {
                float num183 = 1.5f;
                if (npc.type == NPCID.Scarecrow1 || npc.type == NPCID.Scarecrow6)
                {
                    num183 = 2f;
                }
                if (npc.type == NPCID.Scarecrow2 || npc.type == NPCID.Scarecrow7)
                {
                    num183 = 1.25f;
                }
                if (npc.type == NPCID.Scarecrow3 || npc.type == NPCID.Scarecrow8)
                {
                    num183 = 2.25f;
                }
                if (npc.type == NPCID.Scarecrow4 || npc.type == NPCID.Scarecrow9)
                {
                    num183 = 1.5f;
                }
                if (npc.type == NPCID.Scarecrow5 || npc.type == NPCID.Scarecrow10)
                {
                    num183 = 1f;
                }
                if (npc.type < NPCID.Scarecrow6)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.X *= 0.85f;
                        if ((double)npc.velocity.X > -0.3 && (double)npc.velocity.X < 0.3)
                        {
                            flag = true;
                            npc.velocity.Y = -7f;
                            npc.velocity.X = num183 * (float)npc.direction;
                        }
                    }
                    else if (npc.spriteDirection == npc.direction)
                    {
                        npc.velocity.X = (npc.velocity.X * 10f + num183 * (float)npc.direction) / 11f;
                    }
                }
                else if (npc.velocity.X < 0f - num183 || npc.velocity.X > num183)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num183 && npc.direction == 1)
                {
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > num183)
                    {
                        npc.velocity.X = num183;
                    }
                }
                else if (npc.velocity.X > 0f - num183 && npc.direction == -1)
                {
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < 0f - num183)
                    {
                        npc.velocity.X = 0f - num183;
                    }
                }
            }
            else if (npc.type == NPCID.Crab || npc.type == NPCID.SeaSnail || npc.type == NPCID.VortexLarva)
            {
                if (npc.velocity.X < -0.5f || npc.velocity.X > 0.5f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < 0.5f && npc.direction == 1)
                {
                    npc.velocity.X += 0.03f;
                    if (npc.velocity.X > 0.5f)
                    {
                        npc.velocity.X = 0.5f;
                    }
                }
                else if (npc.velocity.X > -0.5f && npc.direction == -1)
                {
                    npc.velocity.X -= 0.03f;
                    if (npc.velocity.X < -0.5f)
                    {
                        npc.velocity.X = -0.5f;
                    }
                }
            }
            else if (npc.type == NPCID.Mummy || npc.type == NPCID.DarkMummy || npc.type == NPCID.LightMummy || npc.type == NPCID.BloodMummy)
            {
                float num184 = 1f;
                float num185 = 0.05f;
                if (npc.life < npc.lifeMax / 2)
                {
                    num184 = 2f;
                    num185 = 0.1f;
                }
                if (npc.type == NPCID.DarkMummy || npc.type == NPCID.BloodMummy)
                {
                    num184 *= 1.5f;
                }
                if (npc.velocity.X < 0f - num184 || npc.velocity.X > num184)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num184 && npc.direction == 1)
                {
                    npc.velocity.X += num185;
                    if (npc.velocity.X > num184)
                    {
                        npc.velocity.X = num184;
                    }
                }
                else if (npc.velocity.X > 0f - num184 && npc.direction == -1)
                {
                    npc.velocity.X -= num185;
                    if (npc.velocity.X < 0f - num184)
                    {
                        npc.velocity.X = 0f - num184;
                    }
                }
            }
            else if (npc.type == NPCID.BoneLee)
            {
                float num186 = 5f;
                float num187 = 0.2f;
                if (npc.velocity.X < 0f - num186 || npc.velocity.X > num186)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num186 && npc.direction == 1)
                {
                    npc.velocity.X += num187;
                    if (npc.velocity.X > num186)
                    {
                        npc.velocity.X = num186;
                    }
                }
                else if (npc.velocity.X > 0f - num186 && npc.direction == -1)
                {
                    npc.velocity.X -= num187;
                    if (npc.velocity.X < 0f - num186)
                    {
                        npc.velocity.X = 0f - num186;
                    }
                }
            }
            else if (npc.type == NPCID.IceGolem)
            {
                float num188 = 1f;
                float num189 = 0.07f;
                num188 += (1f - (float)npc.life / (float)npc.lifeMax) * 1.5f;
                num189 += (1f - (float)npc.life / (float)npc.lifeMax) * 0.15f;
                if (npc.velocity.X < 0f - num188 || npc.velocity.X > num188)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num188 && npc.direction == 1)
                {
                    npc.velocity.X += num189;
                    if (npc.velocity.X > num188)
                    {
                        npc.velocity.X = num188;
                    }
                }
                else if (npc.velocity.X > 0f - num188 && npc.direction == -1)
                {
                    npc.velocity.X -= num189;
                    if (npc.velocity.X < 0f - num188)
                    {
                        npc.velocity.X = 0f - num188;
                    }
                }
            }
            else if (npc.type == NPCID.Eyezor)
            {
                float num190 = 1f;
                float num192 = 0.08f;
                num190 += (1f - (float)npc.life / (float)npc.lifeMax) * 2f;
                num192 += (1f - (float)npc.life / (float)npc.lifeMax) * 0.2f;
                if (npc.velocity.X < 0f - num190 || npc.velocity.X > num190)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num190 && npc.direction == 1)
                {
                    npc.velocity.X += num192;
                    if (npc.velocity.X > num190)
                    {
                        npc.velocity.X = num190;
                    }
                }
                else if (npc.velocity.X > 0f - num190 && npc.direction == -1)
                {
                    npc.velocity.X -= num192;
                    if (npc.velocity.X < 0f - num190)
                    {
                        npc.velocity.X = 0f - num190;
                    }
                }
            }
            else if (npc.type == NPCID.MartianEngineer)
            {
                if (npc.ai[2] > 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.X *= 0.8f;
                    }
                }
                else
                {
                    float num193 = 0.15f;
                    float num194 = 1.5f;
                    if (npc.velocity.X < 0f - num194 || npc.velocity.X > num194)
                    {
                        if (npc.velocity.Y == 0f)
                        {
                            npc.velocity *= 0.7f;
                        }
                    }
                    else if (npc.velocity.X < num194 && npc.direction == 1)
                    {
                        npc.velocity.X += num193;
                        if (npc.velocity.X > num194)
                        {
                            npc.velocity.X = num194;
                        }
                    }
                    else if (npc.velocity.X > 0f - num194 && npc.direction == -1)
                    {
                        npc.velocity.X -= num193;
                        if (npc.velocity.X < 0f - num194)
                        {
                            npc.velocity.X = 0f - num194;
                        }
                    }
                }
            }
            else if (npc.type == NPCID.Butcher)
            {
                float num195 = 3f;
                float num196 = 0.1f;
                if (Math.Abs(npc.velocity.X) > 2f)
                {
                    num196 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 2.5)
                {
                    num196 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 3f)
                {
                    num196 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 3.5)
                {
                    num196 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 4f)
                {
                    num196 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 4.5)
                {
                    num196 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 5f)
                {
                    num196 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 5.5)
                {
                    num196 *= 0.8f;
                }
                num195 += (1f - (float)npc.life / (float)npc.lifeMax) * 3f;
                if (npc.velocity.X < 0f - num195 || npc.velocity.X > num195)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num195 && npc.direction == 1)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.93f;
                    }
                    npc.velocity.X += num196;
                    if (npc.velocity.X > num195)
                    {
                        npc.velocity.X = num195;
                    }
                }
                else if (npc.velocity.X > 0f - num195 && npc.direction == -1)
                {
                    if (npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.93f;
                    }
                    npc.velocity.X -= num196;
                    if (npc.velocity.X < 0f - num195)
                    {
                        npc.velocity.X = 0f - num195;
                    }
                }
            }
            else if (npc.type == NPCID.GiantWalkingAntlion || npc.type == NPCID.WalkingAntlion || npc.type == NPCID.LarvaeAntlion)
            {
                float num197 = 2.5f;
                float num198 = 10f;
                float num199 = Math.Abs(npc.velocity.X);
                if (npc.type == NPCID.LarvaeAntlion)
                {
                    num197 = 2.25f;
                    num198 = 7f;
                    if (num199 > 2.5f)
                    {
                        num197 = 3f;
                        num198 += 75f;
                    }
                    else if (num199 > 2f)
                    {
                        num197 = 2.75f;
                        num198 += 55f;
                    }
                }
                else if (num199 > 2.75f)
                {
                    num197 = 3.5f;
                    num198 += 80f;
                }
                else if ((double)num199 > 2.25)
                {
                    num197 = 3f;
                    num198 += 60f;
                }
                if ((double)Math.Abs(npc.velocity.Y) < 0.5)
                {
                    if (npc.velocity.X > 0f && npc.direction < 0)
                    {
                        npc.velocity *= 0.95f;
                    }
                    if (npc.velocity.X < 0f && npc.direction > 0)
                    {
                        npc.velocity *= 0.95f;
                    }
                }
                if (Math.Abs(npc.velocity.Y) > npc.gravity)
                {
                    float num200 = 3f;
                    if (npc.type == NPCID.LarvaeAntlion)
                    {
                        num200 = 2f;
                    }
                    num198 *= num200;
                }
                if (npc.velocity.X <= 0f && npc.direction < 0)
                {
                    npc.velocity.X = (npc.velocity.X * num198 - num197) / (num198 + 1f);
                }
                else if (npc.velocity.X >= 0f && npc.direction > 0)
                {
                    npc.velocity.X = (npc.velocity.X * num198 + num197) / (num198 + 1f);
                }
                else if (Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) > 20f && Math.Abs(npc.velocity.Y) <= npc.gravity)
                {
                    npc.velocity.X *= 0.99f;
                    npc.velocity.X += (float)npc.direction * 0.025f;
                }
            }
            else if (npc.type == NPCID.Scutlix || npc.type == NPCID.VortexHornet || npc.type == NPCID.SolarDrakomire || npc.type == NPCID.SolarSolenian || npc.type == NPCID.SolarSpearman || npc.type == NPCID.DesertBeast)
            {
                float num201 = 5f;
                float num3 = 0.25f;
                float num4 = 0.7f;
                if (npc.type == NPCID.VortexHornet)
                {
                    num201 = 6f;
                    num3 = 0.2f;
                    num4 = 0.8f;
                }
                else if (npc.type == NPCID.SolarDrakomire)
                {
                    num201 = 4f;
                    num3 = 0.1f;
                    num4 = 0.95f;
                }
                else if (npc.type == NPCID.SolarSolenian)
                {
                    num201 = 6f;
                    num3 = 0.15f;
                    num4 = 0.85f;
                }
                else if (npc.type == NPCID.SolarSpearman)
                {
                    num201 = 5f;
                    num3 = 0.1f;
                    num4 = 0.95f;
                }
                else if (npc.type == NPCID.DesertBeast)
                {
                    num201 = 5f;
                    num3 = 0.15f;
                    num4 = 0.98f;
                }
                if (npc.velocity.X < 0f - num201 || npc.velocity.X > num201)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= num4;
                    }
                }
                else if (npc.velocity.X < num201 && npc.direction == 1)
                {
                    npc.velocity.X += num3;
                    if (npc.velocity.X > num201)
                    {
                        npc.velocity.X = num201;
                    }
                }
                else if (npc.velocity.X > 0f - num201 && npc.direction == -1)
                {
                    npc.velocity.X -= num3;
                    if (npc.velocity.X < 0f - num201)
                    {
                        npc.velocity.X = 0f - num201;
                    }
                }
            }
            else if ((npc.type >= NPCID.ArmedZombie && npc.type <= NPCID.ArmedZombieCenx) || npc.type == NPCID.Crawdad || npc.type == NPCID.Crawdad2 || npc.type == NPCID.ArmedTorchZombie)
            {
                if (npc.ai[2] == 0f)
                {
                    npc.damage = npc.defDamage;
                    float num5 = 1f;
                    num5 *= 1f + (1f - npc.scale);
                    // --------------------------------------------------------------- HERE
                    num5 = Math.Max(num5, 0.5f);
                    // --------------------------------------------------------------- HERE
                    if (npc.velocity.X < 0f - num5 || npc.velocity.X > num5)
                    {
                        if (npc.velocity.Y == 0f)
                        {
                            npc.velocity *= 0.8f;
                        }
                    }
                    else if (npc.velocity.X < num5 && npc.direction == 1)
                    {
                        npc.velocity.X += 0.07f;
                        if (npc.velocity.X > num5)
                        {
                            npc.velocity.X = num5;
                        }
                    }
                    else if (npc.velocity.X > 0f - num5 && npc.direction == -1)
                    {
                        npc.velocity.X -= 0.07f;
                        if (npc.velocity.X < 0f - num5)
                        {
                            npc.velocity.X = 0f - num5;
                        }
                    }
                    if (npc.velocity.Y == 0f && (!Main.IsItDay() || (double)npc.position.Y > Main.worldSurface * 16.0) && !Main.player[npc.target].dead)
                    {
                        Vector2 vector15 = npc.Center - Main.player[npc.target].Center;
                        int num6 = 50;
                        if (npc.type >= NPCID.Crawdad && npc.type <= NPCID.Crawdad2)
                        {
                            num6 = 42;
                        }
                        if (vector15.Length() < (float)num6 && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                        {
                            npc.velocity.X *= 0.7f;
                            npc.ai[2] = 1f;
                        }
                    }
                }
                else
                {
                    npc.damage = (int)((double)npc.defDamage * 1.5);
                    npc.ai[3] = 1f;
                    npc.velocity.X *= 0.9f;
                    if ((double)Math.Abs(npc.velocity.X) < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 20f || npc.velocity.Y != 0f || (Main.IsItDay() && (double)npc.position.Y < Main.worldSurface * 16.0))
                    {
                        npc.ai[2] = 0f;
                    }
                }
            }
            else if (npc.type != NPCID.SkeletonArcher && npc.type != NPCID.GoblinArcher && npc.type != NPCID.IcyMerman && npc.type != NPCID.PirateDeadeye && npc.type != NPCID.PirateCrossbower && npc.type != NPCID.PirateCaptain && npc.type != NPCID.Paladin && npc.type != NPCID.SkeletonSniper && npc.type != NPCID.TacticalSkeleton && npc.type != NPCID.SkeletonCommando && npc.type != NPCID.ElfArcher && npc.type != NPCID.CultistArcherBlue && npc.type != NPCID.CultistArcherWhite && npc.type != NPCID.BrainScrambler && npc.type != NPCID.RayGunner && (npc.type < NPCID.BoneThrowingSkeleton || npc.type > NPCID.BoneThrowingSkeleton4) && npc.type != NPCID.DrManFly && npc.type != NPCID.GreekSkeleton && npc.type != NPCID.StardustSoldier && npc.type != NPCID.StardustSpiderBig && (npc.type < NPCID.Salamander || npc.type > NPCID.Salamander9) && npc.type != NPCID.NebulaSoldier && npc.type != NPCID.VortexHornetQueen && npc.type != NPCID.MartianWalker)
            {
                float num7 = 1f;
                if (npc.type == NPCID.Gnome)
                {
                    num7 = 2.5f;
                }
                if (npc.type == NPCID.PincushionZombie)
                {
                    num7 = 1.1f;
                }
                if (npc.type == NPCID.SlimedZombie)
                {
                    num7 = 0.9f;
                }
                if (npc.type == NPCID.SwampZombie)
                {
                    num7 = 1.2f;
                }
                if (npc.type == NPCID.TwiggyZombie)
                {
                    num7 = 0.8f;
                }
                if (npc.type == NPCID.BaldZombie)
                {
                    num7 = 0.95f;
                }
                if (npc.type == NPCID.FemaleZombie)
                {
                    num7 = 0.87f;
                }
                if (npc.type == NPCID.ZombieRaincoat)
                {
                    num7 = 1.05f;
                }
                if (npc.type == NPCID.MaggotZombie)
                {
                    num7 = 0.8f;
                }
                if (npc.type == NPCID.BloodZombie)
                {
                    float num8 = (Main.player[npc.target].Center - npc.Center).Length();
                    num8 *= 0.0025f;
                    if ((double)num8 > 1.5)
                    {
                        num8 = 1.5f;
                    }
                    num7 = ((!Main.expertMode) ? (2.5f - num8) : (3f - num8));
                    num7 *= 0.8f;
                }
                if (npc.type == NPCID.BloodZombie || npc.type == NPCID.Zombie || npc.type == NPCID.BaldZombie || npc.type == NPCID.PincushionZombie || npc.type == NPCID.SlimedZombie || npc.type == NPCID.SwampZombie || npc.type == NPCID.TwiggyZombie || npc.type == NPCID.FemaleZombie || npc.type == NPCID.ZombieRaincoat || npc.type == NPCID.ZombieXmas || npc.type == NPCID.ZombieSweater)
                {
                    num7 *= 1f + (1f - npc.scale);
                    // --------------------------------------------------------------- HERE
                    num7 = Math.Max(num7, 0.5f);
                    // --------------------------------------------------------------- HERE
                }
                if (npc.velocity.X < 0f - num7 || npc.velocity.X > num7)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num7 && npc.direction == 1)
                {
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > num7)
                    {
                        npc.velocity.X = num7;
                    }
                }
                else if (npc.velocity.X > 0f - num7 && npc.direction == -1)
                {
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < 0f - num7)
                    {
                        npc.velocity.X = 0f - num7;
                    }
                }
            }
            if (npc.type >= NPCID.HellArmoredBones && npc.type <= NPCID.HellArmoredBonesSword)
            {
                Lighting.AddLight((int)npc.Center.X / 16, (int)npc.Center.Y / 16, 0.2f, 0.1f, 0f);
            }
            else if (npc.type == NPCID.MartianWalker)
            {
                Lighting.AddLight(npc.Top + new Vector2(0f, 20f), 0.3f, 0.3f, 0.7f);
            }
            else if (npc.type == NPCID.DesertGhoulCorruption)
            {
                Vector3 rgb = new Vector3(0.7f, 1f, 0.2f) * 0.5f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb);
            }
            else if (npc.type == NPCID.DesertGhoulCrimson)
            {
                Vector3 rgb2 = new Vector3(1f, 1f, 0.5f) * 0.4f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb2);
            }
            else if (npc.type == NPCID.DesertGhoulHallow)
            {
                Vector3 rgb3 = new Vector3(0.6f, 0.3f, 1f) * 0.4f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb3);
            }
            else if (npc.type == NPCID.SolarDrakomire)
            {
                npc.hide = false;
                for (int num9 = 0; num9 < 200; num9++)
                {
                    if (Main.npc[num9].active && Main.npc[num9].type == NPCID.SolarDrakomireRider && Main.npc[num9].ai[0] == (float)npc.whoAmI)
                    {
                        npc.hide = true;
                        break;
                    }
                }
            }
            else if (npc.type == NPCID.MushiLadybug)
            {
                if (npc.velocity.Y != 0f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                    if (Main.player[npc.target].Center.X < npc.position.X && npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.95f;
                    }
                    else if (Main.player[npc.target].Center.X > npc.position.X + (float)npc.width && npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.95f;
                    }
                    if (Main.player[npc.target].Center.X < npc.position.X && npc.velocity.X > -5f)
                    {
                        npc.velocity.X -= 0.1f;
                    }
                    else if (Main.player[npc.target].Center.X > npc.position.X + (float)npc.width && npc.velocity.X < 5f)
                    {
                        npc.velocity.X += 0.1f;
                    }
                }
                else if (Main.player[npc.target].Center.Y + 50f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    flag = true;
                    npc.velocity.Y = -7f;
                }
            }
            else if (npc.type == NPCID.VortexRifleman)
            {
                if (npc.localAI[3] == 0f)
                {
                    npc.localAI[3] = 1f;
                    npc.ai[3] = -120f;
                }
                if (npc.velocity.Y == 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.velocity.Y != 0f && npc.ai[2] == 1f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = -npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num10 = 0.3f;
                        float num11 = 8f;
                        float num12 = 0.3f;
                        float num14 = 7f;
                        float num15 = Main.player[npc.target].Center.X - (float)(npc.direction * 300) - npc.Center.X;
                        float num16 = Main.player[npc.target].Bottom.Y - npc.Bottom.Y;
                        if (num15 < 0f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X *= 0.9f;
                        }
                        else if (num15 > 0f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X *= 0.9f;
                        }
                        if (num15 < 0f && npc.velocity.X > 0f - num14)
                        {
                            npc.velocity.X -= num12;
                        }
                        else if (num15 > 0f && npc.velocity.X < num14)
                        {
                            npc.velocity.X += num12;
                        }
                        if (npc.velocity.X > num14)
                        {
                            npc.velocity.X = num14;
                        }
                        if (npc.velocity.X < 0f - num14)
                        {
                            npc.velocity.X = 0f - num14;
                        }
                        if (num16 < -20f && npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y *= 0.8f;
                        }
                        else if (num16 > 20f && npc.velocity.Y < 0f)
                        {
                            npc.velocity.Y *= 0.8f;
                        }
                        if (num16 < -20f && npc.velocity.Y > 0f - num11)
                        {
                            npc.velocity.Y -= num10;
                        }
                        else if (num16 > 20f && npc.velocity.Y < num11)
                        {
                            npc.velocity.Y += num10;
                        }
                    }
                    if (Main.rand.NextBool(3))
                    {
                        npc.position += npc.netOffset;
                        Vector2 vector16 = npc.Center + new Vector2(npc.direction * -14, -8f) - Vector2.One * 4f;
                        Vector2 vector17 = new Vector2(npc.direction * -6, 12f) * 0.2f + Utils.RandomVector2(Main.rand, -1f, 1f) * 0.1f;
                        Dust obj5 = Main.dust[Dust.NewDust(vector16, 8, 8, DustID.Vortex, vector17.X, vector17.Y, 100, Color.Transparent, 1f + Main.rand.NextFloat() * 0.5f)];
                        obj5.noGravity = true;
                        obj5.velocity = vector17;
                        obj5.customData = npc;
                        npc.position -= npc.netOffset;
                    }
                    for (int num17 = 0; num17 < 200; num17++)
                    {
                        if (num17 != npc.whoAmI && Main.npc[num17].active && Main.npc[num17].type == npc.type && Math.Abs(npc.position.X - Main.npc[num17].position.X) + Math.Abs(npc.position.Y - Main.npc[num17].position.Y) < (float)npc.width)
                        {
                            if (npc.position.X < Main.npc[num17].position.X)
                            {
                                npc.velocity.X -= 0.15f;
                            }
                            else
                            {
                                npc.velocity.X += 0.15f;
                            }
                            if (npc.position.Y < Main.npc[num17].position.Y)
                            {
                                npc.velocity.Y -= 0.15f;
                            }
                            else
                            {
                                npc.velocity.Y += 0.15f;
                            }
                        }
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    flag = true;
                    npc.velocity.Y = -5f;
                    npc.ai[2] = 1f;
                }
                if (npc.ai[3] < 0f)
                {
                    npc.ai[3] += 1f;
                }
                int num18 = 30;
                int num19 = 10;
                int num20 = 180;
                if (npc.ai[3] >= 0f && npc.ai[3] <= (float)num18)
                {
                    Vector2 vector18 = npc.DirectionTo(Main.player[npc.target].Center);
                    bool flag4 = Math.Abs(vector18.Y) <= Math.Abs(vector18.X);
                    bool flag5 = npc.Distance(Main.player[npc.target].Center) < 800f && flag4 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
                    npc.ai[3] = MathHelper.Clamp(npc.ai[3] + (float)flag5.ToDirectionInt(), 0f, num18);
                }
                if (npc.ai[3] >= (float)(num18 + 1) && (npc.ai[3] += 1f) >= (float)(num18 + num19))
                {
                    npc.ai[3] = num18 - num20;
                    npc.netUpdate = true;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[3] == (float)num18)
                {
                    npc.ai[3] += 1f;
                    npc.netUpdate = true;
                    int num21 = 20;
                    Vector2 chaserPosition = npc.Center + new Vector2(npc.direction * 30, 2f);
                    Vector2 vector19 = npc.DirectionTo(Main.player[npc.target].Center) * num21;
                    if (vector19.HasNaNs())
                    {
                        vector19 = new Vector2(npc.direction * num21, 0f);
                    }
                    int num22 = 2;
                    Utils.ChaseResults chaseResults = Utils.GetChaseResults(chaserPosition, num21, Main.player[npc.target].Center, Main.player[npc.target].velocity * 0.5f / num22);
                    if (chaseResults.InterceptionHappens)
                    {
                        Vector2 vector20 = chaseResults.ChaserVelocity / num22;
                        vector19.X = vector20.X;
                        vector19.Y = vector20.Y;
                    }
                    int attackDamage_ForProjectiles = npc.GetAttackDamage_ForProjectiles(75f, 50f);
                    for (int num23 = 0; num23 < 4; num23++)
                    {
                        Vector2 vector22 = vector19 + Utils.RandomVector2(Main.rand, -0.8f, 0.8f) * ((num23 != 0) ? 1 : 0);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), chaserPosition.X, chaserPosition.Y, vector22.X, vector22.Y, 577, attackDamage_ForProjectiles, 1f, Main.myPlayer);
                    }
                }
            }
            else if (npc.type == NPCID.VortexHornet)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.ai[2] = 0f;
                    npc.rotation = 0f;
                }
                else
                {
                    npc.rotation = npc.velocity.X * 0.1f;
                }
                if (npc.velocity.Y != 0f && npc.ai[2] == 1f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = -npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num25 = Main.player[npc.target].Center.X - npc.Center.X;
                        float num26 = Main.player[npc.target].Center.Y - npc.Center.Y;
                        if (num25 < 0f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X *= 0.98f;
                        }
                        else if (num25 > 0f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X *= 0.98f;
                        }
                        if (num25 < -20f && npc.velocity.X > -6f)
                        {
                            npc.velocity.X -= 0.015f;
                        }
                        else if (num25 > 20f && npc.velocity.X < 6f)
                        {
                            npc.velocity.X += 0.015f;
                        }
                        if (npc.velocity.X > 6f)
                        {
                            npc.velocity.X = 6f;
                        }
                        if (npc.velocity.X < -6f)
                        {
                            npc.velocity.X = -6f;
                        }
                        if (num26 < -20f && npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y *= 0.98f;
                        }
                        else if (num26 > 20f && npc.velocity.Y < 0f)
                        {
                            npc.velocity.Y *= 0.98f;
                        }
                        if (num26 < -20f && npc.velocity.Y > -6f)
                        {
                            npc.velocity.Y -= 0.15f;
                        }
                        else if (num26 > 20f && npc.velocity.Y < 6f)
                        {
                            npc.velocity.Y += 0.15f;
                        }
                    }
                    for (int num27 = 0; num27 < 200; num27++)
                    {
                        if (num27 != npc.whoAmI && Main.npc[num27].active && Main.npc[num27].type == npc.type && Math.Abs(npc.position.X - Main.npc[num27].position.X) + Math.Abs(npc.position.Y - Main.npc[num27].position.Y) < (float)npc.width)
                        {
                            if (npc.position.X < Main.npc[num27].position.X)
                            {
                                npc.velocity.X -= 0.05f;
                            }
                            else
                            {
                                npc.velocity.X += 0.05f;
                            }
                            if (npc.position.Y < Main.npc[num27].position.Y)
                            {
                                npc.velocity.Y -= 0.05f;
                            }
                            else
                            {
                                npc.velocity.Y += 0.05f;
                            }
                        }
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    flag = true;
                    npc.velocity.Y = -5f;
                    npc.ai[2] = 1f;
                }
            }
            else if (npc.type == NPCID.VortexHornetQueen)
            {
                float num28 = 6f;
                float num29 = 0.2f;
                float num30 = 6f;
                if (npc.ai[1] > 0f && npc.velocity.Y > 0f)
                {
                    npc.velocity.Y *= 0.85f;
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.Y = -0.4f;
                    }
                }
                if (npc.velocity.Y != 0f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num31 = Main.player[npc.target].Center.X - (float)(npc.direction * 300) - npc.Center.X;
                        if (num31 < 40f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X *= 0.98f;
                        }
                        else if (num31 > 40f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X *= 0.98f;
                        }
                        if (num31 < 40f && npc.velocity.X > 0f - num28)
                        {
                            npc.velocity.X -= num29;
                        }
                        else if (num31 > 40f && npc.velocity.X < num28)
                        {
                            npc.velocity.X += num29;
                        }
                        if (npc.velocity.X > num28)
                        {
                            npc.velocity.X = num28;
                        }
                        if (npc.velocity.X < 0f - num28)
                        {
                            npc.velocity.X = 0f - num28;
                        }
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    flag = true;
                    npc.velocity.Y = 0f - num30;
                }
                for (int num32 = 0; num32 < 200; num32++)
                {
                    if (num32 != npc.whoAmI && Main.npc[num32].active && Main.npc[num32].type == npc.type && Math.Abs(npc.position.X - Main.npc[num32].position.X) + Math.Abs(npc.position.Y - Main.npc[num32].position.Y) < (float)npc.width)
                    {
                        if (npc.position.X < Main.npc[num32].position.X)
                        {
                            npc.velocity.X -= 0.1f;
                        }
                        else
                        {
                            npc.velocity.X += 0.1f;
                        }
                        if (npc.position.Y < Main.npc[num32].position.Y)
                        {
                            npc.velocity.Y -= 0.1f;
                        }
                        else
                        {
                            npc.velocity.Y += 0.1f;
                        }
                    }
                }
                if (Main.rand.NextBool(6) && npc.ai[1] <= 20f)
                {
                    npc.position += npc.netOffset;
                    Dust obj6 = Main.dust[Dust.NewDust(npc.Center + new Vector2((npc.spriteDirection == 1) ? 8 : (-20), -20f), 8, 8, DustID.Vortex, npc.velocity.X, npc.velocity.Y, 100)];
                    obj6.velocity = obj6.velocity / 4f + npc.velocity / 2f;
                    obj6.scale = 0.6f;
                    obj6.noLight = true;
                    npc.position -= npc.netOffset;
                }
                if (npc.ai[1] >= 57f)
                {
                    npc.position += npc.netOffset;
                    int num33 = Utils.SelectRandom<int>(Main.rand, 161, 229);
                    Dust obj7 = Main.dust[Dust.NewDust(npc.Center + new Vector2((npc.spriteDirection == 1) ? 8 : (-20), -20f), 8, 8, num33, npc.velocity.X, npc.velocity.Y, 100)];
                    obj7.velocity = obj7.velocity / 4f + npc.DirectionTo(Main.player[npc.target].Top);
                    obj7.scale = 1.2f;
                    obj7.noLight = true;
                    npc.position -= npc.netOffset;
                }
                if (Main.rand.NextBool(6))
                {
                    npc.position += npc.netOffset;
                    Dust dust5 = Main.dust[Dust.NewDust(npc.Center, 2, 2, DustID.Vortex)];
                    dust5.position = npc.Center + new Vector2((npc.spriteDirection == 1) ? 26 : (-26), 24f);
                    dust5.velocity.X = 0f;
                    if (dust5.velocity.Y < 0f)
                    {
                        dust5.velocity.Y = 0f;
                    }
                    dust5.noGravity = true;
                    dust5.scale = 1f;
                    dust5.noLight = true;
                    npc.position -= npc.netOffset;
                }
            }
            else if (npc.type == NPCID.SnowFlinx)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.rotation = 0f;
                    npc.localAI[0] = 0f;
                }
                else if (npc.localAI[0] == 1f)
                {
                    npc.rotation += npc.velocity.X * 0.05f;
                }
            }
            else if (npc.type == NPCID.VortexLarva)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.rotation = 0f;
                }
                else
                {
                    npc.rotation += npc.velocity.X * 0.08f;
                }
            }
            if (npc.type == NPCID.Vampire && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vector23 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num204 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector23.X;
                float num35 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector23.Y;
                if ((float)Math.Sqrt(num204 * num204 + num35 * num35) > 300f)
                {
                    npc.Transform(158);
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.expertMode && npc.target >= 0 && (npc.type == NPCID.BlackRecluse || npc.type == NPCID.BlackRecluseWall || npc.type == NPCID.JungleCreeper || npc.type == NPCID.JungleCreeperWall) && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                {
                    npc.localAI[0] += 1f;
                    if (npc.justHit)
                    {
                        npc.localAI[0] -= Main.rand.Next(20, 60);
                        if (npc.localAI[0] < 0f)
                        {
                            npc.localAI[0] = 0f;
                        }
                    }
                    if (npc.localAI[0] > (float)Main.rand.Next(180, 900))
                    {
                        npc.localAI[0] = 0f;
                        Vector2 vector24 = Main.player[npc.target].Center - npc.Center;
                        vector24.Normalize();
                        vector24 *= 8f;
                        int attackDamage_ForProjectiles2 = npc.GetAttackDamage_ForProjectiles(18f, 18f);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, vector24.X, vector24.Y, 472, attackDamage_ForProjectiles2, 0f, Main.myPlayer);
                    }
                }
                if (npc.velocity.Y == 0f)
                {
                    int num36 = -1;
                    switch (npc.type)
                    {
                        case 164:
                            num36 = 165;
                            break;
                        case 236:
                            num36 = 237;
                            break;
                        case 163:
                            num36 = 238;
                            break;
                        case 239:
                            num36 = 240;
                            break;
                        case 530:
                            num36 = 531;
                            break;
                    }
                    if (num36 != -1 && npc.NPCCanStickToWalls())
                    {
                        npc.Transform(num36);
                    }
                }
            }
            if (npc.type == NPCID.IceGolem)
            {
                if (npc.justHit && Main.rand.NextBool(3))
                {
                    npc.ai[2] -= Main.rand.Next(30);
                }
                if (npc.ai[2] < 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                float num37 = Main.rand.Next(30, 900);
                num37 *= (float)npc.life / (float)npc.lifeMax;
                num37 += 30f;
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] >= num37 && npc.velocity.Y == 0f && !Main.player[npc.target].dead && !Main.player[npc.target].frozen && ((npc.direction > 0 && npc.Center.X < Main.player[npc.target].Center.X) || (npc.direction < 0 && npc.Center.X > Main.player[npc.target].Center.X)) && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    Vector2 vector25 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + 20f);
                    vector25.X += 10 * npc.direction;
                    float num38 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector25.X;
                    float num39 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector25.Y;
                    num38 += (float)Main.rand.Next(-40, 41);
                    num39 += (float)Main.rand.Next(-40, 41);
                    float num40 = (float)Math.Sqrt(num38 * num38 + num39 * num39);
                    npc.netUpdate = true;
                    num40 = 15f / num40;
                    num38 *= num40;
                    num39 *= num40;
                    int num41 = 32;
                    int num42 = 257;
                    vector25.X += num38 * 3f;
                    vector25.Y += num39 * 3f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), vector25.X, vector25.Y, num38, num39, num42, num41, 0f, Main.myPlayer);
                    npc.ai[2] = 0f;
                }
            }
            if (npc.type == NPCID.Eyezor)
            {
                if (npc.justHit)
                {
                    npc.ai[2] -= Main.rand.Next(30);
                }
                if (npc.ai[2] < 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                float num43 = Main.rand.Next(60, 1800);
                num43 *= (float)npc.life / (float)npc.lifeMax;
                num43 += 15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] >= num43 && npc.velocity.Y == 0f && !Main.player[npc.target].dead && !Main.player[npc.target].frozen && ((npc.direction > 0 && npc.Center.X < Main.player[npc.target].Center.X) || (npc.direction < 0 && npc.Center.X > Main.player[npc.target].Center.X)) && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    Vector2 vector26 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + 12f);
                    vector26.X += 6 * npc.direction;
                    float num44 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector26.X;
                    float num46 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector26.Y;
                    num44 += (float)Main.rand.Next(-40, 41);
                    num46 += (float)Main.rand.Next(-30, 0);
                    float num47 = (float)Math.Sqrt(num44 * num44 + num46 * num46);
                    npc.netUpdate = true;
                    num47 = 15f / num47;
                    num44 *= num47;
                    num46 *= num47;
                    int num48 = 30;
                    int num49 = 83;
                    vector26.X += num44 * 3f;
                    vector26.Y += num46 * 3f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), vector26.X, vector26.Y, num44, num46, num49, num48, 0f, Main.myPlayer);
                    npc.ai[2] = 0f;
                }
            }
            if (npc.type == NPCID.MartianEngineer)
            {
                if (npc.confused)
                {
                    npc.ai[2] = -60f;
                }
                else
                {
                    if (npc.ai[2] < 60f)
                    {
                        npc.ai[2] += 1f;
                    }
                    if (npc.ai[2] > 0f && NPC.CountNPCS(387) >= 4 * NPC.CountNPCS(386))
                    {
                        npc.ai[2] = 0f;
                    }
                    if (npc.justHit)
                    {
                        npc.ai[2] = -30f;
                    }
                    if (npc.ai[2] == 30f)
                    {
                        int num50 = (int)npc.position.X / 16;
                        int num51 = (int)npc.position.Y / 16;
                        int num52 = (int)npc.position.X / 16;
                        int num53 = (int)npc.position.Y / 16;
                        int num54 = 5;
                        int num55 = 0;
                        bool flag6 = false;
                        int num57 = 2;
                        int num58 = 0;
                        while (!flag6 && num55 < 100)
                        {
                            num55++;
                            int num59 = Main.rand.Next(num50 - num54, num50 + num54);
                            for (int num60 = Main.rand.Next(num51 - num54, num51 + num54); num60 < num51 + num54; num60++)
                            {
                                if ((num60 < num51 - num57 || num60 > num51 + num57 || num59 < num50 - num57 || num59 > num50 + num57) && (num60 < num53 - num58 || num60 > num53 + num58 || num59 < num52 - num58 || num59 > num52 + num58) && Main.tile[num59, num60].HasUnactuatedTile)
                                {
                                    bool flag7 = true;
                                    if (Main.tile[num59, num60 - 1].LiquidType == LiquidID.Lava)
                                    {
                                        flag7 = false;
                                    }
                                    if (flag7 && Main.tileSolid[Main.tile[num59, num60].TileType] && !Collision.SolidTiles(num59 - 1, num59 + 1, num60 - 4, num60 - 1))
                                    {
                                        int num61 = NPC.NewNPC(npc.GetSource_FromAI(), num59 * 16 - npc.width / 2, num60 * 16, 387);
                                        Main.npc[num61].position.Y = num60 * 16 - Main.npc[num61].height;
                                        flag6 = true;
                                        npc.netUpdate = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (npc.ai[2] == 60f)
                    {
                        npc.ai[2] = -120f;
                    }
                }
            }
            if (npc.type == NPCID.GigaZapper)
            {
                if (npc.confused)
                {
                    npc.ai[2] = -60f;
                }
                else
                {
                    if (npc.ai[2] < 20f)
                    {
                        npc.ai[2] += 1f;
                    }
                    if (npc.justHit)
                    {
                        npc.ai[2] = -30f;
                    }
                    if (npc.ai[2] == 20f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.ai[2] = -10 + Main.rand.Next(3) * -10;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y + 8f, npc.direction * 6, 0f, 437, 25, 1f, Main.myPlayer);
                    }
                }
            }
            if (npc.type == NPCID.SkeletonArcher || npc.type == NPCID.GoblinArcher || npc.type == NPCID.IcyMerman || npc.type == NPCID.PirateDeadeye || npc.type == NPCID.PirateCrossbower || npc.type == NPCID.PirateCaptain || npc.type == NPCID.Paladin || npc.type == NPCID.SkeletonSniper || npc.type == NPCID.TacticalSkeleton || npc.type == NPCID.SkeletonCommando || npc.type == NPCID.ElfArcher || npc.type == NPCID.CultistArcherBlue || npc.type == NPCID.CultistArcherWhite || npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner || (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) || npc.type == NPCID.DrManFly || npc.type == NPCID.GreekSkeleton || npc.type == NPCID.StardustSoldier || npc.type == NPCID.StardustSpiderBig || (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9) || npc.type == NPCID.NebulaSoldier || npc.type == NPCID.VortexHornetQueen || npc.type == NPCID.MartianWalker)
            {
                bool flag8 = npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner || npc.type == NPCID.MartianWalker;
                bool flag9 = npc.type == NPCID.VortexHornetQueen;
                bool flag10 = true;
                int num62 = -1;
                int num63 = -1;
                if (npc.type == NPCID.StardustSoldier)
                {
                    flag8 = true;
                    num62 = 120;
                    num63 = 120;
                    if (npc.ai[1] <= 220f)
                    {
                        flag10 = false;
                    }
                }
                if (npc.ai[1] > 0f)
                {
                    npc.ai[1] -= 1f;
                }
                if (npc.justHit)
                {
                    npc.ai[1] = 30f;
                    npc.ai[2] = 0f;
                }
                int num64 = 70;
                if (npc.type == NPCID.CultistArcherBlue || npc.type == NPCID.CultistArcherWhite)
                {
                    num64 = 80;
                }
                if (npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner)
                {
                    num64 = 80;
                }
                if (npc.type == NPCID.MartianWalker)
                {
                    num64 = 15;
                }
                if (npc.type == NPCID.ElfArcher)
                {
                    num64 = 110;
                }
                if (npc.type == NPCID.SkeletonSniper)
                {
                    num64 = 200;
                }
                if (npc.type == NPCID.TacticalSkeleton)
                {
                    num64 = 120;
                }
                if (npc.type == NPCID.SkeletonCommando)
                {
                    num64 = 90;
                }
                if (npc.type == NPCID.GoblinArcher)
                {
                    num64 = 180;
                }
                if (npc.type == NPCID.IcyMerman)
                {
                    num64 = 50;
                }
                if (npc.type == NPCID.GreekSkeleton)
                {
                    num64 = 100;
                }
                if (npc.type == NPCID.PirateDeadeye)
                {
                    num64 = 40;
                }
                if (npc.type == NPCID.PirateCrossbower)
                {
                    num64 = 80;
                }
                if (npc.type == NPCID.Paladin)
                {
                    num64 = 30;
                }
                if (npc.type == NPCID.StardustSoldier)
                {
                    num64 = 330;
                }
                if (npc.type == NPCID.StardustSpiderBig)
                {
                    num64 = 60;
                }
                if (npc.type == NPCID.NebulaSoldier)
                {
                    num64 = 180;
                }
                if (npc.type == NPCID.VortexHornetQueen)
                {
                    num64 = 60;
                }
                bool flag11 = false;
                if (npc.type == NPCID.PirateCaptain)
                {
                    if (npc.localAI[2] >= 20f)
                    {
                        flag11 = true;
                    }
                    num64 = ((!flag11) ? 8 : 60);
                }
                int num65 = num64 / 2;
                if (npc.type == NPCID.NebulaSoldier)
                {
                    num65 = num64 - 1;
                }
                if (npc.type == NPCID.VortexHornetQueen)
                {
                    num65 = num64 - 1;
                }
                if (npc.type == NPCID.StardustSoldier)
                {
                    num65 = 220;
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.ai[2] > 0f)
                {
                    if (flag10)
                    {
                        npc.TargetClosest();
                    }
                    if (npc.ai[1] == (float)num65)
                    {
                        if (npc.type == NPCID.PirateCaptain)
                        {
                            npc.localAI[2] += 1f;
                        }
                        float num66 = 11f;
                        if (npc.type == NPCID.GoblinArcher)
                        {
                            num66 = 9f;
                        }
                        if (npc.type == NPCID.IcyMerman)
                        {
                            num66 = 7f;
                        }
                        if (npc.type == NPCID.Paladin)
                        {
                            num66 = 9f;
                        }
                        if (npc.type == NPCID.SkeletonCommando)
                        {
                            num66 = 4f;
                        }
                        if (npc.type == NPCID.PirateDeadeye)
                        {
                            num66 = 14f;
                        }
                        if (npc.type == NPCID.PirateCrossbower)
                        {
                            num66 = 16f;
                        }
                        if (npc.type == NPCID.RayGunner)
                        {
                            num66 = 7f;
                        }
                        if (npc.type == NPCID.MartianWalker)
                        {
                            num66 = 8f;
                        }
                        if (npc.type == NPCID.StardustSpiderBig)
                        {
                            num66 = 4f;
                        }
                        if (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4)
                        {
                            num66 = 7f;
                        }
                        if (npc.type == NPCID.GreekSkeleton)
                        {
                            num66 = 8f;
                        }
                        if (npc.type == NPCID.DrManFly)
                        {
                            num66 = 7.5f;
                        }
                        if (npc.type == NPCID.StardustSoldier)
                        {
                            num66 = 1f;
                        }
                        if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            num66 = 7f;
                        }
                        Vector2 chaserPosition2 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                        if (npc.type == NPCID.GreekSkeleton)
                        {
                            chaserPosition2.Y -= 14f;
                        }
                        if (npc.type == NPCID.IcyMerman)
                        {
                            chaserPosition2.Y -= 10f;
                        }
                        if (npc.type == NPCID.Paladin)
                        {
                            chaserPosition2.Y -= 10f;
                        }
                        if (npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner)
                        {
                            chaserPosition2.Y += 6f;
                        }
                        if (npc.type == NPCID.MartianWalker)
                        {
                            chaserPosition2.Y = npc.position.Y + 20f;
                        }
                        if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            chaserPosition2.Y -= 8f;
                        }
                        if (npc.type == NPCID.VortexHornetQueen)
                        {
                            chaserPosition2 += new Vector2(npc.spriteDirection * 2, -12f);
                            num66 = 7f;
                        }
                        float num68 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - chaserPosition2.X;
                        float num69 = Math.Abs(num68) * 0.1f;
                        if (npc.type == NPCID.SkeletonSniper || npc.type == NPCID.TacticalSkeleton)
                        {
                            num69 = 0f;
                        }
                        if (npc.type == NPCID.PirateCrossbower)
                        {
                            num69 = Math.Abs(num68) * 0.08f;
                        }
                        if (npc.type == NPCID.PirateDeadeye || (npc.type == NPCID.PirateCaptain && !flag11))
                        {
                            num69 = 0f;
                        }
                        if (npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner || npc.type == NPCID.MartianWalker)
                        {
                            num69 = 0f;
                        }
                        if (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4)
                        {
                            num69 = Math.Abs(num68) * (float)Main.rand.Next(10, 50) * 0.01f;
                        }
                        if (npc.type == NPCID.DrManFly)
                        {
                            num69 = Math.Abs(num68) * (float)Main.rand.Next(10, 50) * 0.01f;
                        }
                        if (npc.type == NPCID.GreekSkeleton)
                        {
                            num69 = Math.Abs(num68) * (float)Main.rand.Next(-10, 11) * 0.0035f;
                        }
                        if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            num69 = Math.Abs(num68) * (float)Main.rand.Next(1, 11) * 0.0025f;
                        }
                        float num70 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - chaserPosition2.Y - num69;
                        if (npc.type == NPCID.SkeletonSniper)
                        {
                            num68 += (float)Main.rand.Next(-40, 41) * 0.2f;
                            num70 += (float)Main.rand.Next(-40, 41) * 0.2f;
                        }
                        else if (npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner || npc.type == NPCID.MartianWalker)
                        {
                            num68 += (float)Main.rand.Next(-100, 101) * 0.4f;
                            num70 += (float)Main.rand.Next(-100, 101) * 0.4f;
                            num68 *= (float)Main.rand.Next(85, 116) * 0.01f;
                            num70 *= (float)Main.rand.Next(85, 116) * 0.01f;
                            if (npc.type == NPCID.MartianWalker)
                            {
                                num68 += (float)Main.rand.Next(-100, 101) * 0.6f;
                                num70 += (float)Main.rand.Next(-100, 101) * 0.6f;
                                num68 *= (float)Main.rand.Next(85, 116) * 0.015f;
                                num70 *= (float)Main.rand.Next(85, 116) * 0.015f;
                            }
                        }
                        else if (npc.type == NPCID.GreekSkeleton)
                        {
                            num68 += (float)Main.rand.Next(-40, 41) * 0.4f;
                            num70 += (float)Main.rand.Next(-40, 41) * 0.4f;
                        }
                        else if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            num68 += (float)Main.rand.Next(-40, 41) * 0.3f;
                            num70 += (float)Main.rand.Next(-40, 41) * 0.3f;
                        }
                        else if (npc.type == NPCID.VortexHornetQueen)
                        {
                            num68 += (float)Main.rand.Next(-30, 31) * 0.3f;
                            num70 += (float)Main.rand.Next(-30, 31) * 0.3f;
                        }
                        else if (npc.type != NPCID.TacticalSkeleton)
                        {
                            num68 += (float)Main.rand.Next(-40, 41);
                            num70 += (float)Main.rand.Next(-40, 41);
                        }
                        float num71 = (float)Math.Sqrt(num68 * num68 + num70 * num70);
                        npc.netUpdate = true;
                        num71 = num66 / num71;
                        num68 *= num71;
                        num70 *= num71;
                        int num72 = 35;
                        int num73 = 82;
                        if (npc.type == NPCID.GoblinArcher)
                        {
                            num72 = 11;
                        }
                        if (npc.type == NPCID.IcyMerman)
                        {
                            num72 = 37;
                        }
                        if (npc.type == NPCID.CultistArcherBlue || npc.type == NPCID.CultistArcherWhite)
                        {
                            num72 = 40;
                        }
                        if (npc.type == NPCID.ElfArcher)
                        {
                            num72 = 45;
                        }
                        if (npc.type == NPCID.DrManFly)
                        {
                            num72 = 50;
                        }
                        if (npc.type == NPCID.GoblinArcher)
                        {
                            num73 = 81;
                        }
                        if (npc.type == NPCID.CultistArcherBlue || npc.type == NPCID.CultistArcherWhite)
                        {
                            num73 = 81;
                        }
                        if (npc.type == NPCID.BrainScrambler)
                        {
                            num73 = 436;
                            num72 = 24;
                        }
                        if (npc.type == NPCID.RayGunner)
                        {
                            num73 = 438;
                            num72 = 30;
                        }
                        if (npc.type == NPCID.MartianWalker)
                        {
                            num73 = 592;
                            num72 = 35;
                        }
                        if (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4)
                        {
                            num73 = 471;
                            num72 = 15;
                        }
                        if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            num73 = 572;
                            num72 = 14;
                        }
                        if (npc.type == NPCID.GreekSkeleton)
                        {
                            num73 = 508;
                            num72 = 18;
                        }
                        if (npc.type == NPCID.IcyMerman)
                        {
                            num73 = 177;
                        }
                        if (npc.type == NPCID.DrManFly)
                        {
                            num73 = 501;
                        }
                        if (npc.type == NPCID.StardustSoldier)
                        {
                            num73 = 537;
                            num72 = npc.GetAttackDamage_ForProjectiles(60f, 45f);
                        }
                        if (npc.type == NPCID.NebulaSoldier)
                        {
                            num73 = 573;
                            num72 = npc.GetAttackDamage_ForProjectiles(60f, 45f);
                        }
                        if (npc.type == NPCID.VortexHornetQueen)
                        {
                            num73 = 581;
                            num72 = npc.GetAttackDamage_ForProjectiles(60f, 45f);
                        }
                        if (npc.type == NPCID.SkeletonSniper)
                        {
                            num73 = 302;
                            num72 = 100;
                        }
                        if (npc.type == NPCID.Paladin)
                        {
                            num73 = 300;
                            num72 = 60;
                        }
                        if (npc.type == NPCID.SkeletonCommando)
                        {
                            num73 = 303;
                            num72 = 60;
                        }
                        if (npc.type == NPCID.PirateDeadeye)
                        {
                            num73 = 180;
                            num72 = 25;
                        }
                        if (npc.type == NPCID.PirateCrossbower)
                        {
                            num73 = 82;
                            num72 = 40;
                        }
                        if (npc.type == NPCID.TacticalSkeleton)
                        {
                            num72 = 50;
                            num73 = 180;
                        }
                        if (npc.type == NPCID.PirateCaptain)
                        {
                            num73 = 180;
                            num72 = 30;
                            if (flag11)
                            {
                                num72 = 100;
                                num73 = 240;
                                npc.localAI[2] = 0f;
                            }
                        }
                        Player player3 = Main.player[npc.target];
                        Vector2? vector27 = null;
                        if (npc.type == NPCID.VortexHornetQueen)
                        {
                            vector27 = Main.rand.NextVector2FromRectangle(player3.Hitbox);
                        }
                        if (vector27.HasValue)
                        {
                            Utils.ChaseResults chaseResults2 = Utils.GetChaseResults(chaserPosition2, num66, vector27.Value, player3.velocity);
                            if (chaseResults2.InterceptionHappens)
                            {
                                Vector2 vector39 = Utils.FactorAcceleration(chaseResults2.ChaserVelocity, chaseResults2.InterceptionTime, new Vector2(0f, 0.1f), 15);
                                num68 = vector39.X;
                                num70 = vector39.Y;
                            }
                        }
                        chaserPosition2.X += num68;
                        chaserPosition2.Y += num70;
                        if (npc.type == NPCID.Paladin)
                        {
                            num72 = npc.GetAttackDamage_ForProjectiles(num72, (float)num72 * 0.75f);
                        }
                        if (npc.type >= NPCID.BrainScrambler && npc.type <= NPCID.MartianSaucer)
                        {
                            num72 = npc.GetAttackDamage_ForProjectiles(num72, (float)num72 * 0.8f);
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (npc.type == NPCID.TacticalSkeleton)
                            {
                                for (int num74 = 0; num74 < 4; num74++)
                                {
                                    num68 = player3.position.X + (float)player3.width * 0.5f - chaserPosition2.X;
                                    num70 = player3.position.Y + (float)player3.height * 0.5f - chaserPosition2.Y;
                                    num71 = (float)Math.Sqrt(num68 * num68 + num70 * num70);
                                    num71 = 12f / num71;
                                    num68 = (num68 += (float)Main.rand.Next(-40, 41));
                                    num70 = (num70 += (float)Main.rand.Next(-40, 41));
                                    num68 *= num71;
                                    num70 *= num71;
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), chaserPosition2.X, chaserPosition2.Y, num68, num70, num73, num72, 0f, Main.myPlayer);
                                }
                            }
                            else if (npc.type == NPCID.StardustSoldier)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), chaserPosition2.X, chaserPosition2.Y, num68, num70, num73, num72, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            }
                            else if (npc.type == NPCID.NebulaSoldier)
                            {
                                for (int num75 = 0; num75 < 4; num75++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X - (float)(npc.spriteDirection * 4), npc.Center.Y + 6f, (float)(-3 + 2 * num75) * 0.15f, (float)(-Main.rand.Next(0, 3)) * 0.2f - 0.1f, num73, num72, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                }
                            }
                            else if (npc.type == NPCID.StardustSpiderBig)
                            {
                                int num76 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, 410, npc.whoAmI);
                                Main.npc[num76].velocity = new Vector2(num68, -6f + num70);
                            }
                            else
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), chaserPosition2.X, chaserPosition2.Y, num68, num70, num73, num72, 0f, Main.myPlayer);
                            }
                        }
                        if (Math.Abs(num70) > Math.Abs(num68) * 2f)
                        {
                            if (num70 > 0f)
                            {
                                npc.ai[2] = 1f;
                            }
                            else
                            {
                                npc.ai[2] = 5f;
                            }
                        }
                        else if (Math.Abs(num68) > Math.Abs(num70) * 2f)
                        {
                            npc.ai[2] = 3f;
                        }
                        else if (num70 > 0f)
                        {
                            npc.ai[2] = 2f;
                        }
                        else
                        {
                            npc.ai[2] = 4f;
                        }
                    }
                    if ((npc.velocity.Y != 0f && !flag9) || npc.ai[1] <= 0f)
                    {
                        npc.ai[2] = 0f;
                        npc.ai[1] = 0f;
                    }
                    else if (!flag8 || (num62 != -1 && npc.ai[1] >= (float)num62 && npc.ai[1] < (float)(num62 + num63) && (!flag9 || npc.velocity.Y == 0f)))
                    {
                        npc.velocity.X *= 0.9f;
                        npc.spriteDirection = npc.direction;
                    }
                }
                if (npc.type == NPCID.DrManFly && !Main.eclipse)
                {
                    flag8 = true;
                }
                else if ((npc.ai[2] <= 0f || flag8) && (npc.velocity.Y == 0f || flag9) && npc.ai[1] <= 0f && !Main.player[npc.target].dead)
                {
                    bool flag13 = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
                    if (npc.type == NPCID.MartianWalker)
                    {
                        flag13 = Collision.CanHitLine(npc.Top + new Vector2(0f, 20f), 0, 0, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
                    }
                    if (Main.player[npc.target].stealth == 0f && Main.player[npc.target].itemAnimation == 0)
                    {
                        flag13 = false;
                    }
                    if (flag13)
                    {
                        float num77 = 10f;
                        Vector2 vector28 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                        float num79 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector28.X;
                        float num80 = Math.Abs(num79) * 0.1f;
                        float num81 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector28.Y - num80;
                        num79 += (float)Main.rand.Next(-40, 41);
                        num81 += (float)Main.rand.Next(-40, 41);
                        float num82 = (float)Math.Sqrt(num79 * num79 + num81 * num81);
                        float num83 = 700f;
                        if (npc.type == NPCID.PirateDeadeye)
                        {
                            num83 = 550f;
                        }
                        if (npc.type == NPCID.PirateCrossbower)
                        {
                            num83 = 800f;
                        }
                        if (npc.type >= NPCID.Salamander && npc.type <= NPCID.Salamander9)
                        {
                            num83 = 190f;
                        }
                        if (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4)
                        {
                            num83 = 200f;
                        }
                        if (npc.type == NPCID.GreekSkeleton)
                        {
                            num83 = 400f;
                        }
                        if (npc.type == NPCID.DrManFly)
                        {
                            num83 = 400f;
                        }
                        if (num82 < num83)
                        {
                            npc.netUpdate = true;
                            npc.velocity.X *= 0.5f;
                            num82 = num77 / num82;
                            num79 *= num82;
                            num81 *= num82;
                            npc.ai[2] = 3f;
                            npc.ai[1] = num64;
                            if (Math.Abs(num81) > Math.Abs(num79) * 2f)
                            {
                                if (num81 > 0f)
                                {
                                    npc.ai[2] = 1f;
                                }
                                else
                                {
                                    npc.ai[2] = 5f;
                                }
                            }
                            else if (Math.Abs(num79) > Math.Abs(num81) * 2f)
                            {
                                npc.ai[2] = 3f;
                            }
                            else if (num81 > 0f)
                            {
                                npc.ai[2] = 2f;
                            }
                            else
                            {
                                npc.ai[2] = 4f;
                            }
                        }
                    }
                }
                if (npc.ai[2] <= 0f || (flag8 && (num62 == -1 || !(npc.ai[1] >= (float)num62) || !(npc.ai[1] < (float)(num62 + num63)))))
                {
                    float num84 = 1f;
                    float num85 = 0.07f;
                    float num86 = 0.8f;
                    if (npc.type == NPCID.PirateDeadeye)
                    {
                        num84 = 2f;
                        num85 = 0.09f;
                    }
                    else if (npc.type == NPCID.PirateCrossbower)
                    {
                        num84 = 1.5f;
                        num85 = 0.08f;
                    }
                    else if (npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner)
                    {
                        num84 = 2f;
                        num85 = 0.5f;
                    }
                    else if (npc.type == NPCID.MartianWalker)
                    {
                        num84 = 4f;
                        num85 = 1f;
                        num86 = 0.7f;
                    }
                    else if (npc.type == NPCID.StardustSoldier)
                    {
                        num84 = 2f;
                        num85 = 0.5f;
                    }
                    else if (npc.type == NPCID.StardustSpiderBig)
                    {
                        num84 = 2f;
                        num85 = 0.5f;
                    }
                    else if (npc.type == NPCID.VortexHornetQueen)
                    {
                        num84 = 4f;
                        num85 = 0.6f;
                        num86 = 0.95f;
                    }
                    bool flag14 = false;
                    if ((npc.type == NPCID.BrainScrambler || npc.type == NPCID.RayGunner) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 300f && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        flag14 = true;
                        npc.ai[3] = 0f;
                    }
                    if (npc.type == NPCID.MartianWalker && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 400f && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        flag14 = true;
                        npc.ai[3] = 0f;
                    }
                    if (npc.velocity.X < 0f - num84 || npc.velocity.X > num84 || flag14)
                    {
                        if (npc.velocity.Y == 0f)
                        {
                            npc.velocity *= num86;
                        }
                    }
                    else if (npc.velocity.X < num84 && npc.direction == 1)
                    {
                        npc.velocity.X += num85;
                        if (npc.velocity.X > num84)
                        {
                            npc.velocity.X = num84;
                        }
                    }
                    else if (npc.velocity.X > 0f - num84 && npc.direction == -1)
                    {
                        npc.velocity.X -= num85;
                        if (npc.velocity.X < 0f - num84)
                        {
                            npc.velocity.X = 0f - num84;
                        }
                    }
                }
                if (npc.type == NPCID.MartianWalker)
                {
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] >= 6f)
                    {
                        npc.localAI[2] = 0f;
                        npc.localAI[3] = Main.player[npc.target].DirectionFrom(npc.Top + new Vector2(0f, 20f)).ToRotation();
                    }
                }
            }
            if (npc.type == NPCID.Clown && Main.netMode != NetmodeID.MultiplayerClient && !Main.player[npc.target].dead)
            {
                if (npc.justHit)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] > 60f)
                {
                    Vector2 vector29 = new Vector2(npc.position.X + (float)npc.width * 0.5f - (float)(npc.direction * 24), npc.position.Y + 4f);
                    if (!Main.rand.NextBool(5) || NPC.AnyNPCs(378))
                    {
                        int num87 = Main.rand.Next(3, 8) * npc.direction;
                        int num88 = Main.rand.Next(-8, -5);
                        int num90 = Projectile.NewProjectile(npc.GetSource_FromThis(), vector29.X, vector29.Y, num87, num88, 75, 80, 0f, Main.myPlayer);
                        Main.projectile[num90].timeLeft = 300;
                        npc.ai[2] = 0f;
                    }
                    else
                    {
                        npc.ai[2] = -120f;
                        int number = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector29.X, (int)vector29.Y, 378);
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, number);
                    }
                }
            }
            if (npc.velocity.Y == 0f || flag)
            {
                int num91 = (int)(npc.position.Y + (float)npc.height + 7f) / 16;
                int num92 = (int)(npc.position.Y - 9f) / 16;
                int num93 = (int)npc.position.X / 16;
                int num94 = (int)(npc.position.X + (float)npc.width) / 16;
                int num205 = (int)(npc.position.X + 8f) / 16;
                int num95 = (int)(npc.position.X + (float)npc.width - 8f) / 16;
                bool flag15 = false;
                for (int num96 = num205; num96 <= num95; num96++)
                {
                    if (num96 >= num93 && num96 <= num94 && Main.tile[num96, num91] == null)
                    {
                        flag15 = true;
                        continue;
                    }
                    if (Main.tile[num96, num92] != null && Main.tile[num96, num92].HasUnactuatedTile && Main.tileSolid[Main.tile[num96, num92].TileType])
                    {
                        flag23 = false;
                        break;
                    }
                    if (!flag15 && num96 >= num93 && num96 <= num94 && Main.tile[num96, num91].HasUnactuatedTile && Main.tileSolid[Main.tile[num96, num91].TileType])
                    {
                        flag23 = true;
                    }
                }
                if (!flag23 && npc.velocity.Y < 0f)
                {
                    npc.velocity.Y = 0f;
                }
                if (flag15)
                {
                    return;
                }
            }
            if (npc.type == NPCID.VortexLarva)
            {
                flag23 = false;
            }
            if (npc.velocity.Y >= 0f && (npc.type != NPCID.WalkingAntlion || npc.directionY != 1))
            {
                int num97 = 0;
                if (npc.velocity.X < 0f)
                {
                    num97 = -1;
                }
                if (npc.velocity.X > 0f)
                {
                    num97 = 1;
                }
                Vector2 vector30 = npc.position;
                vector30.X += npc.velocity.X;
                int num98 = (int)((vector30.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * num97)) / 16f);
                int num100 = (int)((vector30.Y + (float)npc.height - 1f) / 16f);
                if (WorldGen.InWorld(num98, num100, 4))
                {
                    if (Main.tile[num98, num100] == null)
                    {
                        Main.tile[num98, num100].CopyFrom(default);
                    }
                    if (Main.tile[num98, num100 - 1] == null)
                    {
                        Main.tile[num98, num100 - 1].CopyFrom(default);
                    }
                    if (Main.tile[num98, num100 - 2] == null)
                    {
                        Main.tile[num98, num100 - 2].CopyFrom(default);
                    }
                    if (Main.tile[num98, num100 - 3] == null)
                    {
                        Main.tile[num98, num100 - 3].CopyFrom(default);
                    }
                    if (Main.tile[num98, num100 + 1] == null)
                    {
                        Main.tile[num98, num100 + 1].CopyFrom(default);
                    }
                    if (Main.tile[num98 - num97, num100 - 3] == null)
                    {
                        Main.tile[num98 - num97, num100 - 3].CopyFrom(default);
                    }
                    if ((float)(num98 * 16) < vector30.X + (float)npc.width && (float)(num98 * 16 + 16) > vector30.X && ((Main.tile[num98, num100].HasUnactuatedTile && !Main.tile[num98, num100].TopSlope && !Main.tile[num98, num100 - 1].TopSlope && Main.tileSolid[Main.tile[num98, num100].TileType] && !Main.tileSolidTop[Main.tile[num98, num100].TileType]) || (Main.tile[num98, num100 - 1].IsHalfBlock && Main.tile[num98, num100 - 1].HasUnactuatedTile)) && (!Main.tile[num98, num100 - 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[num98, num100 - 1].TileType] || Main.tileSolidTop[Main.tile[num98, num100 - 1].TileType] || (Main.tile[num98, num100 - 1].IsHalfBlock && (!Main.tile[num98, num100 - 4].HasUnactuatedTile || !Main.tileSolid[Main.tile[num98, num100 - 4].TileType] || Main.tileSolidTop[Main.tile[num98, num100 - 4].TileType]))) && (!Main.tile[num98, num100 - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[num98, num100 - 2].TileType] || Main.tileSolidTop[Main.tile[num98, num100 - 2].TileType]) && (!Main.tile[num98, num100 - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[num98, num100 - 3].TileType] || Main.tileSolidTop[Main.tile[num98, num100 - 3].TileType]) && (!Main.tile[num98 - num97, num100 - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[num98 - num97, num100 - 3].TileType]))
                    {
                        float num101 = num100 * 16;
                        if (Main.tile[num98, num100].IsHalfBlock)
                        {
                            num101 += 8f;
                        }
                        if (Main.tile[num98, num100 - 1].IsHalfBlock)
                        {
                            num101 -= 8f;
                        }
                        if (num101 < vector30.Y + (float)npc.height)
                        {
                            float num102 = vector30.Y + (float)npc.height - num101;
                            float num103 = 16.1f;
                            if (npc.type == NPCID.BlackRecluse || npc.type == NPCID.WallCreeper || npc.type == NPCID.JungleCreeper || npc.type == NPCID.BloodCrawler || npc.type == NPCID.DesertScorpionWalk)
                            {
                                num103 += 8f;
                            }
                            if (num102 <= num103)
                            {
                                npc.gfxOffY += npc.position.Y + (float)npc.height - num101;
                                npc.position.Y = num101 - (float)npc.height;
                                if (num102 < 9f)
                                {
                                    npc.stepSpeed = 1f;
                                }
                                else
                                {
                                    npc.stepSpeed = 2f;
                                }
                            }
                        }
                    }
                }
            }
            if (flag23)
            {
                int num104 = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f);
                int num105 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
                if (npc.type == NPCID.Clown || npc.type == NPCID.BlackRecluse || npc.type == NPCID.WallCreeper || npc.type == NPCID.LihzahrdCrawler || npc.type == NPCID.JungleCreeper || npc.type == NPCID.BloodCrawler || npc.type == NPCID.AnomuraFungus || npc.type == NPCID.MushiLadybug || npc.type == NPCID.Paladin || npc.type == NPCID.Scutlix || npc.type == NPCID.VortexRifleman || npc.type == NPCID.VortexHornet || npc.type == NPCID.VortexHornetQueen || npc.type == NPCID.WalkingAntlion || npc.type == NPCID.GiantWalkingAntlion || npc.type == NPCID.SolarDrakomire || npc.type == NPCID.DesertScorpionWalk || npc.type == NPCID.DesertBeast || npc.type == NPCID.LarvaeAntlion)
                {
                    num104 = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f);
                }
                if (Main.tile[num104, num105] == null)
                {
                    Main.tile[num104, num105].CopyFrom(default);
                }
                if (Main.tile[num104, num105 - 1] == null)
                {
                    Main.tile[num104, num105 - 1].CopyFrom(default);
                }
                if (Main.tile[num104, num105 - 2] == null)
                {
                    Main.tile[num104, num105 - 2].CopyFrom(default);
                }
                if (Main.tile[num104, num105 - 3] == null)
                {
                    Main.tile[num104, num105 - 3].CopyFrom(default);
                }
                if (Main.tile[num104, num105 + 1] == null)
                {
                    Main.tile[num104, num105 + 1].CopyFrom(default);
                }
                if (Main.tile[num104 + npc.direction, num105 - 1] == null)
                {
                    Main.tile[num104 + npc.direction, num105 - 1].CopyFrom(default);
                }
                if (Main.tile[num104 + npc.direction, num105 + 1] == null)
                {
                    Main.tile[num104 + npc.direction, num105 + 1].CopyFrom(default);
                }
                if (Main.tile[num104 - npc.direction, num105 + 1] == null)
                {
                    Main.tile[num104 - npc.direction, num105 + 1].CopyFrom(default);
                }
                if (Main.tile[num104, num105 - 1].HasUnactuatedTile && (TileLoader.IsClosedDoor(Main.tile[num104, num105 - 1]) || Main.tile[num104, num105 - 1].TileType == 388) && flag26)
                {
                    npc.ai[2] += 1f;
                    npc.ai[3] = 0f;
                    if (npc.ai[2] >= 60f)
                    {
                        bool flag16 = npc.type == NPCID.Zombie || npc.type == NPCID.ArmedZombie || npc.type == NPCID.TorchZombie || npc.type == NPCID.ZombieXmas || npc.type == NPCID.ZombieSweater || npc.type == NPCID.BaldZombie || npc.type == NPCID.ZombieEskimo || npc.type == NPCID.PincushionZombie || npc.type == NPCID.SlimedZombie || npc.type == NPCID.SwampZombie || npc.type == NPCID.TwiggyZombie || npc.type == NPCID.FemaleZombie || npc.type == NPCID.ZombieRaincoat || npc.type == NPCID.ZombieSuperman || npc.type == NPCID.ZombiePixie || npc.type == NPCID.ZombieDoctor || npc.type == NPCID.Skeleton || npc.type == NPCID.SkeletonAlien || npc.type == NPCID.SkeletonAstonaut || npc.type == NPCID.SkeletonTopHat || npc.type == NPCID.UndeadMiner || npc.type == NPCID.Nymph || npc.type == NPCID.UndeadViking || npc.type == NPCID.ArmoredSkeleton || npc.type == NPCID.ArmoredViking || npc.type == NPCID.MisassembledSkeleton || npc.type == NPCID.PantlessSkeleton || npc.type == NPCID.BoneThrowingSkeleton || npc.type == NPCID.BoneThrowingSkeleton2 || npc.type == NPCID.BoneThrowingSkeleton3 || npc.type == NPCID.BoneThrowingSkeleton4 || npc.type == NPCID.GreekSkeleton || npc.type == NPCID.HeadacheSkeleton || npc.type == NPCID.SporeSkeleton;
                        bool flag17 = Main.player[npc.target].ZoneGraveyard && Main.rand.NextBool(60);
                        if ((!Main.bloodMoon || Main.getGoodWorld) && !flag17 && flag16)
                        {
                            npc.ai[1] = 0f;
                        }
                        npc.velocity.X = 0.5f * (float)(-npc.direction);
                        int num106 = 5;
                        if (Main.tile[num104, num105 - 1].TileType == 388)
                        {
                            num106 = 2;
                        }
                        npc.ai[1] += num106;
                        if (npc.type == NPCID.GoblinThief)
                        {
                            npc.ai[1] += 1f;
                        }
                        if (npc.type == NPCID.AngryBones || npc.type == NPCID.AngryBonesBig || npc.type == NPCID.AngryBonesBigMuscle || npc.type == NPCID.AngryBonesBigHelmet)
                        {
                            npc.ai[1] += 6f;
                        }
                        npc.ai[2] = 0f;
                        bool flag18 = false;
                        if (npc.ai[1] >= 10f)
                        {
                            flag18 = true;
                            npc.ai[1] = 10f;
                        }
                        if (npc.type == NPCID.Butcher)
                        {
                            flag18 = true;
                        }
                        WorldGen.KillTile(num104, num105 - 1, fail: true);
                        if ((Main.netMode != NetmodeID.MultiplayerClient || !flag18) && flag18 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (npc.type == NPCID.GoblinPeon)
                            {
                                WorldGen.KillTile(num104, num105 - 1);
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, num104, num105 - 1);
                                }
                            }
                            else
                            {
                                if (TileLoader.IsClosedDoor(Main.tile[num104, num105 - 1]))
                                {
                                    bool flag19 = WorldGen.OpenDoor(num104, num105 - 1, npc.direction);
                                    if (!flag19)
                                    {
                                        npc.ai[3] = num154;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == NetmodeID.Server && flag19)
                                    {
                                        NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, num104, num105 - 1, npc.direction);
                                    }
                                }
                                if (Main.tile[num104, num105 - 1].TileType == 388)
                                {
                                    bool flag20 = WorldGen.ShiftTallGate(num104, num105 - 1, closing: false);
                                    if (!flag20)
                                    {
                                        npc.ai[3] = num154;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == NetmodeID.Server && flag20)
                                    {
                                        NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 4, num104, num105 - 1);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    int num107 = npc.spriteDirection;
                    if (npc.type == NPCID.VortexRifleman)
                    {
                        num107 *= -1;
                    }
                    if ((npc.velocity.X < 0f && num107 == -1) || (npc.velocity.X > 0f && num107 == 1))
                    {
                        if (npc.height >= 32 && Main.tile[num104, num105 - 2].HasUnactuatedTile && Main.tileSolid[Main.tile[num104, num105 - 2].TileType])
                        {
                            if (Main.tile[num104, num105 - 3].HasUnactuatedTile && Main.tileSolid[Main.tile[num104, num105 - 3].TileType])
                            {
                                npc.velocity.Y = -8f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.velocity.Y = -7f;
                                npc.netUpdate = true;
                            }
                        }
                        else if (Main.tile[num104, num105 - 1].HasUnactuatedTile && Main.tileSolid[Main.tile[num104, num105 - 1].TileType])
                        {
                            if (npc.type == NPCID.Gnome)
                            {
                                npc.velocity.Y = -8f;
                                int num108 = (int)(npc.position.Y + (float)npc.height) / 16;
                                if (WorldGen.SolidTile((int)npc.Center.X / 16, num108 - 8))
                                {
                                    npc.direction *= -1;
                                    npc.spriteDirection = npc.direction;
                                    npc.velocity.X = 3 * npc.direction;
                                }
                            }
                            else
                            {
                                npc.velocity.Y = -6f;
                            }
                            npc.netUpdate = true;
                        }
                        else if (npc.position.Y + (float)npc.height - (float)(num105 * 16) > 20f && Main.tile[num104, num105].HasUnactuatedTile && !Main.tile[num104, num105].TopSlope && Main.tileSolid[Main.tile[num104, num105].TileType])
                        {
                            npc.velocity.Y = -5f;
                            npc.netUpdate = true;
                        }
                        else if (npc.directionY < 0 && npc.type != NPCID.Crab && (!Main.tile[num104, num105 + 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[num104, num105 + 1].TileType]) && (!Main.tile[num104 + npc.direction, num105 + 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[num104 + npc.direction, num105 + 1].TileType]))
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X *= 1.5f;
                            npc.netUpdate = true;
                        }
                        else if (flag26)
                        {
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                        }
                        if (npc.velocity.Y == 0f && flag24 && npc.ai[3] == 1f)
                        {
                            npc.velocity.Y = -5f;
                        }
                        if (npc.velocity.Y == 0f && (Main.expertMode || npc.type == NPCID.ZombieMerman) && Main.player[npc.target].Bottom.Y < npc.Top.Y && Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < (float)(Main.player[npc.target].width * 3) && Collision.CanHit(npc, Main.player[npc.target]))
                        {
                            if (npc.type == NPCID.ZombieMerman)
                            {
                                int num109 = (int)((npc.Bottom.Y - 16f - Main.player[npc.target].Bottom.Y) / 16f);
                                if (num109 < 14 && Collision.CanHit(npc, Main.player[npc.target]))
                                {
                                    if (num109 < 7)
                                    {
                                        npc.velocity.Y = -8.8f;
                                    }
                                    else if (num109 < 8)
                                    {
                                        npc.velocity.Y = -9.2f;
                                    }
                                    else if (num109 < 9)
                                    {
                                        npc.velocity.Y = -9.7f;
                                    }
                                    else if (num109 < 10)
                                    {
                                        npc.velocity.Y = -10.3f;
                                    }
                                    else if (num109 < 11)
                                    {
                                        npc.velocity.Y = -10.6f;
                                    }
                                    else
                                    {
                                        npc.velocity.Y = -11f;
                                    }
                                }
                            }
                            if (npc.velocity.Y == 0f)
                            {
                                int num112 = 6;
                                if (Main.player[npc.target].Bottom.Y > npc.Top.Y - (float)(num112 * 16))
                                {
                                    npc.velocity.Y = -7.9f;
                                }
                                else
                                {
                                    int num113 = (int)(npc.Center.X / 16f);
                                    int num114 = (int)(npc.Bottom.Y / 16f) - 1;
                                    for (int num115 = num114; num115 > num114 - num112; num115--)
                                    {
                                        if (Main.tile[num113, num115].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[num113, num115].TileType])
                                        {
                                            npc.velocity.Y = -7.9f;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if ((npc.type == NPCID.AngryBones || npc.type == NPCID.AngryBonesBig || npc.type == NPCID.AngryBonesBigMuscle || npc.type == NPCID.AngryBonesBigHelmet || npc.type == NPCID.CorruptBunny || npc.type == NPCID.ArmoredSkeleton || npc.type == NPCID.Werewolf || npc.type == NPCID.CorruptPenguin || npc.type == NPCID.Nymph || npc.type == NPCID.GrayGrunt || npc.type == NPCID.GigaZapper || npc.type == NPCID.CrimsonBunny || npc.type == NPCID.CrimsonPenguin || (npc.type >= NPCID.DesertGhoul && npc.type <= NPCID.DesertGhoulHallow)) && npc.velocity.Y == 0f)
                    {
                        int num116 = 100;
                        int num117 = 50;
                        if (npc.type == NPCID.ZombieMerman)
                        {
                            num116 = 150;
                            num117 = 150;
                        }
                        if (Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < (float)num116 && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < (float)num117 && ((npc.direction > 0 && npc.velocity.X >= 1f) || (npc.direction < 0 && npc.velocity.X <= -1f)))
                        {
                            if (npc.type == NPCID.ZombieMerman)
                            {
                                npc.velocity.X += npc.direction;
                                npc.velocity.X *= 2f;
                                if (npc.velocity.X > 8f)
                                {
                                    npc.velocity.X = 8f;
                                }
                                if (npc.velocity.X < -8f)
                                {
                                    npc.velocity.X = -8f;
                                }
                                npc.velocity.Y = -4.5f;
                                if (npc.position.Y > Main.player[npc.target].position.Y + 40f)
                                {
                                    npc.velocity.Y -= 2f;
                                }
                                if (npc.position.Y > Main.player[npc.target].position.Y + 80f)
                                {
                                    npc.velocity.Y -= 2f;
                                }
                                if (npc.position.Y > Main.player[npc.target].position.Y + 120f)
                                {
                                    npc.velocity.Y -= 2f;
                                }
                            }
                            else
                            {
                                npc.velocity.X *= 2f;
                                if (npc.velocity.X > 3f)
                                {
                                    npc.velocity.X = 3f;
                                }
                                if (npc.velocity.X < -3f)
                                {
                                    npc.velocity.X = -3f;
                                }
                                npc.velocity.Y = -4f;
                            }
                            npc.netUpdate = true;
                        }
                    }
                    if (npc.type == NPCID.ChaosElemental && npc.velocity.Y < 0f)
                    {
                        npc.velocity.Y *= 1.1f;
                    }
                    if (npc.type == NPCID.BoneLee && npc.velocity.Y == 0f && Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < 150f && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < 50f && ((npc.direction > 0 && npc.velocity.X >= 1f) || (npc.direction < 0 && npc.velocity.X <= -1f)))
                    {
                        npc.velocity.X = 8 * npc.direction;
                        npc.velocity.Y = -4f;
                        npc.netUpdate = true;
                    }
                    if (npc.type == NPCID.BoneLee && npc.velocity.Y < 0f)
                    {
                        npc.velocity.X *= 1.2f;
                        npc.velocity.Y *= 1.1f;
                    }
                    if (npc.type == NPCID.Butcher && npc.velocity.Y < 0f)
                    {
                        npc.velocity.X *= 1.3f;
                        npc.velocity.Y *= 1.1f;
                    }
                }
            }
            else if (flag26)
            {
                npc.ai[1] = 0f;
                npc.ai[2] = 0f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.ChaosElemental && npc.ai[3] >= (float)num154)
            {
                int targetTileX = (int)Main.player[npc.target].Center.X / 16;
                int targetTileY = (int)Main.player[npc.target].Center.Y / 16;
                Vector2 chosenTile = Vector2.Zero;
                if (npc.AI_AttemptToFindTeleportSpot(ref chosenTile, targetTileX, targetTileY, 20, 9))
                {
                    npc.position.X = chosenTile.X * 16f - (float)(npc.width / 2);
                    npc.position.Y = chosenTile.Y * 16f - (float)npc.height;
                    npc.ai[3] = -120f;
                    npc.netUpdate = true;
                }
            }
        }
        private static void AI_015_KingSlime(NPC npc)
        {
            float num759 = 1f;
            float num760 = 1f;
            bool flag68 = false;
            bool flag79 = false;
            bool flag90 = false;
            float num761 = 2f;
            if (Main.getGoodWorld)
            {
                num761 -= 1f - (float)npc.life / (float)npc.lifeMax;
                num760 *= num761;
            }
            npc.aiAction = 0;
            if (npc.ai[3] == 0f && npc.life > 0)
            {
                npc.ai[3] = npc.lifeMax;
            }
            if (npc.localAI[3] == 0f)
            {
                npc.localAI[3] = 1f;
                flag68 = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[0] = -100f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }
            int num762 = 3000;
            if (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > (float)num762)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > (float)num762)
                {
                    npc.EncourageDespawn(10);
                    if (Main.player[npc.target].Center.X < npc.Center.X)
                    {
                        npc.direction = 1;
                    }
                    else
                    {
                        npc.direction = -1;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] != 5f)
                    {
                        npc.netUpdate = true;
                        npc.ai[2] = 0f;
                        npc.ai[0] = 0f;
                        npc.ai[1] = 5f;
                        npc.localAI[1] = Main.maxTilesX * 16;
                        npc.localAI[2] = Main.maxTilesY * 16;
                    }
                }
            }
            if (!Main.player[npc.target].dead && npc.timeLeft > 10 && npc.ai[2] >= 300f && npc.ai[1] < 5f && npc.velocity.Y == 0f)
            {
                npc.ai[2] = 0f;
                npc.ai[0] = 0f;
                npc.ai[1] = 5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.TargetClosest(faceTarget: false);
                    Point point10 = npc.Center.ToTileCoordinates();
                    Point point11 = Main.player[npc.target].Center.ToTileCoordinates();
                    Vector2 vector224 = Main.player[npc.target].Center - npc.Center;
                    int num764 = 10;
                    int num765 = 0;
                    int num766 = 7;
                    int num767 = 0;
                    bool flag101 = false;
                    if (npc.localAI[0] >= 360f || vector224.Length() > 2000f)
                    {
                        if (npc.localAI[0] >= 360f)
                        {
                            npc.localAI[0] = 360f;
                        }
                        flag101 = true;
                        num767 = 100;
                    }
                    while (!flag101 && num767 < 100)
                    {
                        num767++;
                        int num768 = Main.rand.Next(point11.X - num764, point11.X + num764 + 1);
                        int num769 = Main.rand.Next(point11.Y - num764, point11.Y + 1);
                        if ((num769 >= point11.Y - num766 && num769 <= point11.Y + num766 && num768 >= point11.X - num766 && num768 <= point11.X + num766) || (num769 >= point10.Y - num765 && num769 <= point10.Y + num765 && num768 >= point10.X - num765 && num768 <= point10.X + num765) || Main.tile[num768, num769].HasUnactuatedTile)
                        {
                            continue;
                        }
                        int num770 = num769;
                        int num771 = 0;
                        if (Main.tile[num768, num770].HasUnactuatedTile && Main.tileSolid[Main.tile[num768, num770].TileType] && !Main.tileSolidTop[Main.tile[num768, num770].TileType])
                        {
                            num771 = 1;
                        }
                        else
                        {
                            for (; num771 < 150 && num770 + num771 < Main.maxTilesY; num771++)
                            {
                                int num772 = num770 + num771;
                                if (Main.tile[num768, num772].HasUnactuatedTile && Main.tileSolid[Main.tile[num768, num772].TileType] && !Main.tileSolidTop[Main.tile[num768, num772].TileType])
                                {
                                    num771--;
                                    break;
                                }
                            }
                        }
                        num769 += num771;
                        bool flag2 = true;
                        if (flag2 && Main.tile[num768, num769].LiquidType == LiquidID.Lava)
                        {
                            flag2 = false;
                        }
                        if (flag2 && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                        {
                            flag2 = false;
                        }
                        if (flag2)
                        {
                            npc.localAI[1] = num768 * 16 + 8;
                            npc.localAI[2] = num769 * 16 + 16;
                            flag101 = true;
                            break;
                        }
                    }
                    if (num767 >= 100)
                    {
                        Vector2 bottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
                        npc.localAI[1] = bottom.X;
                        npc.localAI[2] = bottom.Y;
                    }
                }
            }
            if (!Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0) || Math.Abs(npc.Top.Y - Main.player[npc.target].Bottom.Y) > 160f)
            {
                npc.ai[2]++;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.localAI[0]++;
                }
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[0]--;
                if (npc.localAI[0] < 0f)
                {
                    npc.localAI[0] = 0f;
                }
            }
            if (npc.timeLeft < 10 && (npc.ai[0] != 0f || npc.ai[1] != 0f))
            {
                npc.ai[0] = 0f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
                flag79 = false;
            }
            Dust dust37;
            Dust dust87;
            if (npc.ai[1] == 5f)
            {
                flag79 = true;
                npc.aiAction = 1;
                npc.ai[0]++;
                num759 = MathHelper.Clamp((60f - npc.ai[0]) / 60f, 0f, 1f);
                num759 = 0.5f + num759 * 0.5f;
                if (npc.ai[0] >= 60f)
                {
                    flag90 = true;
                }
                if (npc.ai[0] == 60f)
                {
                    Gore.NewGore(npc.GetSource_FromThis(), npc.Center + new Vector2(-40f, -npc.height / 2), npc.velocity, 734);
                }
                if (npc.ai[0] >= 60f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Bottom = new Vector2(npc.localAI[1], npc.localAI[2]);
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                }
                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 120f)
                {
                    npc.ai[1] = 6f;
                    npc.ai[0] = 0f;
                }
                if (!flag90)
                {
                    for (int num773 = 0; num773 < 10; num773++)
                    {
                        int num775 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                        Main.dust[num775].noGravity = true;
                        dust37 = Main.dust[num775];
                        dust87 = dust37;
                        dust87.velocity *= 0.5f;
                    }
                }
            }
            else if (npc.ai[1] == 6f)
            {
                flag79 = true;
                npc.aiAction = 0;
                npc.ai[0]++;
                num759 = MathHelper.Clamp(npc.ai[0] / 30f, 0f, 1f);
                num759 = 0.5f + num759 * 0.5f;
                if (npc.ai[0] >= 30f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                    npc.TargetClosest();
                }
                if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;
                    npc.TargetClosest();
                }
                for (int num776 = 0; num776 < 10; num776++)
                {
                    int num777 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                    Main.dust[num777].noGravity = true;
                    dust37 = Main.dust[num777];
                    dust87 = dust37;
                    dust87.velocity *= 2f;
                }
            }
            npc.dontTakeDamage = (npc.hide = flag90);
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.8f;
                if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                {
                    npc.velocity.X = 0f;
                }
                if (!flag79)
                {
                    npc.ai[0] += 2f;
                    if ((double)npc.life < (double)npc.lifeMax * 0.8)
                    {
                        npc.ai[0] += 1f;
                    }
                    if ((double)npc.life < (double)npc.lifeMax * 0.6)
                    {
                        npc.ai[0] += 1f;
                    }
                    if ((double)npc.life < (double)npc.lifeMax * 0.4)
                    {
                        npc.ai[0] += 2f;
                    }
                    if ((double)npc.life < (double)npc.lifeMax * 0.2)
                    {
                        npc.ai[0] += 3f;
                    }
                    if ((double)npc.life < (double)npc.lifeMax * 0.1)
                    {
                        npc.ai[0] += 4f;
                    }
                    if (npc.ai[0] >= 0f)
                    {
                        npc.netUpdate = true;
                        npc.TargetClosest();
                        if (npc.ai[1] == 3f)
                        {
                            npc.velocity.Y = -13f;
                            npc.velocity.X += 3.5f * (float)npc.direction;
                            npc.ai[0] = -200f;
                            npc.ai[1] = 0f;
                        }
                        else if (npc.ai[1] == 2f)
                        {
                            npc.velocity.Y = -6f;
                            npc.velocity.X += 4.5f * (float)npc.direction;
                            npc.ai[0] = -120f;
                            npc.ai[1] += 1f;
                        }
                        else
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X += 4f * (float)npc.direction;
                            npc.ai[0] = -120f;
                            npc.ai[1] += 1f;
                        }
                    }
                    else if (npc.ai[0] >= -30f)
                    {
                        npc.aiAction = 1;
                    }
                }
            }
            else if (npc.target < 255)
            {
                float num778 = 3f;
                if (Main.getGoodWorld)
                {
                    num778 = 6f;
                }
                if ((npc.direction == 1 && npc.velocity.X < num778) || (npc.direction == -1 && npc.velocity.X > 0f - num778))
                {
                    if ((npc.direction == -1 && (double)npc.velocity.X < 0.1) || (npc.direction == 1 && (double)npc.velocity.X > -0.1))
                    {
                        npc.velocity.X += 0.2f * (float)npc.direction;
                    }
                    else
                    {
                        npc.velocity.X *= 0.93f;
                    }
                }
            }
            int num779 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, 255, new Color(0, 80, 255, 80), npc.scale * 1.2f);
            Main.dust[num779].noGravity = true;
            dust37 = Main.dust[num779];
            dust87 = dust37;
            dust87.velocity *= 0.5f;
            if (npc.life <= 0)
            {
                return;
            }
            float num780 = (float)npc.life / (float)npc.lifeMax;
            num780 = num780 * 0.5f + 0.75f;
            num780 *= num759;
            num780 *= num760;
            if (num780 != npc.scale || flag68)
            {
                npc.position.X += npc.width / 2;
                npc.position.Y += npc.height;
                // --------------------------------------------------------------- HERE
                npc.scale = num780 * CustomizedNPC.GetScaleMultiplier(npc);
                // --------------------------------------------------------------- HERE
                npc.width = (int)(98f * npc.scale);
                npc.height = (int)(92f * npc.scale);
                npc.position.X -= npc.width / 2;
                npc.position.Y -= npc.height;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int num781 = (int)((double)npc.lifeMax * 0.05);
            if (!((float)(npc.life + num781) < npc.ai[3]))
            {
                return;
            }
            npc.ai[3] = npc.life;
            int num782 = Main.rand.Next(1, 4);
            for (int num783 = 0; num783 < num782; num783++)
            {
                int x = (int)(npc.position.X + (float)Main.rand.Next(npc.width - 32));
                int y = (int)(npc.position.Y + (float)Main.rand.Next(npc.height - 32));
                int num784 = 1;
                if (Main.expertMode && Main.rand.NextBool(4))
                {
                    num784 = 535;
                }
                int num786 = NPC.NewNPC(npc.GetSource_FromThis(), x, y, num784);
                Main.npc[num786].SetDefaults(num784);
                Main.npc[num786].velocity.X = (float)Main.rand.Next(-15, 16) * 0.1f;
                Main.npc[num786].velocity.Y = (float)Main.rand.Next(-30, 1) * 0.1f;
                Main.npc[num786].ai[0] = -1000 * Main.rand.Next(3);
                Main.npc[num786].ai[1] = 0f;
                if (Main.netMode == NetmodeID.Server && num786 < 200)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num786);
                }
            }
        }
        private static void AI_070_DukeFishronBubble(NPC npc)
        {
            if (npc.target == 255)
            {
                npc.TargetClosest();
                npc.ai[3] = (float)Main.rand.Next(80, 121) / 100f;
                float num70 = (float)Main.rand.Next(165, 265) / 15f;
                npc.velocity = Vector2.Normalize(Main.player[npc.target].Center - npc.Center + new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101))) * num70;
                npc.netUpdate = true;
            }
            Vector2 vector41 = Vector2.Normalize(Main.player[npc.target].Center - npc.Center);
            npc.velocity = (npc.velocity * 40f + vector41 * 20f) / 41f;
            npc.scale = npc.ai[3];
            npc.alpha -= 30;
            if (npc.alpha < 50)
            {
                npc.alpha = 50;
            }
            npc.alpha = 50;
            npc.velocity.X = (npc.velocity.X * 50f + Main.windSpeedCurrent * 2f + (float)Main.rand.Next(-10, 11) * 0.1f) / 51f;
            npc.velocity.Y = (npc.velocity.Y * 50f + -0.25f + (float)Main.rand.Next(-10, 11) * 0.2f) / 51f;
            if (npc.velocity.Y > 0f)
            {
                npc.velocity.Y -= 0.04f;
            }
            if (npc.ai[0] == 0f)
            {
                int num71 = 40;
                Rectangle rect = npc.getRect();
                rect.X -= num71 + npc.width / 2;
                rect.Y -= num71 + npc.height / 2;
                rect.Width += num71 * 2;
                rect.Height += num71 * 2;
                for (int num72 = 0; num72 < 255; num72++)
                {
                    Player player8 = Main.player[num72];
                    if (player8.active && !player8.dead && rect.Intersects(player8.getRect()))
                    {
                        npc.ai[0] = 1f;
                        npc.ai[1] = 4f;
                        npc.netUpdate = true;
                        break;
                    }
                }
            }
            if (npc.ai[0] == 0f)
            {
                npc.ai[1]++;
                if (npc.ai[1] >= 150f)
                {
                    npc.ai[0] = 1f;
                    npc.ai[1] = 4f;
                }
            }
            if (npc.ai[0] == 1f)
            {
                npc.ai[1]--;
                if (npc.ai[1] <= 0f)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                    return;
                }
            }
            if (npc.justHit || npc.ai[0] == 1f)
            {
                npc.dontTakeDamage = true;
                npc.position = npc.Center;
                npc.width = (npc.height = 100);
                npc.position = new Vector2(npc.position.X - (float)(npc.width / 2), npc.position.Y - (float)(npc.height / 2));
                npc.EncourageDespawn(3);
            }
        }
        private static void AI_95_SmallStarCell(NPC npc)
        {
            float num544 = 300f;
            if (npc.velocity.Length() > 4f)
            {
                npc.velocity *= 0.95f;
            }
            npc.velocity *= 0.99f;
            npc.ai[0]++;
            float num545 = MathHelper.Clamp(npc.ai[0] / num544, 0f, 1f);
            // --------------------------------------------------------------- HERE
            npc.scale = 1f + 0.3f * num545 * CustomizedNPC.GetScaleMultiplier(npc);
            // --------------------------------------------------------------- HERE
            if (npc.ai[0] >= num544)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Transform(405);
                    npc.netUpdate = true;
                }
                return;
            }
            npc.rotation += npc.velocity.X * 0.1f;
            if (!(npc.ai[0] > 20f))
            {
                return;
            }
            Vector2 center33 = npc.Center;
            int num547 = (int)(npc.ai[0] / (num544 / 2f));
            for (int num548 = 0; num548 < num547 + 1; num548++)
            {
                if (!Main.rand.NextBool(2))
                {
                    int num549 = 226;
                    float num550 = 0.4f;
                    if (num548 % 2 == 1)
                    {
                        num549 = 226;
                        num550 = 0.65f;
                    }
                    Vector2 vector209 = center33 + ((float)Main.rand.NextDouble() * ((float)Math.PI * 2f)).ToRotationVector2() * (12f - (float)(num547 * 2));
                    int num551 = Dust.NewDust(vector209 - Vector2.One * 12f, 24, 24, num549, npc.velocity.X / 2f, npc.velocity.Y / 2f);
                    Dust dust24 = Main.dust[num551];
                    Dust dust87 = dust24;
                    dust87.position -= new Vector2(2f);
                    Main.dust[num551].velocity = Vector2.Normalize(center33 - vector209) * 1.5f * (10f - (float)num547 * 2f) / 10f;
                    Main.dust[num551].noGravity = true;
                    Main.dust[num551].scale = num550;
                    Main.dust[num551].customData = npc;
                }
            }
        }
        private static void AI_101_AncientDoom(NPC npc)
        {
            float num608 = 420f;
            float num609 = 120f;
            int num610 = 1;
            float value3 = 0f;
            float value4 = 1f;
            float num611 = 4f;
            bool flag109 = !(npc.ai[1] >= 0f) || !Main.npc[(int)npc.ai[0]].active;
            if (Main.npc[(int)npc.ai[0]].type == NPCID.CultistBoss)
            {
                if (Main.npc[(int)npc.ai[0]].life < Main.npc[(int)npc.ai[0]].lifeMax / 2)
                {
                    num610 = 2;
                }
                if (Main.npc[(int)npc.ai[0]].life < Main.npc[(int)npc.ai[0]].lifeMax / 4)
                {
                    num610 = 3;
                }
            }
            else
            {
                flag109 = true;
            }
            npc.ai[1] += num610;
            float num612 = npc.ai[1] / num609;
            num612 = MathHelper.Clamp(num612, 0f, 1f);
            npc.position = npc.Center;
            // --------------------------------------------------------------- HERE
            npc.scale = MathHelper.Lerp(value3, value4, num612) * CustomizedNPC.GetScaleMultiplier(npc);
            // --------------------------------------------------------------- HERE
            npc.Center = npc.position;
            npc.alpha = (int)(255f - num612 * 255f);
            if (Main.rand.NextBool(6))
            {
                Vector2 vector220 = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                Dust dust72 = Main.dust[Dust.NewDust(npc.Center - vector220 * 20f, 0, 0, DustID.Shadowflame)];
                dust72.noGravity = true;
                dust72.position = npc.Center - vector220 * Main.rand.Next(10, 21) * npc.scale;
                dust72.velocity = vector220.RotatedBy(1.5707963705062866) * 4f;
                dust72.scale = 0.5f + Main.rand.NextFloat();
                dust72.fadeIn = 0.5f;
            }
            if (Main.rand.NextBool(6))
            {
                Vector2 vector221 = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                Dust dust73 = Main.dust[Dust.NewDust(npc.Center - vector221 * 30f, 0, 0, DustID.Granite)];
                dust73.noGravity = true;
                dust73.position = npc.Center - vector221 * 20f * npc.scale;
                dust73.velocity = vector221.RotatedBy(-1.5707963705062866) * 2f;
                dust73.scale = 0.5f + Main.rand.NextFloat();
                dust73.fadeIn = 0.5f;
            }
            if (Main.rand.NextBool(6))
            {
                Vector2 vector222 = Vector2.UnitY.RotatedByRandom(6.2831854820251465);
                Dust dust74 = Main.dust[Dust.NewDust(npc.Center - vector222 * 30f, 0, 0, DustID.Granite)];
                dust74.position = npc.Center - vector222 * 20f * npc.scale;
                dust74.velocity = Vector2.Zero;
                dust74.scale = 0.5f + Main.rand.NextFloat();
                dust74.fadeIn = 0.5f;
                dust74.noLight = true;
            }
            npc.localAI[0] += (float)Math.PI / 60f;
            npc.localAI[1] = 0.25f + Vector2.UnitY.RotatedBy(npc.ai[1] * ((float)Math.PI * 2f) / 60f).Y * 0.25f;
            if (npc.ai[1] >= num608)
            {
                flag109 = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int num614 = 0; num614 < 4; num614++)
                    {
                        Vector2 vector225 = new Vector2(0f, 0f - num611).RotatedBy((float)Math.PI / 2f * (float)num614);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, vector225.X, vector225.Y, 593, npc.damage, 0f, Main.myPlayer);
                    }
                }
            }
            if (flag109)
            {
                npc.HitEffect(0, 9999.0);
                npc.active = false;
            }
        }
        private static void AI_106_DD2MysteriousPortal(NPC npc)
        {
            if (npc.alpha == 0)
            {
                Lighting.AddLight(npc.Center, 0.5f, 0.1f, 0.3f);
            }
            SlotId val;
            if (npc.ai[1] == 0f)
            {
                if (npc.localAI[0] == 0f)
                {
                    SoundEngine.PlaySound(in SoundID.DD2_EtherianPortalOpen, npc.Center);
                    float[] array6 = npc.localAI;
                    val = SlotId.Invalid;
                    array6[3] = val.ToFloat();
                }
                if (npc.localAI[0] > 150f)
                {
                    SoundEngine.TryGetActiveSound(SlotId.FromFloat(npc.localAI[3]), out var activeSound);
                    if (activeSound == null)
                    {
                        float[] array7 = npc.localAI;
                        val = SoundEngine.PlaySound(in SoundID.DD2_EtherianPortalIdleLoop, npc.Center);
                        array7[3] = val.ToFloat();
                    }
                }
                if (!DD2Event.EnemySpawningIsOnHold)
                {
                    npc.ai[0]++;
                }
                if (npc.ai[0] >= (float)DD2Event.LaneSpawnRate)
                {
                    if (npc.ai[0] >= (float)(DD2Event.LaneSpawnRate * 3))
                    {
                        npc.ai[0] = 0f;
                    }
                    npc.netUpdate = true;
                    if (Main.netMode != NetmodeID.MultiplayerClient && (int)npc.ai[0] % DD2Event.LaneSpawnRate == 0)
                    {
                        DD2Event.SpawnMonsterFromGate(npc.Bottom);
                        if (DD2Event.EnemySpawningIsOnHold)
                        {
                            npc.ai[0] += 1f;
                        }
                    }
                }
                npc.localAI[0]++;
                if (npc.localAI[0] > 180f)
                {
                    npc.localAI[0] = 180f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[0] >= 180f)
                {
                    if (NPC.AnyNPCs(548))
                    {
                        npc.dontTakeDamage = true;
                        return;
                    }
                    npc.ai[1] = 1f;
                    npc.ai[0] = 0f;
                    npc.dontTakeDamage = true;
                }
            }
            else if (npc.ai[1] == 1f)
            {
                npc.ai[0]++;
                // --------------------------------------------------------------- HERE
                npc.scale = MathHelper.Lerp(1f, 0.05f, Utils.GetLerpValue(500f, 600f, npc.ai[0], clamped: true)) * CustomizedNPC.GetScaleMultiplier(npc);
                // --------------------------------------------------------------- HERE
                SoundEngine.TryGetActiveSound(SlotId.FromFloat(npc.localAI[3]), out var activeSound2);
                if (activeSound2 == null)
                {
                    float[] array8 = npc.localAI;
                    val = SoundEngine.PlaySound(in SoundID.DD2_EtherianPortalIdleLoop, npc.Center);
                    array8[3] = val.ToFloat();
                }
                SoundEngine.TryGetActiveSound(SlotId.FromFloat(npc.localAI[3]), out var activeSound3);
                activeSound2 = activeSound3;
                if (activeSound2 != null)
                {
                    activeSound2.Volume = npc.scale;
                }
                if (npc.ai[0] >= 550f)
                {
                    npc.dontTakeDamage = false;
                    npc.life = 0;
                    npc.checkDead();
                    npc.netUpdate = true;
                    activeSound2?.Stop();
                }
            }
        }
    }
}
