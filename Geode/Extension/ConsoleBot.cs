using Geode.Network;
using System;

namespace Geode.Extension
{
    public class ConsoleBot
    {
        private GeodeExtension Extension;
        public readonly int BotID = new Random().Next(999000000, 999999999);
        public string BotName { get; private set; }
        public string BotMotto { get; private set; }
        public string BotLook { get; private set; }
        public string BotBadges { get; private set; }
        public string BotCreationDate { get; private set; }
        public string BotCreatorName { get; private set; }
        public string BotCreatorLook { get; private set; }
        private bool IsBotVisible = false;
        public event Action<string> OnBotLoaded;
        public event Action<string> OnMessageReceived;

        public ConsoleBot(GeodeExtension Extension, string BotName, string BotMotto = "Console bot.", string BotLook = "hd-3704-29.ch-3135-95.lg-3136-95", string BotBadges = "BOT FR17A NO83 ITB26 NL446", string BotCreationDate = "W-W-1984", string BotCreatorName = "Lilith", string BotCreatorLook = "hr-3870-45.hd-600-10.ch-665-71.lg-3781-100-71.ha-3614-91-95.he-3469-1412.fa-3276-1412.ca-3702-71-71")
        {
            {
                this.Extension = Extension;
                this.Extension.OnDataInterceptEvent += Extension_OnDataInterceptEvent;
                this.BotName = BotName;
                this.BotMotto = BotMotto;
                this.BotLook = BotLook;
                this.BotBadges = BotBadges;
                this.BotCreationDate = BotCreationDate;
                this.BotCreatorName = BotCreatorName;
                this.BotCreatorLook = BotCreatorLook;
                this.Extension.OnConnectedEvent += Extension_OnConnectedEvent;
                this.Extension.OnStoppingEvent += Extension_OnStoppingEvent;
            }
        }

        private void Extension_OnConnectedEvent(Network.Protocol.HPacket e)
        {
            if (IsBotVisible)
            {
                ShowBot();
                OnBotLoaded.Invoke("DefaultLoad");
            }
        }

        private void Extension_OnStoppingEvent()
        {
            if (IsBotVisible)
            {
                HideBot(); //Hide bot before extension closes
            }
        }

        private void Extension_OnDataInterceptEvent(DataInterceptedEventArgs e)
        {
            if (Extension.Out.SendMsg.Match(e)) // User sent a message.
            {
                int RequestedFriendID = e.Packet.ReadInt32();
                string RequestedMessage = e.Packet.ReadUTF8();
                if (RequestedFriendID == BotID) // Bot received a message.
                {
                    e.IsBlocked = true;
                    OnMessageReceived.Invoke(RequestedMessage);
                    if (RequestedMessage.ToLower() == "/exit")
                    {
                        HideBot();
                        Extension.DisableEventHandlers = true; // To avoid infinite event handler loop
                        Extension.OnDataIntercept(e);
                        Environment.Exit(0);
                    }
                }
            }

            if (Extension.Out.RemoveFriend.Match(e)) // User requested a friend remove.
            {
                int RequestedFriendsCount = e.Packet.ReadInt32();
                for (int i = 0; i < RequestedFriendsCount; i++) // Iterate requested friends
                {
                    int RequestedFriendID = e.Packet.ReadInt32();
                    if (RequestedFriendID == BotID) // Bot remove was requested.
                    {
                        e.IsBlocked = true;
                        HideBot();
                        Extension.DisableEventHandlers = true; // To avoid infinite event handler loop
                        Extension.OnDataIntercept(e);
                        Environment.Exit(0);
                    }
                }
            }

            if (Extension.Out.GetExtendedProfile.Match(e)) // Bot profile was requested.
            {
                int RequestedFriendID = e.Packet.ReadInt32();
                if (RequestedFriendID == BotID)
                {
                    e.IsBlocked = true;
                    var BotBadgesArray = new string[] { "", "", "", "", "" };
                    for (int i = 0; i <= BotBadges.Split(' ').Length - 1; i++)
                    {
                        BotBadgesArray[i] = BotBadges.Split(' ')[i];
                    }
                    Extension.SendToClientAsync(Extension.In.ExtendedProfile, BotID, BotName, BotLook, BotMotto, BotCreationDate, 0, 1, true, false, true, 0, -255, true);
                    Extension.SendToClientAsync(Extension.In.HabboUserBadges, BotID, BotBadgesArray.Length, 1, BotBadgesArray[0], 2, BotBadgesArray[1], 3, BotBadgesArray[2], 4, BotBadgesArray[3], 5, BotBadgesArray[4]);
                    Extension.SendToClientAsync(Extension.In.RelationshipStatusInfo, BotID, 1, 1, 1, 0, BotCreatorName, BotCreatorLook);
                }
            }

            if (Extension.In.FriendRequests.Match(e) && IsBotVisible) // Show Bot when the initial console load is complete.
            {
                ShowBot();
                OnBotLoaded.Invoke("ConsoleLoad");
            }
        }

        public void BotSendMessage(string Message)
        {
            Extension.SendToClientAsync(Extension.In.NewConsole, BotID, Message, 0, "");
        }

        public void ShowBot()
        {
            if (Extension.IsConnected)
            {
                HideBot();
                int CreatorRelation = 65537;
                Extension.SendToClientAsync(Extension.In.FriendListUpdate, 0, 1, false, false, "", BotID, '\u0001' + "[BOT] " + BotName, 1, true, false, BotLook, 0, "", 0, true, true, true, CreatorRelation);
            }
            IsBotVisible = true;
        }

        public void HideBot()
        {
            if (Extension.IsConnected)
            {
                Extension.SendToClientAsync(Extension.In.FriendListUpdate, 0, 1, -1, BotID);
            }
            IsBotVisible = false;
        }
    }
}
