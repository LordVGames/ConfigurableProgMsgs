using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using CustomStringExtensions;

// This namepsace block makes the mod vanilla compatible...somehow.
namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace ConfigurableProgressionMessages
{
    [BepInDependency(RiskOfOptionsCompat.PluginName, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ConfigurableProgressionMessages : BaseUnityPlugin
    {
        public enum WhenToSendMsg
        {
            Never,
            OnFirstTime,
            OnEveryTime
        }
        public static class ModConfigEntries
        {
            public static ConfigEntry<bool> DebugLogging { get; set; }
            public static ConfigEntry<string>[] Messages { get; set; } = new ConfigEntry<string>[progMsgsCount];
            public static ConfigEntry<int>[] SendOnStageX { get; set; } = new ConfigEntry<int>[progMsgsCount];
            public static ConfigEntry<int>[] SendAgainAfterXStages { get; set; } = new ConfigEntry<int>[progMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnLoopStart { get; set; } = new ConfigEntry<WhenToSendMsg>[progMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnBazaarVisit { get; set; } = new ConfigEntry<WhenToSendMsg>[progMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnVoidFieldsVisit { get; set; } = new ConfigEntry<WhenToSendMsg>[progMsgsCount];
        }
        public class ConfigEntryNames
        {
            public const string DebugLogging = "Enable debug logging";
            public const string Message = "Message to send";
            public const string SendOnStageX = "Send On Stage X";
            public const string SendAgainAfterXStages = "Send Again After X Stages";
            public const string SendOnLoopStart = "Send On Loop Start";
            public const string SendOnBazaarVisit = "Send On Bazaar Visit";
            public const string SendOnVoidFieldsVisit = "Send On Void Fields Visit";
        }

        public const string PluginName = "ConfigurableProgressionMessages";
        public const string PluginVersion = "2.1.0";
        public const string PluginAuthor = "LordVGames";
        public const string PluginGUID = $"{PluginAuthor}.{PluginName}";

        internal const string detailedMessageConfigDesc = "Leave blank for no message. If you want to include extra messages for the mod to randomly pick from put \"EXTRAMSG:\" before every message past the first one. If you really want to use \"EXTRAMSG:\" in a message, put a forward slash right before it.\nExample: \"my 1st message  EXTRAMSG: my 2nd message EXTRAMSG: my 3rd message with /EXTRAMSG:\"";
        // Fun fact: If you change this & build the mod it'll automatically make and/or use the new amount progression messages w/o any other changes
        internal const int progMsgsCount = 8;
        //language=regex
        internal const string multiMsgRegex = "(?<!/)(EXTRAMSG:)";

        private string _currentSceneName;
        private int _previousLoopClearCount = 0;
        private int _previousStageNum = 0;
        private bool _hasVoidFieldsBeenVisited = false;
        private bool _hasBazaarBeenVisited = false;
        private bool _wasChatMsgSent = false;
        private int[] _tempSendOnStageXValues = new int[progMsgsCount];

        public void Awake()
        {
            Log.Init(Logger);
            ReadConfig();
            SetupSettingChangedEvents();

            Run.onRunStartGlobal += (Run run) =>
            {
                _hasBazaarBeenVisited = false;
                _hasVoidFieldsBeenVisited = false;
                _previousStageNum = 0;
                _previousLoopClearCount = 0;
                for (int i = 0; i < progMsgsCount; i++)
                {
                    _tempSendOnStageXValues[i] = ModConfigEntries.SendOnStageX[i].Value;
                }
            };
            On.RoR2.Run.OnServerSceneChanged += (orig, self, sceneName) =>
            {
                orig(self, sceneName);
                _currentSceneName = sceneName;
            };
            On.RoR2.Stage.BeginServer += (orig, self) =>
            {
                orig(self);
                int currentStageNum = Run.instance.stageClearCount + 1;
                bool hasStageNumChanged = false;
                bool hasLoopStarted = false;
                if (currentStageNum > _previousStageNum)
                {
                    hasStageNumChanged = true;
                    _previousStageNum = currentStageNum;
                }
                if (Run.instance.loopClearCount > _previousLoopClearCount)
                {
                    hasLoopStarted = true;
                    _previousLoopClearCount = Run.instance.loopClearCount;
                }
                if (ModConfigEntries.DebugLogging.Value)
                {
                    Log.Debug($"Current scene name is \"{_currentSceneName}\". \"arena\" is the Void Fields, \"bazaar\" is the Bazaar");
                    Log.Debug($"Did a loop start this stage? {hasLoopStarted}");
                    Log.Debug($"Current stage number is {currentStageNum}");
                    Log.Debug($"Did the stage number changes from last stage? {hasStageNumChanged}");
                }

                for (int i = 0; i < progMsgsCount; i++)
                {
                    _wasChatMsgSent = false;
                    if (hasStageNumChanged && currentStageNum == _tempSendOnStageXValues[i])
                    {
                        SendProgMsg(i);
                        if (ModConfigEntries.SendAgainAfterXStages[i].Value > 0)
                        {
                            _tempSendOnStageXValues[i] += ModConfigEntries.SendAgainAfterXStages[i].Value;
                            if (ModConfigEntries.DebugLogging.Value)
                            {
                                Log.Debug($"Message #{i + 1} will send again on stage {_tempSendOnStageXValues[i]}, {ModConfigEntries.SendAgainAfterXStages[i].Value} stages after the current one.");
                            }
                        }
                        continue;
                    }
                    if (hasLoopStarted)
                    {
                        switch (ModConfigEntries.SendOnLoopStart[i].Value)
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (Run.instance.loopClearCount == 1)
                                {
                                    SendProgMsg(i);
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendProgMsg(i);
                                break;
                        }
                        if (_wasChatMsgSent)
                        {
                            continue;
                        }
                    }
                    // Void Fields
                    if (_currentSceneName == "arena")
                    {
                        switch (ModConfigEntries.SendOnVoidFieldsVisit[i].Value)
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!_hasVoidFieldsBeenVisited)
                                {
                                    SendProgMsg(i);
                                    _hasVoidFieldsBeenVisited = true;
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendProgMsg(i);
                                break;
                        }
                        if (_wasChatMsgSent)
                        {
                            continue;
                        }
                    }
                    else if (_currentSceneName == "bazaar")
                    {
                        switch (ModConfigEntries.SendOnBazaarVisit[i].Value)
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!_hasBazaarBeenVisited)
                                {
                                    SendProgMsg(i);
                                    _hasBazaarBeenVisited = true;
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendProgMsg(i);
                                break;
                        }
                    }
                }
            };
        }

        private void ReadConfig()
        {
            ModConfigEntries.DebugLogging = Config.Bind(
                "General",
                ConfigEntryNames.DebugLogging,
                false,
                "Only useful if you're trying to figure out a problem either with your messages or the mod itself."
            );

            for (int i = 0; i < progMsgsCount; i++)
            {
                string sectionName = $"Progression Message #{i + 1}";

                string defaultMessageContents = "";
                string defaultMessageDesc = "The message(s) that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message.";
                WhenToSendMsg defaultWhenToSendOnLoopStart = WhenToSendMsg.Never;
                if (i == 0)
                {
                    defaultMessageContents = "<size=125%><color=#005500>The planet is growing restless from your presence...</color></size>";
                    defaultMessageDesc = $"The message(s) that will be sent when the conditions are met. {detailedMessageConfigDesc}";
                    defaultWhenToSendOnLoopStart = WhenToSendMsg.OnFirstTime;
                }

                ModConfigEntries.Messages[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.Message,
                    defaultMessageContents,
                    defaultMessageDesc
                );
                ModConfigEntries.SendOnStageX[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.SendOnStageX,
                    -1,
                    "At the start of what stage should the message be sent? Set to -1 for no stage."
                );
                ModConfigEntries.SendAgainAfterXStages[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.SendAgainAfterXStages,
                    -1,
                    $"After how many stages should the message be sent again? Set to -1 to not send again. Does nothing if \"{ConfigEntryNames.SendOnStageX}\" is -1."
                );
                ModConfigEntries.SendOnLoopStart[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.SendOnLoopStart,
                    defaultWhenToSendOnLoopStart,
                    "Should the message be sent when a loop is started?"
                );
                ModConfigEntries.SendOnBazaarVisit[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.SendOnBazaarVisit,
                    WhenToSendMsg.Never,
                    "Should the message be sent when you visit the bazaar?"
                );
                ModConfigEntries.SendOnVoidFieldsVisit[i] = Config.Bind(
                    sectionName,
                    ConfigEntryNames.SendOnVoidFieldsVisit,
                    WhenToSendMsg.Never,
                    "Should the message be sent when you visit the void fields?"
                );
            }

            if (RiskOfOptionsCompat.ModIsRunning)
            {
                RiskOfOptionsCompat.AddProgMsgsToRiskOfOptions();
            }
        }

        private void SetupSettingChangedEvents()
        {
            for (int i = 0; i < progMsgsCount; i++)
            {
                // "i" has to be copied here because lambda expressions will get a reference instead of a copy to the original "i" and mess things up later
                int ir = i;
                ModConfigEntries.Messages[ir].SettingChanged += (sender, args) =>
                {
                    SendChangedProgMsgToClientChat(ModConfigEntries.Messages[ir].Value, ir);
                };
            }
        }

        private void SendChangedProgMsgToClientChat(string progMsg, int progMsgIndex)
        {
            if (progMsg.IsMultiMsg())
            {
                SendChatMsg($"<color=yellow>Progression Message #{progMsgIndex + 1} will randomly pick from these messages when it's sent:</color>", false);
                foreach (string Msg in GetMsgListFromMultiMsg(progMsg))
                {
                    SendChatMsg(Msg.CleanUpProgMsg(), false);
                }
            }
            else if (progMsg.IsNullOrWhiteSpace())
            {
                SendChatMsg($"<color=yellow>Progression Message #{progMsgIndex + 1} will not send any message.\nIt's best to set the message to never send instead.", false);
            }
            else
            {
                SendChatMsg($"<color=yellow>Progression Message #{progMsgIndex + 1} is now:</color>", false);
                SendChatMsg(progMsg.CleanUpProgMsg(), false);
            }
        }

        private void SendProgMsg(int arrayNum)
        {
            _wasChatMsgSent = true;
            string msgToSend;
            string multiMsg = ModConfigEntries.Messages[arrayNum].Value;
            if (multiMsg.IsMultiMsg())
            {
                List<string> multiMsgList = GetMsgListFromMultiMsg(multiMsg);
                Random rng = new Random();
                int randomMsgIndex = rng.Next(0, multiMsgList.Count - 1);
                msgToSend = multiMsgList[randomMsgIndex].CleanUpProgMsg();
            }
            else
            {
                msgToSend = multiMsg.CleanUpProgMsg();
            }
            SendChatMsg(msgToSend);
        }

        private List<string> GetMsgListFromMultiMsg(string multiMsg)
        {
            List<string> multiMsgList = Regex.Split(multiMsg, multiMsgRegex).ToList<string>();
            // Iterating backwards because it's the only way to iterate & remove things in a list AFAIK
            for (int i = multiMsgList.Count - 1; i >= 0; i--)
            {
                if (multiMsgList[i] == "EXTRAMSG:")
                {
                    multiMsgList.RemoveAt(i);
                }
            }
            return multiMsgList;
        }

        private void SendChatMsg(string msgToSend, bool broadcastToAll = true)
        {
            if (broadcastToAll)
            {
                // Partially ported from ChatMessage.Send in R2API.Utils
                Chat.SimpleChatMessage chatMsg = new Chat.SimpleChatMessage() { baseToken = msgToSend };
                Chat.SendBroadcastChat(chatMsg);
            }
            else
            {
                Chat.AddMessage(msgToSend);
            }
        }
    }
}