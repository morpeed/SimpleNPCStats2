using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace SimpleNPCStats2.Common
{
    /// <summary>
    /// IL modifications on large methods (i.e. vanilla AI) do not function, this finnicky workaround is required instead.
    /// Currently is only used to modify scale of a few NPCs with a dynamic scale
    /// King Slime, Firefly, Snail, Duke Fishron Bubble, Small Star Cell, Ancient Doom, DD2 Portal
    /// </summary>
    internal class NPCOverrides : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (npc.aiStyle == NPCAIStyleID.KingSlime || npc.aiStyle == NPCAIStyleID.DukeFishronBubble || npc.aiStyle == NPCAIStyleID.SmallStarCell || npc.aiStyle == NPCAIStyleID.AncientDoom || npc.aiStyle == NPCAIStyleID.DD2MysteriousPortal)
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
