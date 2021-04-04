using System;
using Geode.Extension;
using Geode.Network;

namespace ChatConsole_CSharp
{
    [Module("ChatConsole_CSharp", "Lilith", "For testing purposes only.")]
    class GeodeExtension : GService
    {
        private int BotFriendID = 999999999;
        private string BotFriendCreatorName = "Lilith";
        private string BotFriendCreatorLook = "hr-3731-45-45.hd-600-10.ha-3734.fa-3276-1412";
        private string BotFriendName = "ChatConsole_CSharp";
        private string BotFriendMotto = "For testing purposes only.";
        private string BotFriendCreationDate = "25-5-2020";
        private string BotFriendLook = "hd-3704-29.ch-3135-95.lg-3136-95";
        private string[] BotFriendBadges = new string[] { "BOT", "FR17A", "NO83", "ITB26", "NL446" };

        public override void OnDataIntercept(DataInterceptedEventArgs data)
        {
            if (data.Packet.Id == Out.GetExtendedProfile.Id)
            {
                int RequestedFriendID = data.Packet.ReadInt32();
                if (RequestedFriendID == BotFriendID)
                {
                    SendToClientAsync(In.ExtendedProfile, BotFriendID, BotFriendName, BotFriendLook, BotFriendMotto, BotFriendCreationDate, 0, 1, true, false, true, 0, -255, true);
                    SendToClientAsync(In.HabboUserBadges, BotFriendID, BotFriendBadges.Length, 1, BotFriendBadges[0], 2, BotFriendBadges[1], 3, BotFriendBadges[2], 4, BotFriendBadges[3], 5, BotFriendBadges[4]);
                    SendToClientAsync(In.RelationshipStatusInfo, BotFriendID, 1, 1, 1, 0, BotFriendCreatorName, BotFriendCreatorLook);
                }
            }

            if (data.Packet.Id == Out.SendMsg.Id)
            {
                int RequestedFriendID = data.Packet.ReadInt32();
                string RequestedFriendText = data.Packet.ReadUTF8();
                if (RequestedFriendID == BotFriendID)
                {
                    data.IsBlocked = true;
                    bool CommandHandled = false;
                    if (RequestedFriendText == "/exit")
                    {
                        CommandHandled = true;
                        HideBotFriend();
                        base.OnDataIntercept(data);
                        Environment.Exit(0);
                    }
                    if (RequestedFriendText.ToLower() == "/help")
                    {
                        CommandHandled = true;
                        BotFriendSendMessage("Commands:");
                        BotFriendSendMessage("/look1 and /look2 to change current look.");
                        BotFriendSendMessage("/sit to force sit.");
                        BotFriendSendMessage("/fx to get light sabber fx.");
                        BotFriendSendMessage("/exit to exit extension.");
                    }
                    if (RequestedFriendText.ToLower() == "/look1")
                    {
                        CommandHandled = true;
                        SendToServerAsync(Out.UpdateFigureData, "F", "ch-665-71.hr-515-45.fa-3276-72.hd-600-10.he-3274-84.lg-3216-73");
                    }
                    if (RequestedFriendText.ToLower() == "/look2")
                    {
                        CommandHandled = true;
                        SendToServerAsync(Out.UpdateFigureData, "M", "ch-235-71.hr-893-45.fa-3276-72.hd-180-10.he-3274-84.lg-3290-82");
                    }
                    if (RequestedFriendText.ToLower() == "/sit")
                    {
                        CommandHandled = true;
                        SendToServerAsync(Out.ChangePosture, 1);
                    }
                    if (RequestedFriendText.ToLower() == "/fx")
                    {
                        CommandHandled = true;
                        SendToServerAsync(Out.Chat, ":yyxxabxa", 0, -1);
                    }
                    if (CommandHandled == false)
                        BotFriendWelcome();
                }
            }

            base.OnDataIntercept(data);
        }

        [InDataCapture("FriendRequests")]
        public void OnFriendRequests(DataInterceptedEventArgs e)
        {
            ShowBotFriend();
            BotFriendWelcome();
        }

        public void BotFriendWelcome()
        {
            BotFriendSendMessage("Welcome |");
            BotFriendSendMessage("Use /help to get info.");
        }

        public void BotFriendSendMessage(string Message)
        {
            SendToClientAsync(In.NewConsole, BotFriendID, Message, 0, "");
        }

        public void ShowBotFriend()
        {
            int CreatorRelation = 65537;
            SendToClientAsync(In.FriendListUpdate, 0, 1, false, false, "", BotFriendID, "[BOT] " + BotFriendName, 1, true, false, BotFriendLook, 0, "", 0, true, true, true, CreatorRelation);
        }

        public void HideBotFriend()
        {
            SendToClientAsync(In.FriendListUpdate, 0, 1, -1, BotFriendID);
        }

        public override void OnConnected(Geode.Network.Protocol.HPacket packet)
        {
            base.OnConnected(packet);
            ShowBotFriend();
            BotFriendWelcome();
        }

        public override void OnCriticalError(string error_desc)
        {
            base.OnCriticalError(error_desc);
            Environment.Exit(0);
        }
    }

}
