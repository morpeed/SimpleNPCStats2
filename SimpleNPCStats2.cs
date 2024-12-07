using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using tModPorter;
using System.Reflection;
using SimpleNPCStats2.Common;
using SimpleNPCStats2.Common.Config;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using Terraria.ModLoader.IO;
using System;
using Microsoft.Xna.Framework;

namespace SimpleNPCStats2
{
    public class SimpleNPCStats2 : Mod
    {
        public enum MessageType : byte
        {
            SetupCustomizedNPC
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType messageType = (MessageType)reader.ReadByte();

            switch (messageType)
            {
                case MessageType.SetupCustomizedNPC:

                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.SetupCustomizedNPC);

                        var netIds = new List<byte>();
                        foreach (var npc in Main.ActiveNPCs)
                        {
                            if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result) && result.TypeNetID < 0)
                            {
                                netIds.Add((byte)npc.whoAmI);
                                netIds.Add((byte)-result.TypeNetID);
                            }
                        }

                        packet.Write((byte)netIds.Count);
                        foreach (var netId in netIds)
                        {
                            packet.Write(netId);
                        }


                        packet.Send(whoAmI);
                    }
                    else
                    {
                        byte arrayLength = reader.ReadByte();
                        Dictionary<int, int> netIds = new Dictionary<int, int>();
                        for (int i = 0; i < arrayLength / 2; i++)
                        {
                            int npcWhoAmI = reader.ReadByte();
                            int netId = -reader.ReadByte();

                            netIds[npcWhoAmI] = netId;
                        }

                        foreach (var npc in Main.ActiveNPCs)
                        {
                            if (npc.TryGetGlobalNPC<CustomizedNPC>(out var result))
                            {
                                int type;
                                if (netIds.TryGetValue(npc.whoAmI, out var value))
                                {
                                    type = value;
                                }
                                else
                                {
                                    type = npc.type;
                                }

                                if (ConfigSystem.StaticNPCData.TryGetValue(type, out var value2))
                                {
                                    result.Setup(npc, value2);
                                }
                                else
                                {
                                    result.Setup(npc);
                                }
                            }
                        }
                    }

                    break;

                default:
                    Logger.WarnFormat("ExampleMod: Unknown Message type: {0}", messageType);
                    break;
            }
        }
    }

    public class SyncPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)SimpleNPCStats2.MessageType.SetupCustomizedNPC);
                packet.Send();
            }
        }
    }
}