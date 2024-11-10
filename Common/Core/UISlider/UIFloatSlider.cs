using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNPCStats2.Common.Core.UISlider
{
    public class UIFloatSlider : UISlider<float>
    {
        private float rounding;

        public override float Value
        {
            get => base.Value;
            set
            {
                if (rounding > 0)
                {
                    base.Value = (float)Math.Clamp(Math.Round(value / rounding) * rounding, min, max);
                }
                else
                {
                    base.Value = Math.Clamp(value, min, max);
                }
            }
        }

        public override float UpdateSliderValue(float barPercent)
        {
            return min + (max - min) * barPercent;
        }

        public UIFloatSlider(float defaultValue = 0, float min = 0, float max = 10, float rounding = 0) : base(defaultValue)
        {
            this.min = min;
            this.max = max;
            this.rounding = rounding;
        }
    }
}
