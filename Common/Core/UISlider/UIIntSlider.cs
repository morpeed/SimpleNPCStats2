using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNPCStats2.Common.Core.UISlider
{
    public class UIIntSlider : UISlider<int>
    {
        public override int Value
        { 
            get => base.Value;
            set
            {
                if (rounding > 0)
                {
                    value = (int)Math.Round(value / (float)rounding) * rounding;
                }
                base.Value = Math.Clamp(value, min, max);
            }
        }

        public override int UpdateSliderValue(float barPercent)
        {
            return (int)(min + (max - min) * barPercent);
        }

        private int rounding;

        public UIIntSlider(int defaultValue = 0, int min = 0, int max = 10, int rounding = 0) : base(defaultValue)
        {
            Value = defaultValue;
            this.min = min;
            this.max = max;
            this.rounding = rounding;
        }
    }
}
