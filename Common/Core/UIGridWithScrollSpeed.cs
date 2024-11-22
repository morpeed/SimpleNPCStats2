using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace SimpleNPCStats2.Common.Core
{
    public class UIGridWithScrollSpeed : UIGrid
    {
        public float scrollSpeed = 1f;

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            var tempScrollbar = _scrollbar;
            _scrollbar = null;

            base.ScrollWheel(evt);

            _scrollbar = tempScrollbar;
            if (_scrollbar != null)
            {
                _scrollbar.ViewPosition -= evt.ScrollWheelValue * scrollSpeed;
            }
        }
    }
}
