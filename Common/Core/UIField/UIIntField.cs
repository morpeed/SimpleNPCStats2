using System;
using System.Text.RegularExpressions;
using System.Globalization;
using SimpleNPCStats2.Common.Core.UIField;

namespace SimpleNPCStats2.Common.Config.UI.Core
{
    public class UIIntField : UIField<int>
    {
        public UIIntField(int defaultValue = 0, int maxCharacters = 9, int min = int.MinValue, int max = int.MaxValue, int rounding = 0) : base(defaultValue, maxCharacters)
        {
            this.min = min;
            this.max = max;
            if (rounding < 0)
            {
                rounding = 0;
            }
            this.rounding = rounding;
        }

        private readonly int min;
        private readonly int max;
        private readonly int rounding;

        protected override string FilterValueString(string value) => Regex.Replace(value, "[^-\\d]", "");

        protected override bool TryParseValueString(string value, out int result)
        {
            if (int.TryParse(value, CultureInfo.InvariantCulture, out int intResult))
            {
                if (rounding > 1)
                {
                    result = Math.Clamp((int)Math.Round((float)intResult / rounding) * rounding, min, max);
                }
                else
                {
                    result = Math.Clamp(intResult, min, max);
                }
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
