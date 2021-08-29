Imports Geode.Extension

Class MainWindow
    Public WithEvents Extension As GeodeExtension
    Public WithEvents ConsoleBot As ConsoleBot

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Extension = New GeodeExtension("ConsoleBotVB", "Geode examples.", "Lilith") 'Instantiate extension
        Extension.Start() 'Start extension
        ConsoleBot = New ConsoleBot(Extension, "VB example") 'Instantiate a new ConsoleBot
        ConsoleBot.ShowBot() 'Show ConsoleBot
    End Sub

    Sub BotWelcome()
        ConsoleBot.BotSendMessage("Welcome |")
        ConsoleBot.BotSendMessage("Use /help to get info.")
    End Sub

    Private Sub ConsoleBot_OnBotLoaded(e As String) Handles ConsoleBot.OnBotLoaded
        BotWelcome() 'Show welcome message when ConsoleBot loaded
    End Sub

    Private Sub ConsoleBot_OnMessageReceived(e As String) Handles ConsoleBot.OnMessageReceived
        Select Case e.ToLower 'Handle received message
            Case "/help"
                ConsoleBot.BotSendMessage("Commands:")
                ConsoleBot.BotSendMessage("/look1 and /look2 to change current look.")
                ConsoleBot.BotSendMessage("/sit to force sit.")
                ConsoleBot.BotSendMessage("/fx to get light sabber fx.")
                ConsoleBot.BotSendMessage("/exit to exit extension.")
            Case "/look1"
                Extension.SendToServerAsync(Extension.Out.UpdateFigureData, "F", "hr-515-45.ch-665-71.lg-3216-73.hd-600-10.fa-3276-72")
            Case "/look2"
                Extension.SendToServerAsync(Extension.Out.UpdateFigureData, "M", "hr-893-45.ch-235-71.lg-3290-82.hd-180-10.fa-3276-72")
            Case "/sit"
                Extension.SendToServerAsync(Extension.Out.ChangePosture, 1)
            Case "/fx"
                Extension.SendToServerAsync(Extension.Out.Chat, ":yyxxabxa", 0, -1)
            Case Else
                BotWelcome()
        End Select
    End Sub

    Private Sub Extension_OnCriticalErrorEvent(e As String) Handles Extension.OnCriticalErrorEvent
        ShowInTaskbar = True
        Activate()
        MsgBox(e & ".", MsgBoxStyle.Critical, "Critical error") 'Show extension critical error
        Environment.Exit(0)
    End Sub

End Class
