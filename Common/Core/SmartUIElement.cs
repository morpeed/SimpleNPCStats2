using Microsoft.Xna.Framework;
using SimpleNPCStats2.Common.Junk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.UI;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleNPCStats2.Common.Core
{
    public enum UIHoverStatus
    {
        Unknown = -1,
        MouseHovering,
        ForcedHover,
        NoHover
    }

    /// <summary>
    /// UIElement with some extra features;
    /// </summary>
    public class SmartUIElement : UIElement
    {
        public string hoverText;
        public bool showHoverText = true;

        public bool muted;

        public SmartUIElement() : base() { }

        public UIHoverStatus hoverStatus = UIHoverStatus.MouseHovering;
        public bool HoverConditional
        {
            get
            {
                return hoverStatus switch
                {
                    UIHoverStatus.MouseHovering => IsMouseHovering,
                    UIHoverStatus.ForcedHover => true,
                    _ => false
                };
            }
        }

        public sealed override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SafeUpdate(gameTime);
        }
        public virtual void SafeUpdate(GameTime gametime) { }

        public bool Initialised { get; private set; }
        public sealed override void OnInitialize()
        {
            base.OnInitialize();
            SafeOnInitialize();
            Initialised = true;
        }
        public virtual void SafeOnInitialize() { }

        public bool Activated { get; private set; }
        public sealed override void OnActivate()
        {
            base.OnActivate();
            SafeOnActivate();
            Activated = true;
        }
        public virtual void SafeOnActivate() { }

        public SoundStyle? leftMouseDownSound;
        public sealed override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            bool playSound = true;
            SafeLeftMouseDown(evt, ref playSound);
            if (leftMouseDownSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(leftMouseDownSound);
            }
        }
        public virtual void SafeLeftMouseDown(UIMouseEvent evt, ref bool playSound) { }

        public SoundStyle? leftMouseUpSound;
        public sealed override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            bool playSound = true;
            SafeLeftMouseUp(evt, ref playSound);
            if (leftMouseUpSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(leftMouseUpSound);
            }
        }
        public virtual void SafeLeftMouseUp(UIMouseEvent evt, ref bool playSound) { }

        public SoundStyle? leftClickSound;
        public sealed override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            bool playSound = true;
            SafeLeftClick(evt, ref playSound);
            if (leftClickSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(leftClickSound);
            }
        }
        public virtual void SafeLeftClick(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? leftDoubleClickSound;
        public sealed override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            bool playSound = true;
            SafeLeftDoubleClick(evt, ref playSound);
            if (leftDoubleClickSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(leftDoubleClickSound);
            }
        }
        public virtual void SafeLeftDoubleClick(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? rightMouseDownSound;
        public sealed override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);
            bool playSound = true;
            SafeRightMouseDown(evt, ref playSound);
            if (rightMouseDownSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(rightMouseDownSound);
            }
        }
        public virtual void SafeRightMouseDown(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? rightMouseUpSound;
        public sealed override void RightMouseUp(UIMouseEvent evt)
        {
            base.RightMouseUp(evt);
            bool playSound = true;
            SafeRightMouseUp(evt, ref playSound);
            if (rightMouseUpSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(rightMouseUpSound);
            }
        }
        public virtual void SafeRightMouseUp(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? rightClickSound;
        public sealed override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            bool playSound = true;
            SafeRightClick(evt, ref playSound);
            if (rightClickSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(rightClickSound);
            }
        }
        public virtual void SafeRightClick(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? rightDoubleClickSound;
        public sealed override void RightDoubleClick(UIMouseEvent evt)
        {
            base.RightDoubleClick(evt);
            bool playSound = true;
            SafeRightDoubleClick(evt, ref playSound);
            if (rightDoubleClickSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(rightDoubleClickSound);
            }
        }
        public virtual void SafeRightDoubleClick(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? mouseOverSound;
        public sealed override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            bool playSound = true;
            SafeMouseOver(evt, ref playSound);
            if (mouseOverSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(mouseOverSound);
            }
        }
        public virtual void SafeMouseOver(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? mouseOutSound;
        public sealed override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            bool playSound = true;
            SafeMouseOut(evt, ref playSound);
            if (mouseOutSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(mouseOutSound);
            }
        }
        public virtual void SafeMouseOut(UIMouseEvent evt, ref bool playSound) { }


        public SoundStyle? scrollWheelSound;
        public sealed override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            bool playSound = true;
            SafeScrollWheel(evt, ref playSound);
            if (scrollWheelSound != null && !muted && playSound)
            {
                SoundEngine.PlaySound(scrollWheelSound);
            }
        }
        public virtual void SafeScrollWheel(UIScrollWheelEvent evt, ref bool playSound) { }

        public sealed override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (IsMouseHovering && showHoverText && hoverText != null)
            {
                UICommon.TooltipMouseText(hoverText);
            }
            SafeDraw(spriteBatch);
        }
        public virtual void SafeDraw(SpriteBatch spriteBatch) { }
    }
}
