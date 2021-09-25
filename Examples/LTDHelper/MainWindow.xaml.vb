Imports Geode.Extension
Imports Geode.Network

Class MainWindow
    Public CurrentLanguageInt As Integer = 0
    Public WithEvents Extension As GeodeExtension
    Public WithEvents ConsoleBot As ConsoleBot
    Public TaskStarted As Boolean = False
    Public TaskBlocked As Boolean = False
    Public TestMode As Boolean = False
    Public CatalogCategory As String() = {"ler", "set_mode"}
    Public TaskCanBeStopped As Boolean = False

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Visibility = Visibility.Hidden 'Hide window on startup
        If System.Globalization.CultureInfo.CurrentCulture.Name.ToLower.StartsWith("es") Then
            CurrentLanguageInt = 1
        End If
        Extension = New GeodeExtension("LTDHelper", "Geode examples.", "Lilith") 'Instantiate extension
        Extension.Start() 'Start extension
        ConsoleBot = New ConsoleBot(Extension, "LTDHelper") 'Instantiate a new ConsoleBot
        ConsoleBot.ShowBot() 'Show ConsoleBot
    End Sub

    Sub BotWelcome()
        ConsoleBot.BotSendMessage(AppTranslator.WelcomeMessage(CurrentLanguageInt))
        ConsoleBot.BotSendMessage(AppTranslator.BuyAdvice(CurrentLanguageInt))
        ConsoleBot.BotSendMessage(AppTranslator.RiskAdvice(CurrentLanguageInt))
        ConsoleBot.BotSendMessage(AppTranslator.FullCommandsList(CurrentLanguageInt))
    End Sub

    Async Function TryToBuyLTD() As Task
        TaskCanBeStopped = False
        If TaskStarted = True Then
            Try
                Await Task.Delay(New Random().Next(500, 1000))
                Extension.SendToServerAsync(Extension.Out.GetCatalogIndex, "NORMAL")
                Dim CatalogIndexData = Await Extension.WaitForPacketAsync(Extension.In.CatalogIndex, 4000)
                ConsoleBot.BotSendMessage(AppTranslator.CatalogIndexLoaded(CurrentLanguageInt))
                Dim CatalogRoot As New Geode.Habbo.Packages.HCatalogNode(CatalogIndexData.Packet)
                Dim LTDCategory = FindCatalogCategory(CatalogRoot.Children, CatalogCategory(Convert.ToInt32(TestMode)))
                Await Task.Delay(New Random().Next(500, 1000))
                Extension.SendToServerAsync(Extension.Out.GetCatalogPage, LTDCategory.PageId, -1, "NORMAL")
                ConsoleBot.BotSendMessage(AppTranslator.SimulatingPageClick(CurrentLanguageInt))
                Await Task.Delay(New Random().Next(500, 1000))
                ConsoleBot.BotSendMessage(AppTranslator.TryingToBuy(CurrentLanguageInt))
                Extension.SendToServerAsync(Extension.Out.PurchaseFromCatalog, LTDCategory.PageId, LTDCategory.OfferIds(0), "", 1)
                If Await Extension.WaitForPacketAsync(Extension.In.PurchaseOK, 2000) IsNot Nothing Then
                    ConsoleBot.BotSendMessage(AppTranslator.PurchaseOK(CurrentLanguageInt))
                    TaskBlocked = True
                    TaskStarted = False
                Else
                    Throw New Exception("LTD not purchased!")
                End If
                ConsoleBot.BotSendMessage(AppTranslator.ExitAdvice(CurrentLanguageInt))
            Catch
                ConsoleBot.BotSendMessage(AppTranslator.PurchaseFailed(CurrentLanguageInt))
            End Try
        End If
        TaskCanBeStopped = True
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
                Case "/test"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = True
                        TaskStarted = True
                        TryToBuyLTD()
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/force"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = False
                        TaskStarted = True
                        TryToBuyLTD()
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/start"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = False
                        TaskStarted = True
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/stop"
                    If TaskStarted Then
                        If TaskCanBeStopped = True Then
                            ConsoleBot.BotSendMessage(AppTranslator.StoppedMessage(CurrentLanguageInt))
                            TaskStarted = False
                        Else
                            ConsoleBot.BotSendMessage(AppTranslator.StopFailed(CurrentLanguageInt))
                        End If
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.FullCommandsList(CurrentLanguageInt))
                    End If
                Case Else
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.FullCommandsList(CurrentLanguageInt))
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
            End Select
        Else
            ConsoleBot.BotSendMessage(AppTranslator.ExitAdvice(CurrentLanguageInt))
        End If
    End Sub

    Private Sub Extension_OnDataInterceptEvent(e As DataInterceptedEventArgs) Handles Extension.OnDataInterceptEvent
        If Extension.In.ErrorReport.Match(e) Or Extension.In.PurchaseError.Match(e) Or Extension.In.PurchaseNotAllowed.Match(e) Or Extension.In.NotEnoughBalance.Match(e) Then 'Ignore common purchase errors
            If TaskStarted = True Then
                e.IsBlocked = True
            End If
        End If
        If Extension.In.CatalogPublished.Match(e) Then
            If TaskStarted = True Then
                ConsoleBot.BotSendMessage(AppTranslator.CatalogUpdateReceived(CurrentLanguageInt))
                TryToBuyLTD()
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

Public Class AppTranslator
    '0=English 1=Spanish 2=Portuguese-BR
    Public Shared WelcomeMessage As String() = {
        "Welcome |",
        "Bienvenidx |",
        "Bem-vindo |"
    }
    Public Shared FullCommandsList As String() = {
        "Available commands: /start /stop /force /test /exit",
        "Comandos disponibles: /start /stop /force /test /exit",
        "Comando disponíveis: /start /stop /force /test /exit"
    }
    Public Shared ReducedCommandsList As String() = {
        "Available commands: /stop /exit",
        "Comandos disponibles: /stop /exit",
        "Comandos disponíveis: /stop /exit"
    }
    Public Shared BuyAdvice As String() = {
        "It is recommended to have the catalog closed and not be in a room.",
        "Se recomienda dejar el catalogo cerrado y no estar en una sala.",
        "É recomendado deixar o catálogo fechado e não estar em uma sala."
    }
    Public Shared RiskAdvice As String() = {
        "Use at your own risk!",
        "Usala bajo tu propio riesgo!",
        "Use pelo seu próprio risco!"
    }
    Public Shared PurchaseOK As String() = {
        "Successfully purchased an LTD |",
        "Adquiriste exitosamente un LTD |",
        "Comprou com sucesso um LTD |"
    }
    Public Shared PurchaseFailed As String() = {
        "Error while purshasing an LTD!",
        "Error al comprar un LTD!",
        "Erro ao comprar um LTD!"
    }
    Public Shared ExitAdvice As String() = {
        "Use /exit to finish.",
        "Usa /exit para finalizar.",
        "Use /exit para finalizar."
    }
    Public Shared StartedMessage As String() = {
        "I will try to buy the LTD, you can use /stop to finish.",
        "Intentare comprar un LTD, puedes usar /stop para finalizar.",
        "Vou tentar comprar o LTD, você pode usar /stop para finalizar."
    }
    Public Shared StoppedMessage As String() = {
        "Stopped, you can use /start to try again.",
        "Detenida, puedes usar /start para reintentar.",
        "Parado, você pode usar /start para tentar novamente."
    }
    Public Shared CatalogIndexLoaded As String() = {
        "[Catalog index loaded]",
        "[Indice del catalogo cargado]",
        "[Índice do catálogo carregado]"
    }
    Public Shared SimulatingPageClick As String() = {
        "[Simulating page click]",
        "[Simulando clic de pagina]",
        "[Simulando um clique de página]"
    }
    Public Shared TryingToBuy As String() = {
        "[Trying to buy]",
        "[Intentando comprar]",
        "[Tentando comprar]"
    }
    Public Shared CatalogUpdateReceived As String() = {
        "[Catalog update received]",
        "[Actualizacion del catalogo recibida]",
        "[Atualização do catálogo recebida]"
    }
    Public Shared StopFailed As String() = {
        "Task cannot be stopped right now!",
        "La tarea no puede detenerse ahora!",
        "A tarefa não pode ser parada no momento!"
}
End Class