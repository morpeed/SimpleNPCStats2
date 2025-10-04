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
        public static SimpleNPCStats2 Instance { get; private set; }
        public override void Load()
        {
            Instance = this;
        }

        public enum MessageType : byte
        {
            RequestManualSyncCustomizedNPCs
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType messageType = (MessageType)reader.ReadByte();

            switch (messageType)
            {
                case MessageType.RequestManualSyncCustomizedNPCs:

                    if (Main.netMode == NetmodeID.Server)
                    {
                        foreach (var npc in Main.ActiveNPCs)
                        {
                            if (npc.TryGetGlobalNPC<CustomizedNPC>(out var customizedNpc) && customizedNpc.Enabled)
                            {
                                var packet = GetPacket();
                                packet.Write((byte)MessageType.RequestManualSyncCustomizedNPCs);
                                packet.Write((byte)npc.whoAmI);
                                customizedNpc.Write(npc, packet);
                                packet.Send(whoAmI);
                            }
                        }
                    }
                    else
                    {
                        var npc2 = Main.npc[reader.ReadByte()];
                        if (npc2.TryGetGlobalNPC<CustomizedNPC>(out var customizedNpc2))
                        {
                            customizedNpc2.Read(npc2, reader);
                        }
                        else
                        {
                            var _ = new CustomizedNPC();
                            _.Read(new NPC(), reader);
                        }
                    }

                    break;

                default:
                    Logger.WarnFormat("SimpleNPCStats2: Unknown Message type: {0}", messageType);
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
                packet.Write((byte)SimpleNPCStats2.MessageType.RequestManualSyncCustomizedNPCs);
                packet.Send();
            }
        }
    }
}