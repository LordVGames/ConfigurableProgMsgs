using System.Text.RegularExpressions;
using static ConfigurableProgressionMessages.ConfigurableProgressionMessages;

namespace CustomStringExtensions
{
    public static class CustomStringExtensions
    {
        public static bool IsMultiMsg(this string progMsg) => Regex.Match(progMsg, multiMsgRegex).Success;

        public static string CleanUpProgMsg(this string msg)
        {
            msg = msg.Replace("/EXTRAMSG:", "EXTRAMSG:");
            msg = msg.Trim();
            return msg;
        }
    }
}