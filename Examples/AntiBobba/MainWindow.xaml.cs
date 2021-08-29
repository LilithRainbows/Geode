using System;
using System.Text;
using System.Windows;
using Geode.Extension;
using Geode.Network;

namespace AntiBobba
{
    //To do: Public chat/whisper must be trimmed to 66 characters and console to 43.
    public partial class MainWindow : Window
    {
        public ConsoleBot ConsoleBot;
        public GeodeExtension Extension;
        public bool IsBotEnabled = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden; //Hide window on startup
            Extension = new GeodeExtension("AntiBobba", "Geode examples.", "Lilith"); // Instantiate extension
            //Add extension event handlers
            Extension.OnDataInterceptEvent += Extension_OnDataInterceptEvent;
            Extension.OnCriticalErrorEvent += Extension_OnCriticalErrorEvent;
            //
            Extension.Start(); //Start extension
            ConsoleBot = new ConsoleBot(Extension, "AntiBobba"); // Instantiate a new ConsoleBot
            //Add ConsoleBot event handlers
            ConsoleBot.OnMessageReceived += ConsoleBot_OnMessageReceived;
            ConsoleBot.OnBotLoaded += ConsoleBot_OnBotLoaded;
            //
            ConsoleBot.ShowBot(); //Show ConsoleBot
        }

        public void BotWelcome()
        {
            ConsoleBot.BotSendMessage("Welcome |");
            ShowEnabledInfo();
        }

        public void ShowEnabledInfo()
        {
            ConsoleBot.BotSendMessage("This tool can lead to account ban/mute, use at your own risk!");
            if (IsBotEnabled == true)
            {
                ConsoleBot.BotSendMessage("BobbaBlock is enabled, use /stop to stop.");
            }
            else
            {
                ConsoleBot.BotSendMessage("BobbaBlock is disabled, use /start to start.");
            }
        }

        public void StartFilter()
        {
            IsBotEnabled = true;
            ShowEnabledInfo();
        }

        public void StopFilter()
        {
            IsBotEnabled = false;
            ShowEnabledInfo();
        }

        public string BypassFilter(string input)
        {
            string[] AllowedWords = new string[] { ":sit", ":stand", ":habnam", ":moonwalk" };
            foreach (string AllowedWord in AllowedWords)
            {
                if (AllowedWord == input)
                {
                    return input;
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (char value in input)
            {
                stringBuilder.Append("ӵӵ"); //Alternative: ѵѫ)
                stringBuilder.Append(value);
            }
            stringBuilder.Append("ӵӵ"); //Alternative: ѵѫ
            return stringBuilder.ToString();
        }

        private void ConsoleBot_OnBotLoaded(string e)
        {
            BotWelcome(); //Show welcome message when ConsoleBot loaded
        }

        private void ConsoleBot_OnMessageReceived(string e)
        {
            switch (e.ToLower()) //Handle received message
            {
                case "/start":
                    {
                        StartFilter();
                        break;
                    }

                case "/stop":
                    {
                        StopFilter();
                        break;
                    }

                default:
                    {
                        BotWelcome();
                        break;
                    }
            }
        }

        private void Extension_OnDataInterceptEvent(DataInterceptedEventArgs e)
        {
            if (Extension.Out.Chat.Match(e) && IsBotEnabled) //Public chat
            {
                e.IsBlocked = true;
                Extension.SendToServerAsync(Extension.Out.Chat, BypassFilter(e.Packet.ReadUTF8()), e.Packet.ReadInt32(), e.Packet.ReadInt32());
            }
            if (Extension.Out.Whisper.Match(e) && IsBotEnabled) //Whisper
            {
                string WhisperOriginal = e.Packet.ReadUTF8();
                string WhisperDestination = WhisperOriginal.Remove(WhisperOriginal.IndexOf(" "));
                string WhisperMessage = WhisperOriginal.Remove(0, WhisperOriginal.IndexOf(" ") + 1);
                string WhisperModded = WhisperDestination + " " + BypassFilter(WhisperMessage);
                e.IsBlocked = true;
                Extension.SendToServerAsync(Extension.Out.Whisper, WhisperModded, e.Packet.ReadInt32());
            }
            if (Extension.Out.SendMsg.Match(e) && IsBotEnabled) //Console chat
            {
                int MessageDestination = e.Packet.ReadInt32();
                if (MessageDestination == ConsoleBot.BotID)
                {
                    e.Packet.Position = 0; //Restore packet read position if destination is console bot
                }
                else
                {
                    string MessageModded = BypassFilter(e.Packet.ReadUTF8());
                    e.IsBlocked = true;
                    Extension.SendToServerAsync(Extension.Out.SendMsg, MessageDestination, MessageModded);
                }
            }
        }

        private void Extension_OnCriticalErrorEvent(string e)
        {
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
            Activate();
            MessageBox.Show(e + ".", "Critical error", MessageBoxButton.OK, MessageBoxImage.Error); //Show extension critical error
            Environment.Exit(0);
        }
    }
}
