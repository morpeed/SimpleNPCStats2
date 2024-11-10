using System;
using System.Text.RegularExpressions;
using System.Globalization;
using SimpleNPCStats2.Common.Core.UIField;

namespace SimpleNPCStats2.Common.Config.UI.Core
{
    public class UIFloatField : UIField<float>
    {
        public UIFloatField(float defaultValue = 0, int maxCharacters = 9, float min = float.MinValue, float max = float.MaxValue, float rounding = 0) : base(defaultValue, maxCharacters)
        {
            this.min = min;
            this.max = max;
            if (rounding < 0)
            {
                rounding = 0;
            }
            this.rounding = rounding;
        }

        private readonly float min;
        private readonly float max;
        private readonly float rounding;

        protected override string FilterValueString(string value) => Regex.Replace(value, "[^-\\d.]", "");

        protected override bool TryParseValueString(string value, out float result)
        {
            if (float.TryParse(value, CultureInfo.InvariantCulture, out float floatResult))
            {
                if (rounding > 0)
                {
                    result = Math.Clamp((float)Math.Round(floatResult / rounding) * rounding, min, max);
                }
                else
                {
                    result = Math.Clamp(floatResult, min, max);
                }
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        protected override string FormatString(float value)
        {
            return value.ToString("0.###");
        }
    }
}
