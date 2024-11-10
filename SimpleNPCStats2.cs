using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using tModPorter;
using System.Reflection;

namespace SimpleNPCStats2
{
    public class SimpleNPCStats2 : Mod
    {
        public static bool ModsLoaded { get; private set; }
        public override void PostSetupContent()
        {
            ModsLoaded = true;
        }
    }
}