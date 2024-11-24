using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.UI;
using Terraria.ModLoader.Config.UI;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria;
using System;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using SimpleNPCStats2.Common.Junk;
using SimpleNPCStats2.Common.Core;
using SimpleNPCStats2.Common.Core.UISlider;
using SimpleNPCStats2.Common.Config.UI.NPCUI;
using MonoMod.Core.Utils;
using System.Xml.Linq;
using Terraria.ModLoader.UI;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleNPCStats2.Common.Config.UI
{
    /// <summary>
    /// For future reference, OBJECT refers to the thing (class, struct etc) that this element is attached to :>
    /// Anyway...
    /// 
    /// </summary>
    public class ConfigDataElement : ConfigElement<ConfigData>
    {
        public UIElement Element {get; private set;}

        public static ConfigDataElement Instance { get; private set; }
        public void Save() => SetObject(GetObject());
        public ConfigData Data => (ConfigData)GetObject();

        public override void OnBind()
        {
            base.OnBind();

            Instance = this;
            Height.Set(550, 0);

            Element = new NPCGroups();
            Element.SetSize(0, 0, 1f, 1f);

            Append(Element);
        }

        public void SetElement(UIElement element)
        {
            RemoveAllChildren();

            Element = element;
            Element.SetSize(0, 0, 1f, 1f);
            Element.Recalculate();
            Element.Activate();

            Append(Element);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawChildren(spriteBatch);
        }
    }
}
