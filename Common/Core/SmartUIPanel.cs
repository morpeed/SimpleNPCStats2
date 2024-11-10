using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ID;

namespace SimpleNPCStats2.Common.Core
{
    public class SmartUIPanel : SmartUIElement, ILoadable
    {
        public SmartUIPanel() : base()
        {
            SetPadding(10);
            leftClickSound = SoundID.MenuTick;
            mouseOverSound = SoundID.MenuTick;
        }

        private static Asset<Texture2D>[] _textures;
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            int index = HoverConditional ? 1 : 0;

            // Background
            Texture2D texture = _textures[index].Value;
            SmartRectangle source = texture.Bounds;
            source.Inflate(-2, -2);
            SmartRectangle target = GetDimensions().ToRectangle();
            target.Inflate(-2, -2);
            spriteBatch.Draw(texture, target, source, Color.White);

            // Border
            SNSHelper.Draw3x3Sprite(spriteBatch, _textures[index + 2].Value, GetDimensions().ToRectangle(), new Point(12, 12));
        }

        public void Load(Mod mod)
        {
            _textures =
                [
                    ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Slot_Overlay", AssetRequestMode.ImmediateLoad),
                    ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Slot_Back", AssetRequestMode.ImmediateLoad),
                    ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Slot_Front", AssetRequestMode.ImmediateLoad),
                    ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Slot_Selection", AssetRequestMode.ImmediateLoad)
                ];
        }

        public void Unload()
        {
            _textures = null;
        }
    }
}
