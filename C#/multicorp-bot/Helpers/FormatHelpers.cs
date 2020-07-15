using System;
using System.Collections.Generic;
using System.Text;

namespace multicorp_bot.Helpers
{
    public static class FormatHelpers
    {
        public static  string FormattedNumber(string amount)
        {
            return String.Format("{0:n0}", int.Parse(amount));
        }
    }
}
