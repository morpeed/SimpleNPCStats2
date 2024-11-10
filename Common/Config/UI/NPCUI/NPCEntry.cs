using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimpleNPCStats2.Common.Core;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace SimpleNPCStats2.Common.Config.UI.NPCUI
{
    public class NPCEntry : SmartUIPanel
    {
        public int npcType;
        public int npcNetId;

        private Texture2D texture;
        private Color color;

        public readonly string name;
        public readonly string mod;

        private bool _active;
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                if (value)
                {
                    hoverStatus = UIHoverStatus.ForcedHover;
                }
                else
                {
                    hoverStatus = UIHoverStatus.MouseHovering;
                }
            }
        }

        public NPCEntry(int npcType)
        {
            var npc = ContentSamples.NpcsByNetId[npcType];
            this.npcType = npc.type;
            this.npcNetId = npc.netID;

            if (npc.color != default)
            {
                color = npc.color;
            }
            else
            {
                color = Color.White;
            }

            OverrideSamplerState = SamplerState.PointClamp;

            name = npc.TypeName;
            mod = npc.ModNPC == null ? Language.GetTextValue("RandomWorldName_Location.Terraria") : npc.ModNPC.Mod.DisplayNameClean;
            hoverText = $"{name} [ {mod} ]";
        }

        private bool _textureLoaded;
        public override void SafeUpdate(GameTime gametime)
        {
            if (!_textureLoaded && TextureAssets.Npc[npcType].IsLoaded)
            {
                texture = TextureAssets.Npc[npcType].Value;
                _textureLoaded = true;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (texture != null)
            {
                Rectangle rectangle = texture.Bounds;
                if (Main.npcFrameCount[npcType] > 0)
                {
                    rectangle = texture.Frame(verticalFrames: Main.npcFrameCount[npcType]);
                }

                CalculatedStyle inner = GetInnerDimensions();
                float scale = Math.Min(inner.Width, inner.Height) / Math.Max(rectangle.Width, rectangle.Height);

                spriteBatch.Draw(texture, GetDimensions().Center(), rectangle, color, 0f, new Vector2(rectangle.Width / 2, rectangle.Height / 2), scale, SpriteEffects.None, 0);
            }
        }

        public override int CompareTo(object obj)
        {
            NPCEntry other = (NPCEntry)obj;

            int comparison = npcType.CompareTo(other.npcType);
            if (comparison == 0)
            {
                comparison = npcNetId.CompareTo(other.npcNetId);
            }

            return comparison;
        }
    }
}
