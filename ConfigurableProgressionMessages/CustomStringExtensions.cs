using System.Text.RegularExpressions;
using static ConfigurableProgressionMessages.ConfigurableProgressionMessages;

namespace CustomStringExtensions
{
    public static class CustomStringExtensions
    {
        public static bool ProgMsgIsMultiMsg(this string ProgMsg) => Regex.Match(ProgMsg, MultiMsgRegex).Success;

        public static string CleanUpProgMsg(this string Msg)
        {
            Msg = Msg.Replace("/EXTRAMSG:", "EXTRAMSG:");
            Msg = Msg.Trim();
            return Msg;
        }
    }
}