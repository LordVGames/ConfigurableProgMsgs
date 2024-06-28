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
            public static ConfigEntry<string>[] Messages { get; set; } = new ConfigEntry<string>[ProgMsgsCount];
            public static ConfigEntry<int>[] SendOnStageX { get; set; } = new ConfigEntry<int>[ProgMsgsCount];
            public static ConfigEntry<int>[] SendAgainAfterXStages { get; set; } = new ConfigEntry<int>[ProgMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnLoopStart { get; set; } = new ConfigEntry<WhenToSendMsg>[ProgMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnBazaarVisit { get; set; } = new ConfigEntry<WhenToSendMsg>[ProgMsgsCount];
            public static ConfigEntry<WhenToSendMsg>[] SendOnVoidFieldsVisit { get; set; } = new ConfigEntry<WhenToSendMsg>[ProgMsgsCount];
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

        internal const string DetailedMessageConfigDesc = "Leave blank for no message. If you want to include extra messages for the mod to randomly pick from put \"EXTRAMSG:\" before every message past the first one. If you really want to use \"EXTRAMSG:\" in a message, put a forward slash right before it.\nExample: \"my 1st message  EXTRAMSG: my 2nd message EXTRAMSG: my 3rd message with /EXTRAMSG:\"";
        // Fun fact: If you change this & build the mod it'll automatically make and/or use the new amount progression messages w/o any other changes
        internal const int ProgMsgsCount = 8;
        //language=regex
        internal const string MultiMsgRegex = "(?<!/)(EXTRAMSG:)";

        private string CurrentSceneName;
        private int PreviousLoopClearCount = 0;
        private int PreviousStageNum = 0;
        private bool HasVoidFieldsBeenVisited = false;
        private bool HasBazaarBeenVisited = false;
        private bool WasChatMsgSent = false;
        private int[] TempSendOnStageXValues = new int[ProgMsgsCount];

        public void Awake()
        {
            Log.Init(Logger);
            ReadConfig();
            SetupSettingChangedEvents();

            Run.onRunStartGlobal += (Run run) =>
            {
                HasBazaarBeenVisited = false;
                HasVoidFieldsBeenVisited = false;
                PreviousStageNum = 0;
                PreviousLoopClearCount = 0;
                for (int i = 0; i < ProgMsgsCount; i++)
                {
                    TempSendOnStageXValues[i] = ModConfigEntries.SendOnStageX[i].Value;
                }
            };
            On.RoR2.Run.OnServerSceneChanged += (orig, self, sceneName) =>
            {
                orig(self, sceneName);
                CurrentSceneName = sceneName;
            };
            On.RoR2.Stage.BeginServer += (orig, self) =>
            {
                orig(self);
                int CurrentStageNum = Run.instance.stageClearCount + 1;
                bool HasStageNumChanged = false;
                bool HasLoopStarted = false;
                if (CurrentStageNum > PreviousStageNum)
                {
                    HasStageNumChanged = true;
                    PreviousStageNum = CurrentStageNum;
                }
                if (Run.instance.loopClearCount > PreviousLoopClearCount)
                {
                    HasLoopStarted = true;
                    PreviousLoopClearCount = Run.instance.loopClearCount;
                }
                if (ModConfigEntries.DebugLogging.Value)
                {
                    Log.Debug($"Current scene name is \"{CurrentSceneName}\". \"arena\" is the Void Fields, \"bazaar\" is the Bazaar");
                    Log.Debug($"Did a loop start this stage? {HasLoopStarted}");
                    Log.Debug($"Current stage number is {CurrentStageNum}");
                    Log.Debug($"Did the stage number changes from last stage? {HasStageNumChanged}");
                }

                for (int i = 0; i < ProgMsgsCount; i++)
                {
                    WasChatMsgSent = false;
                    if (HasStageNumChanged && CurrentStageNum == TempSendOnStageXValues[i])
                    {
                        SendProgMsg(i);
                        if (ModConfigEntries.SendAgainAfterXStages[i].Value > 0)
                        {
                            TempSendOnStageXValues[i] += ModConfigEntries.SendAgainAfterXStages[i].Value;
                            if (ModConfigEntries.DebugLogging.Value)
                            {
                                Log.Debug($"Message #{i + 1} will send again on stage {TempSendOnStageXValues[i]}, {ModConfigEntries.SendAgainAfterXStages[i].Value} stages after the current one.");
                            }
                        }
                        continue;
                    }
                    if (HasLoopStarted)
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
                        if (WasChatMsgSent)
                        {
                            continue;
                        }
                    }
                    // Void Fields
                    if (CurrentSceneName == "arena")
                    {
                        switch (ModConfigEntries.SendOnVoidFieldsVisit[i].Value)
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!HasVoidFieldsBeenVisited)
                                {
                                    SendProgMsg(i);
                                    HasVoidFieldsBeenVisited = true;
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendProgMsg(i);
                                break;
                        }
                        if (WasChatMsgSent)
                        {
                            continue;
                        }
                    }
                    else if (CurrentSceneName == "bazaar")
                    {
                        switch (ModConfigEntries.SendOnBazaarVisit[i].Value)
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!HasBazaarBeenVisited)
                                {
                                    SendProgMsg(i);
                                    HasBazaarBeenVisited = true;
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

            for (int i = 0; i < ProgMsgsCount; i++)
            {
                string SectionName = $"Progression Message #{i + 1}";

                string DefaultMessage_Contents = "";
                string DefaultMessage_Desc = "The message(s) that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message.";
                WhenToSendMsg DefaultSendOnLoopStart_WhenToSend = WhenToSendMsg.Never;
                if (i == 0)
                {
                    DefaultMessage_Contents = "<size=125%><color=#005500>The planet is growing restless from your presence...</color></size>";
                    DefaultMessage_Desc = $"The message(s) that will be sent when the conditions are met. {DetailedMessageConfigDesc}";
                    DefaultSendOnLoopStart_WhenToSend = WhenToSendMsg.OnFirstTime;
                }

                ModConfigEntries.Messages[i] = Config.Bind(
                    SectionName,
                    ConfigEntryNames.Message,
                    DefaultMessage_Contents,
                    DefaultMessage_Desc
                );
                ModConfigEntries.SendOnStageX[i] = Config.Bind(
                    SectionName,
                    ConfigEntryNames.SendOnStageX,
                    -1,
                    "At the start of what stage should the message be sent? Set to -1 for no stage."
                );
                ModConfigEntries.SendAgainAfterXStages[i] = Config.Bind(
                    SectionName,
                    ConfigEntryNames.SendAgainAfterXStages,
                    -1,
                    $"After how many stages should the message be sent again? Set to -1 to not send again. Does nothing if \"{ConfigEntryNames.SendOnStageX}\" is -1."
                );
                ModConfigEntries.SendOnLoopStart[i] = Config.Bind(
                    SectionName,
                    ConfigEntryNames.SendOnLoopStart,
                    DefaultSendOnLoopStart_WhenToSend,
                    "Should the message be sent when a loop is started?"
                );
                ModConfigEntries.SendOnBazaarVisit[i] = Config.Bind(
                    SectionName,
                    ConfigEntryNames.SendOnBazaarVisit,
                    WhenToSendMsg.Never,
                    "Should the message be sent when you visit the bazaar?"
                );
                ModConfigEntries.SendOnVoidFieldsVisit[i] = Config.Bind(
                    SectionName,
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
            for (int i = 0; i < ProgMsgsCount; i++)
            {
                // "i" has to be copied here because lambda expressions will get a reference instead of a copy to the original "i" and mess things up later
                int ir = i;
                ModConfigEntries.Messages[ir].SettingChanged += (sender, args) =>
                {
                    SendChangedProgMsgToClientChat(ModConfigEntries.Messages[ir].Value, ir);
                };
            }
        }

        private void SendChangedProgMsgToClientChat(string ProgMsg, int ProgMsgIndex)
        {
            if (ProgMsg.ProgMsgIsMultiMsg())
            {
                SendChatMsg($"<color=yellow>Progression Message #{ProgMsgIndex + 1} will randomly pick from these messages when it's sent:</color>", false);
                foreach (string Msg in GetMsgListFromMultiMsg(ProgMsg))
                {
                    SendChatMsg(Msg.CleanUpProgMsg(), false);
                }
            }
            else if (ProgMsg.IsNullOrWhiteSpace())
            {
                SendChatMsg($"<color=yellow>Progression Message #{ProgMsgIndex + 1} will not send any message.\nIt's best to set the message to never send instead.", false);
            }
            else
            {
                SendChatMsg($"<color=yellow>Progression Message #{ProgMsgIndex + 1} is now:</color>", false);
                SendChatMsg(ProgMsg.CleanUpProgMsg(), false);
            }
        }

        private void SendProgMsg(int ArrayNum)
        {
            WasChatMsgSent = true;
            string MsgToSend;
            string ChosenProgMsg = ModConfigEntries.Messages[ArrayNum].Value;
            if (ChosenProgMsg.ProgMsgIsMultiMsg())
            {
                List<string> MultiMsg_List = GetMsgListFromMultiMsg(ChosenProgMsg);
                Random RNG = new Random();
                int RndMsgIndex = RNG.Next(0, MultiMsg_List.Count - 1);
                MsgToSend = MultiMsg_List[RndMsgIndex].CleanUpProgMsg();
            }
            else
            {
                MsgToSend = ChosenProgMsg.CleanUpProgMsg();
            }
            SendChatMsg(MsgToSend);
        }

        private List<string> GetMsgListFromMultiMsg(string MultiMsg)
        {
            List<string> MultiMsg_List = Regex.Split(MultiMsg, MultiMsgRegex).ToList<string>();
            // Iterating backwards because it's the only way to iterate & remove things in a list AFAIK
            for (int i = MultiMsg_List.Count - 1; i >= 0; i--)
            {
                if (MultiMsg_List[i] == "EXTRAMSG:")
                {
                    MultiMsg_List.RemoveAt(i);
                }
            }
            return MultiMsg_List;
        }

        private void SendChatMsg(string MsgToSend, bool BroadcastToAll = true)
        {
            if (BroadcastToAll)
            {
                // Partially ported from ChatMessage.Send in R2API.Utils
                Chat.SimpleChatMessage ChatMsg = new Chat.SimpleChatMessage() { baseToken = MsgToSend };
                Chat.SendBroadcastChat(ChatMsg);
            }
            else
            {
                Chat.AddMessage(MsgToSend);
            }
        }
    }
}