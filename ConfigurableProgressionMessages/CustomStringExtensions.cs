using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static ConfigurableProgressionMessages.ConfigurableProgressionMessages;

namespace CustomStringExtensions
{
    public static class CustomStringExtensions
    {
        public static bool ProgMsgIsMultiMsg(this string ProgMsg)
        {
            return Regex.Match(ProgMsg, MultiMsgRegex).Success;
        }

        public static string CleanUpProgMsg(this string Msg)
        {
            Msg = Msg.Replace("/EXTRAMSG:", "EXTRAMSG:");
            Msg = Msg.Trim();
            return Msg;
        }
    }
}
