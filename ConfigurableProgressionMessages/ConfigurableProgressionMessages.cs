using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using RoR2;

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
    [BepInPlugin("LordVGames.ConfigurableProgressionMessages", "ConfigurableProgressionMessages", "1.1.0")]
    public class ConfigurableProgressionMessages : BaseUnityPlugin
    {
        public enum WhenToSendMsg
        {
            Never,
            OnFirstTime,
            OnEveryTime
        }
        private static string MultiMsgSeparator = "EXTRAMSG:";
        private static int ProgMsgAll_Count = 6;

        private string CurrentSceneName;
        private int PreviousLoopClearCount = 0;
        private int PreviousStageNum = 0;
        private bool HasVoidFieldsBeenVisited = false;
        private bool HasBazaarBeenVisited = false;
        private bool WasChatMsgSent = false;

        private string[] ProgMsgAll_Message = new string[ProgMsgAll_Count];
        private int[] ProgMsgAll_SendOnStageX = new int[ProgMsgAll_Count];
        private int[] ProgMsgAll_ReSendAfterXStages = new int[ProgMsgAll_Count];
        private WhenToSendMsg[] ProgMsgAll_SendOnLoopStart = new WhenToSendMsg[ProgMsgAll_Count];
        private WhenToSendMsg[] ProgMsgAll_SendOnBazaarVisit = new WhenToSendMsg[ProgMsgAll_Count];
        private WhenToSendMsg[] ProgMsgAll_SendOnVoidFieldsVisit = new WhenToSendMsg[ProgMsgAll_Count];

        #region ConfigEntry creation
        public static ConfigEntry<bool> ProgMsgAll_Debug { get; set; }

        public static ConfigEntry<string> ProgMsg1_Message { get; set; }
        public static ConfigEntry<int> ProgMsg1_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg1_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg1_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg1_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg1_SendOnVoidFieldsVisit { get; set; }

        public static ConfigEntry<string> ProgMsg2_Message { get; set; }
        public static ConfigEntry<int> ProgMsg2_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg2_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg2_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg2_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg2_SendOnVoidFieldsVisit { get; set; }

        public static ConfigEntry<string> ProgMsg3_Message { get; set; }
        public static ConfigEntry<int> ProgMsg3_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg3_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg3_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg3_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg3_SendOnVoidFieldsVisit { get; set; }

        public static ConfigEntry<string> ProgMsg4_Message { get; set; }
        public static ConfigEntry<int> ProgMsg4_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg4_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg4_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg4_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg4_SendOnVoidFieldsVisit { get; set; }

        public static ConfigEntry<string> ProgMsg5_Message { get; set; }
        public static ConfigEntry<int> ProgMsg5_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg5_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg5_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg5_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg5_SendOnVoidFieldsVisit { get; set; }

        public static ConfigEntry<string> ProgMsg6_Message { get; set; }
        public static ConfigEntry<int> ProgMsg6_SendOnStageX { get; set; }
        public static ConfigEntry<int> ProgMsg6_ReSendAfterXStagesNum { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg6_SendOnLoopStart { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg6_SendOnBazaarVisit { get; set; }
        public static ConfigEntry<WhenToSendMsg> ProgMsg6_SendOnVoidFieldsVisit { get; set; }
        #endregion

        public void Awake()
        {
            Log.Init(Logger);
            ReadConfig();

            Run.onRunStartGlobal += (Run run) =>
            {
                HasBazaarBeenVisited = false;
                HasVoidFieldsBeenVisited = false;
                PreviousStageNum = 0;
                PreviousLoopClearCount = 0;
                ResetProgMsgAllSendOnStageX();
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
                if (CurrentStageNum > PreviousStageNum)
                {
                    HasStageNumChanged = true;
                    PreviousStageNum = CurrentStageNum;
                }
                bool HasLoopStarted = false;
                if (Run.instance.loopClearCount > PreviousLoopClearCount)
                {
                    HasLoopStarted = true;
                    PreviousLoopClearCount = Run.instance.loopClearCount;
                }

                for (int i = 0; i < ProgMsgAll_Count; i++)
                {
                    WasChatMsgSent = false;
                    if (HasStageNumChanged && CurrentStageNum == ProgMsgAll_SendOnStageX[i])
                    {
                        SendChatMsg(i);
                        ProgMsgAll_SendOnStageX[i] += ProgMsgAll_ReSendAfterXStages[i];
                        continue;
                    }
                    if (HasLoopStarted)
                    {
                        switch (ProgMsgAll_SendOnLoopStart[i])
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (Run.instance.loopClearCount == 1)
                                {
                                    SendChatMsg(i);
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendChatMsg(i);
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
                        switch (ProgMsgAll_SendOnVoidFieldsVisit[i])
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!HasVoidFieldsBeenVisited)
                                {
                                    SendChatMsg(i);
                                    HasVoidFieldsBeenVisited = true;
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendChatMsg(i);
                                break;
                        }
                        if (WasChatMsgSent)
                        {
                            continue;
                        }
                    }
                    else if (CurrentSceneName == "bazaar")
                    {
                        switch (ProgMsgAll_SendOnBazaarVisit[i])
                        {
                            case WhenToSendMsg.OnFirstTime:
                                if (!HasBazaarBeenVisited)
                                {
                                    SendChatMsg(i);
                                    HasBazaarBeenVisited = true;
                                }
                                break;
                            case WhenToSendMsg.OnEveryTime:
                                SendChatMsg(i);
                                break;
                        }
                        if (WasChatMsgSent)
                        {
                            continue;
                        }
                    }
                }
            };
        }

        private void ReadConfig()
        {
            #region Config binding
            ProgMsg1_Message = Config.Bind<string>(
                "Progression Message #1",
                "Message to send",
                "<size=125%><color=#005500>The planet is growing restless from your presence...</color></size>",
                "The chat message that will be sent when the conditions are met. Leave blank for no message. If you want to include extra messages for the mod to randomly pick from put \"EXTRAMSG:\" before every message past the first one.\nI.E. \"my 1st message  EXTRAMSG: my 2nd message EXTRAMSG: my 3rd message\""
            );
            ProgMsg1_SendOnStageX = Config.Bind<int>(
                "Progression Message #1",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg1_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #1",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg1_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #1",
                "Send On Loop Start",
                WhenToSendMsg.OnFirstTime,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg1_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #1",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg1_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #1",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );

            ProgMsg2_Message = Config.Bind<string>(
                "Progression Message #2",
                "Message to send",
                "",
                "The chat message that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message."
            );
            ProgMsg2_SendOnStageX = Config.Bind<int>(
                "Progression Message #2",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg2_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #2",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg2_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #2",
                "Send On Loop Start",
                WhenToSendMsg.Never,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg2_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #2",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg2_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #2",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );

            ProgMsg3_Message = Config.Bind<string>(
                "Progression Message #3",
                "Message to send",
                "",
                "The chat message that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message."
            );
            ProgMsg3_SendOnStageX = Config.Bind<int>(
                "Progression Message #3",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg3_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #3",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg3_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #3",
                "Send On Loop Start",
                WhenToSendMsg.Never,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg3_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #3",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg3_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #3",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );

            ProgMsg4_Message = Config.Bind<string>(
                "Progression Message #4",
                "Message to send",
                "",
                "The chat message that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message."
            );
            ProgMsg4_SendOnStageX = Config.Bind<int>(
                "Progression Message #4",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg4_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #4",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg4_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #4",
                "Send On Loop Start",
                WhenToSendMsg.Never,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg4_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #4",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg4_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #4",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );

            ProgMsg5_Message = Config.Bind<string>(
                "Progression Message #5",
                "Message to send",
                "",
                "The chat message that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message."
            );
            ProgMsg5_SendOnStageX = Config.Bind<int>(
                "Progression Message #5",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg5_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #5",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg5_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #5",
                "Send On Loop Start",
                WhenToSendMsg.Never,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg5_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #5",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg5_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #5",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );

            ProgMsg6_Message = Config.Bind<string>(
                "Progression Message #6",
                "Message to send",
                "",
                "The chat message that will be sent when the conditions are met. You can have no message or add extra messages in the same ways as the first message."
            );
            ProgMsg6_SendOnStageX = Config.Bind<int>(
                "Progression Message #6",
                "Send On Stage X",
                -1,
                "At the beginning of what stage should the chat message be sent at? Set to -1 for no stage."
            );
            ProgMsg6_ReSendAfterXStagesNum = Config.Bind<int>(
                "Progression Message #6",
                "Send Again After X Stages",
                -1,
                "After how many stages should the message be re-sent? Set to -1 for no re-sending. Does nothing if \"Send On Stage X\" is -1."
            );
            ProgMsg6_SendOnLoopStart = Config.Bind<WhenToSendMsg>(
                "Progression Message #6",
                "Send On Loop Start",
                WhenToSendMsg.Never,
                "Should the message be sent when a loop is started?"
            );
            ProgMsg6_SendOnBazaarVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #6",
                "Send On Bazaar Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the bazaar?"
            );
            ProgMsg6_SendOnVoidFieldsVisit = Config.Bind<WhenToSendMsg>(
                "Progression Message #6",
                "Send On Void Fields Visit",
                WhenToSendMsg.Never,
                "Should the message be sent when you visit the void fields?"
            );
            #endregion

            #region Assigning config values to arrays
            // If there's a less copy-pasty way of doing all this please lmk
            ProgMsgAll_Message[0] = ProgMsg1_Message.Value;
            ProgMsgAll_ReSendAfterXStages[0] = ProgMsg1_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[0] = ProgMsg1_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[0] = ProgMsg1_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[0] = ProgMsg1_SendOnVoidFieldsVisit.Value;

            ProgMsgAll_Message[1] = ProgMsg2_Message.Value;
            ProgMsgAll_ReSendAfterXStages[1] = ProgMsg2_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[1] = ProgMsg2_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[1] = ProgMsg2_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[1] = ProgMsg2_SendOnVoidFieldsVisit.Value;

            ProgMsgAll_Message[2] = ProgMsg3_Message.Value;
            ProgMsgAll_ReSendAfterXStages[2] = ProgMsg3_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[2] = ProgMsg3_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[2] = ProgMsg3_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[2] = ProgMsg3_SendOnVoidFieldsVisit.Value;

            ProgMsgAll_Message[3] = ProgMsg4_Message.Value;
            ProgMsgAll_ReSendAfterXStages[3] = ProgMsg4_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[3] = ProgMsg4_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[3] = ProgMsg4_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[3] = ProgMsg4_SendOnVoidFieldsVisit.Value;

            ProgMsgAll_Message[4] = ProgMsg5_Message.Value;
            ProgMsgAll_ReSendAfterXStages[4] = ProgMsg5_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[4] = ProgMsg5_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[4] = ProgMsg5_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[4] = ProgMsg5_SendOnVoidFieldsVisit.Value;

            ProgMsgAll_Message[5] = ProgMsg6_Message.Value;
            ProgMsgAll_ReSendAfterXStages[5] = ProgMsg6_ReSendAfterXStagesNum.Value;
            ProgMsgAll_SendOnLoopStart[5] = ProgMsg6_SendOnLoopStart.Value;
            ProgMsgAll_SendOnBazaarVisit[5] = ProgMsg6_SendOnBazaarVisit.Value;
            ProgMsgAll_SendOnVoidFieldsVisit[5] = ProgMsg6_SendOnVoidFieldsVisit.Value;

            // This part is in a function because it needs to happen each run start too
            ResetProgMsgAllSendOnStageX();
            #endregion
        }

        private void ResetProgMsgAllSendOnStageX()
        {
            ProgMsgAll_SendOnStageX[0] = ProgMsg1_SendOnStageX.Value;
            ProgMsgAll_SendOnStageX[1] = ProgMsg2_SendOnStageX.Value;
            ProgMsgAll_SendOnStageX[2] = ProgMsg3_SendOnStageX.Value;
            ProgMsgAll_SendOnStageX[3] = ProgMsg4_SendOnStageX.Value;
            ProgMsgAll_SendOnStageX[4] = ProgMsg5_SendOnStageX.Value;
            ProgMsgAll_SendOnStageX[5] = ProgMsg6_SendOnStageX.Value;
        }

        private void SendChatMsg(int ArrayNum)
        {
            WasChatMsgSent = true;
            string MsgToSend;
            string ChosenProgMsg = ProgMsgAll_Message[ArrayNum];

            if (ChosenProgMsg.Contains(MultiMsgSeparator))
            {
                MsgToSend = PickRandomMsgFromMultiMsg(ChosenProgMsg);
            }
            else
            {
                MsgToSend = ChosenProgMsg;
            }

            // Partially ported from ChatMessage.Send in R2API.Utils
            Chat.SimpleChatMessage ChatMsg = new Chat.SimpleChatMessage();
            ChatMsg.baseToken = MsgToSend;
            Chat.SendBroadcastChat(ChatMsg);
        }

        private string PickRandomMsgFromMultiMsg(string MultiMsg)
        {
            string[] MultiMsg_Split = Regex.Split(MultiMsg, MultiMsgSeparator);
            for (int i = 0; i < MultiMsg_Split.Length; i++)
            {
                MultiMsg_Split[i] = MultiMsg_Split[i].Trim();
            }
            Random RNG = new Random();
            int RndMsgIndex = RNG.Next(0, MultiMsg_Split.Length);
           
            return MultiMsg_Split[RndMsgIndex];
        }
    }
}