using System;
using System.Windows;
using Geode.Extension;

namespace ConsoleBotCSharp
{
    public partial class MainWindow : Window
    {
        public ConsoleBot ConsoleBot;
        public GeodeExtension Extension;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden; //Hide window on startup
            Extension = new GeodeExtension("ConsoleBotCSharp", "Geode examples.", "Lilith"); //Instantiate extension
            //Add extension event handlers
            Extension.OnCriticalErrorEvent += Extension_OnCriticalErrorEvent;
            //
            Extension.Start(); //Start extension
            ConsoleBot = new ConsoleBot(Extension, "CSharp example"); //Instantiate a new ConsoleBot
            //Add ConsoleBot event handlers
            ConsoleBot.OnMessageReceived += ConsoleBot_OnMessageReceived;
            ConsoleBot.OnBotLoaded += ConsoleBot_OnBotLoaded; ;
            //
            ConsoleBot.ShowBot(); //Show ConsoleBot
        }

        public void BotWelcome()
        {
            ConsoleBot.BotSendMessage("Welcome |");
            ConsoleBot.BotSendMessage("Use /help to get info.");
        }

        private void ConsoleBot_OnBotLoaded(string e)
        {
            BotWelcome(); //Show welcome message when ConsoleBot loaded
        }

        private void ConsoleBot_OnMessageReceived(string e)
        {
            switch (e.ToLower()) //Handle received message
            {
                case "/help":
                    {
                        ConsoleBot.BotSendMessage("Commands:");
                        ConsoleBot.BotSendMessage("/look1 and /look2 to change current look.");
                        ConsoleBot.BotSendMessage("/sit to force sit.");
                        ConsoleBot.BotSendMessage("/fx to get light sabber fx.");
                        ConsoleBot.BotSendMessage("/exit to exit extension.");
                        break;
                    }

                case "/look1":
                    {
                        Extension.SendToServerAsync(Extension.Out.UpdateFigureData, "F", "hr-515-45.ch-665-71.lg-3216-73.hd-600-10.fa-3276-72");
                        break;
                    }

                case "/look2":
                    {
                        Extension.SendToServerAsync(Extension.Out.UpdateFigureData, "M", "hr-893-45.ch-235-71.lg-3290-82.hd-180-10.fa-3276-72");
                        break;
                    }

                case "/sit":
                    {
                        Extension.SendToServerAsync(Extension.Out.ChangePosture, 1);
                        break;
                    }

                case "/fx":
                    {
                        Extension.SendToServerAsync(Extension.Out.Chat, ":yyxxabxa", 0, -1);
                        break;
                    }

                default:
                    {
                        BotWelcome();
                        break;
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
