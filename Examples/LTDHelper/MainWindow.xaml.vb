Imports Geode.Extension
Imports Geode.Network

Class MainWindow
    Public WithEvents Extension As GeodeExtension
    Public WithEvents ConsoleBot As ConsoleBot
    Public TaskStarted As Boolean = False
    Public TaskBlocked As Boolean = False

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Visibility = Visibility.Hidden 'Hide window on startup
        Extension = New GeodeExtension("LTDHelper", "Geode examples.", "Lilith") 'Instantiate extension
        Extension.Start() 'Start extension
        ConsoleBot = New ConsoleBot(Extension, "LTDHelper") 'Instantiate a new ConsoleBot
        ConsoleBot.ShowBot() 'Show ConsoleBot
    End Sub

    Sub BotWelcome()
        ConsoleBot.BotSendMessage("Welcome |")
        ConsoleBot.BotSendMessage("Use /start or /stop")
    End Sub

    Async Function TryToBuyLTD() As Task
        If TaskStarted Then
            Try
                Dim SW As Stopwatch = Stopwatch.StartNew()
                Extension.SendToServerAsync(Extension.Out.GetCatalogIndex, "NORMAL")
                Dim CatalogIndexData = Await Extension.WaitForPacketAsync(Extension.In.CatalogIndex, 1000)
                Dim CatalogRoot As New Geode.Habbo.Packages.HCatalogNode(CatalogIndexData.Packet)
                Dim LTDCategory = FindCatalogCategory(CatalogRoot.Children, "ler") 'You can test with other category like: set_mode
                Dim CurrentTimeout = 125 - SW.ElapsedMilliseconds
                If CurrentTimeout > 0 Then
                    Await Task.Delay(CurrentTimeout) 'We need to ensure at least 125ms timeout since first packet
                End If
                Extension.SendToServerAsync(Extension.Out.PurchaseFromCatalog, LTDCategory.PageId, LTDCategory.OfferIds(0), "", 1)
                If Await Extension.WaitForPacketAsync(Extension.In.PurchaseOK, 500) IsNot Nothing Then
                    ConsoleBot.BotSendMessage("Successfully purchased an LTD.")
                    TaskBlocked = True
                    TaskStarted = False
                Else
                    Throw New Exception("LTD not purchased!")
                End If
                ConsoleBot.BotSendMessage("Use /exit to finish.")
            Catch
                TryToBuyLTD()
            End Try
        End If
    End Function

    Private Function FindCatalogCategory(NodeChildrens As Geode.Habbo.Packages.HCatalogNode(), CategoryName As String) As Geode.Habbo.Packages.HCatalogNode
        For Each NodeChild In NodeChildrens
            If NodeChild.PageName = CategoryName Then
                Return NodeChild
            Else
                Dim RecursiveSearchResult = FindCatalogCategory(NodeChild.Children, CategoryName)
                If RecursiveSearchResult IsNot Nothing Then
                    Return RecursiveSearchResult
                End If
            End If
        Next
        Return Nothing
    End Function

    Private Sub ConsoleBot_OnBotLoaded(e As String) Handles ConsoleBot.OnBotLoaded
        BotWelcome() 'Show welcome message when ConsoleBot loaded
    End Sub

    Private Sub ConsoleBot_OnMessageReceived(e As String) Handles ConsoleBot.OnMessageReceived
        If TaskBlocked = False Then
            Select Case e.ToLower 'Handle received message
                Case "/start"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage("Ok, i will try to buy the LTD, you can use /stop to finish.")
                        TaskStarted = True
                        TryToBuyLTD()
                    End If
                Case "/stop"
                    If TaskStarted Then
                        ConsoleBot.BotSendMessage("Stopped, you can use /start to try again.")
                        TaskStarted = False
                    End If
                Case Else
                    BotWelcome()
            End Select
        Else
            ConsoleBot.BotSendMessage("Use /exit to finish.")
        End If
    End Sub

    Private Sub Extension_OnDataInterceptEvent(e As DataInterceptedEventArgs) Handles Extension.OnDataInterceptEvent
        If Extension.In.ErrorReport.Match(e) Or Extension.In.PurchaseError.Match(e) Or Extension.In.PurchaseNotAllowed.Match(e) Or Extension.In.NotEnoughBalance.Match(e) Then 'Ignore common purchase errors
            If TaskStarted = True Then
                e.IsBlocked = True
            End If
        End If
    End Sub

    Private Sub Extension_OnCriticalErrorEvent(e As String) Handles Extension.OnCriticalErrorEvent
        Visibility = Visibility.Visible
        ShowInTaskbar = True
        Activate()
        MsgBox(e & ".", MsgBoxStyle.Critical, "Critical error") 'Show extension critical error
        Environment.Exit(0)
    End Sub
End Class

Module SingleInstance
    Sub Main()
        Dim noPreviousInstance As Boolean

        Using m As New Threading.Mutex(True, "LTDHelper for Geode", noPreviousInstance)
            If Not noPreviousInstance Then
                MessageBox.Show("Extension is already started!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Else
                Dim mainWindow As New MainWindow()
                Dim app As New Application()
                app.Run(mainWindow)
            End If
        End Using
    End Sub
End Module