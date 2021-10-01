Imports Geode.Extension
Imports Geode.Habbo.Packages
Imports Geode.Habbo.Packages.StuffData

Class MainWindow
    Public WithEvents Extension As GeodeExtension
    Public WithEvents ConsoleBot As ConsoleBot
    Public MyBlackHoles As New List(Of HFloorObject)

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Visibility = Visibility.Hidden 'Hide window on startup
        Extension = New GeodeExtension("BlackHoleSun", "Geode examples.", "Lilith") 'Instantiate extension
        Extension.Start() 'Start extension
        ConsoleBot = New ConsoleBot(Extension, "BlackHoleSun") 'Instantiate a new ConsoleBot
        ConsoleBot.ShowBot() 'Show ConsoleBot
    End Sub

    Sub BotWelcome()
        ConsoleBot.BotSendMessage("Welcome |")
        ConsoleBot.BotSendMessage("Use /hide or /show to handle black holes.")
    End Sub

    Private Sub ConsoleBot_OnBotLoaded(e As String) Handles ConsoleBot.OnBotLoaded
        BotWelcome() 'Show welcome message when ConsoleBot loaded
    End Sub

    Async Function HideBlackHoles() As Task
        If Extension.HotelServer.Hotel = Geode.Habbo.HHotel.Es Then
            For Each x In Extension.FloorObjects
                If x.Value.TypeId = 3782 Then
                    If CType(x.Value.StuffData, HLegacyStuffData).Data = "0" Then
                        Await Extension.SendToClientAsync(Extension.In.ObjectDataUpdate, x.Value.Id.ToString, 0, "1")
                        If MyBlackHoles.Contains(x.Value) = False Then
                            MyBlackHoles.Add(x.Value)
                        End If
                    End If
                End If
            Next
            If MyBlackHoles.Count = 0 Then
                ConsoleBot.BotSendMessage("Black holes not detected, try reloading the room.")
            Else
                ConsoleBot.BotSendMessage(MyBlackHoles.Count.ToString() & " black holes were hidden.")
            End If
        Else
            ConsoleBot.BotSendMessage("Not supported on this hotel!")
        End If
    End Function

    Async Function ShowBlackHoles() As Task
        For Each x In MyBlackHoles
            Await Extension.SendToClientAsync(Extension.In.ObjectDataUpdate, x.Id.ToString, 0, "0")
        Next
        If MyBlackHoles.Count = 0 Then
            ConsoleBot.BotSendMessage("Black holes not detected, try reloading the room.")
        Else
            ConsoleBot.BotSendMessage(MyBlackHoles.Count.ToString() & " black holes were shown.")
        End If
    End Function

    Private Sub ConsoleBot_OnMessageReceived(e As String) Handles ConsoleBot.OnMessageReceived
        Select Case e.ToLower 'Handle received message
            Case "/hide"
                HideBlackHoles()
            Case "/show"
                ShowBlackHoles()
            Case Else
                BotWelcome()
        End Select
    End Sub

    Private Sub Extension_OnCriticalErrorEvent(e As String) Handles Extension.OnCriticalErrorEvent
        Visibility = Visibility.Visible
        ShowInTaskbar = True
        Activate()
        MsgBox(e & ".", MsgBoxStyle.Critical, "Critical error") 'Show extension critical error
        Environment.Exit(0)
    End Sub

    Private Sub Extension_OnFloorObjectsLoadedEvent(e As Integer) Handles Extension.OnFloorObjectsLoadedEvent
        MyBlackHoles.Clear()
    End Sub
End Class