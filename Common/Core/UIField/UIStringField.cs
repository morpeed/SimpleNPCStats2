using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNPCStats2.Common.Core.UIField
{
    public class UIField : UIField<string>
    {
        public UIField(string defaultValue, int maxCharacters) : base(defaultValue, maxCharacters)
        {

        }

        protected override string FilterValueString(string value) => value;
        protected override bool TryParseValueString(string value, out string result)
        {
            result = value;
            return true;
        }
    }
}
