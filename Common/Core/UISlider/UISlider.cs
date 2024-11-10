using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using SimpleNPCStats2.Common.Config.UI.Core;
using SimpleNPCStats2.Common.Core;
using SimpleNPCStats2.Common.Core.UIField;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace SimpleNPCStats2.Common.Core.UISlider
{
    public abstract class UISlider<T> : SmartUIElement, IResetValue where T : IEquatable<T>
    {
        public T min;
        public T max;
        public virtual T Value { get; set; }
        public T DefaultValue { get; private set; }
        private T oldValue;

        public Color barColor = Color.White;
        private static Color gradientColorPrimary = Color.White;
        private static Color gradientColorSecondary = Color.Black;

        public event Action<T> OnValueChanged;

        public float Percent => GetPercent(Value);
        public float GetPercent(T value)
        {
            return (float)(Convert.ToSingle(value) - Convert.ToSingle(min)) / (float)(Convert.ToSingle(max) - Convert.ToSingle(min));
        }

        public bool Sliding { get; private set; }

        public UISlider(T defaultValue)
        {
            this.DefaultValue = defaultValue;
            Value = defaultValue;
            this.mouseOverSound = SoundID.MenuTick;
            this.leftMouseDownSound = SoundID.MenuTick;
        }

        public void SetGradient(Color a, Color b)
        {
            gradientColorPrimary = a;
            gradientColorSecondary = b;
        }

        public bool ResetValue(bool invoke)
        {
            if (!Value.Equals(DefaultValue))
            {
                Value = DefaultValue;
                if (invoke)
                {
                    OnValueChanged?.Invoke(Value);
                }
                return true;
            }
            return false;
        }

        public sealed override void SafeLeftMouseDown(UIMouseEvent evt, ref bool playSound)
        {
            if (IsMouseHovering)
            {
                Sliding = true;
            }
        }

        public sealed override void SafeLeftMouseUp(UIMouseEvent evt, ref bool playSound)
        {
            Sliding = false;
        }

        public sealed override void SafeUpdate(GameTime gametime)
        {
            if (Sliding)
            {
                Rectangle dim = GetDimensions().ToRectangle();

                dim.Inflate(-10, 0);

                float mouseX = MathHelper.Clamp(Main.mouseX, dim.Left, dim.Right);
                float percent = (mouseX - dim.Left) / dim.Width;

                Value = UpdateSliderValue(percent);
                if (!Value.Equals(oldValue))
                {
                    OnValueChanged?.Invoke(Value);
                }
                oldValue = Value;
            }

            if (IsMouseHovering || Sliding)
            {
                barColor = Color.Yellow;
            }
            else
            {
                barColor = Color.White;
            }
        }

        public abstract T UpdateSliderValue(float barPercent);


        protected sealed override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle dimensions = GetDimensions().ToRectangle();
            Texture2D texture = TextureAssets.ColorBar.Value;

            var edgeColor = Sliding ? Color.Gold : Color.White;

            SNSHelper.Draw3x3Sprite(spriteBatch, texture, dimensions, new Point(4, 4), edgeColor);

            int gradientCount = dimensions.Width - 8;

            for (int i = 0; i < gradientCount; i++)
            {
                Rectangle rect = Rectangle.Empty;
                rect.Width = 1;
                rect.Height = dimensions.Height - 8;
                rect.Offset(dimensions.Location + new Point(4 + i, 4));
                Color lerp = Color.Lerp(gradientColorPrimary, gradientColorSecondary, (float)i / gradientCount);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, lerp);
            }

            texture = TextureAssets.ColorSlider.Value;
            dimensions.Inflate(-10, 0);
            Vector2 pos = new Vector2(dimensions.Width * Percent, dimensions.Height / 2) + dimensions.Location.ToVector2();
            pos.X = Math.Clamp(pos.X, dimensions.Left, dimensions.Right);
            spriteBatch.Draw(texture, pos, null, Color.White, 0f, texture.Center(), 1f, SpriteEffects.None, 0f);

            //Utils.DrawBorderStringBig(spriteBatch, Value.ToString(), dimensions.Location.ToVector2() - Vector2.UnitY * 20, Color.Red);
        }
    }
}
