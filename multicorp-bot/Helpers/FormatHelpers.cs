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

        public static string Capitalize(string msg)
        {
            string newMsg;
            if (msg.Length == 1)
                newMsg = msg.ToUpper();
            else
                newMsg = $"{char.ToUpper(msg[0])}{msg.Substring(1)}";

           return newMsg;
        }
    }
}
