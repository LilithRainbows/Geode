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
    Public TaskCanBeStopped As Boolean = True
    Public PurchaseStepsDelayMs As Integer = -666
    Public CreditLimit As Integer = 0
    Public DiamondLimit As Integer = 0

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Visibility = Visibility.Hidden 'Hide window on startup
        If Globalization.CultureInfo.CurrentCulture.Name.ToLower.StartsWith("es") Then
            CurrentLanguageInt = 1
        End If
        If Globalization.CultureInfo.CurrentCulture.Name.ToLower.StartsWith("pt") Then
            CurrentLanguageInt = 2
        End If
        Extension = New GeodeExtension("LTDHelper", "Geode examples.", "Lilith") 'Instantiate extension
        Extension.Start(GetReadyToConnectGEarthPort) 'Start extension
        ConsoleBot = New ConsoleBot(Extension, "LTDHelper") 'Instantiate a new ConsoleBot
        ConsoleBot.ShowBot() 'Show ConsoleBot
    End Sub

    Function GetReadyToConnectGEarthPort() As Integer
        Dim NetStatProcess As New Process()
        NetStatProcess.StartInfo.FileName = "netstat.exe"
        NetStatProcess.StartInfo.Arguments = "-ano"
        NetStatProcess.StartInfo.RedirectStandardOutput = True
        NetStatProcess.StartInfo.UseShellExecute = False
        NetStatProcess.StartInfo.CreateNoWindow = True
        NetStatProcess.Start()
        Dim NSOutput = NetStatProcess.StandardOutput.ReadToEnd()
        Dim ExtensionProcesses = Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName)
        Dim DefaultPort = 9092
        For PossiblePort As Integer = DefaultPort To DefaultPort + 100
            Dim NSPidFilterOK As Boolean = False
            Dim ExtensionIsUsingCurrentPort = False
            Dim IsPortListened = False
            For Each NSLine In NSOutput.Split(Environment.NewLine)
                If NSLine.EndsWith("PID") Then
                    NSPidFilterOK = True
                End If
                If NSPidFilterOK Then
                    For Each ExtensionProcess In ExtensionProcesses
                        If NSLine.EndsWith(" " & ExtensionProcess.Id) Then
                            If NSLine.Contains(":" & PossiblePort & " ") Then
                                ExtensionIsUsingCurrentPort = True
                            End If
                        End If
                    Next
                    If NSLine.Contains(":" & PossiblePort & " ") And NSLine.Contains("LISTENING") Then
                        IsPortListened = True
                    End If
                End If
            Next
            If IsPortListened And ExtensionIsUsingCurrentPort = False Then
                Return PossiblePort
            End If
        Next
        MsgBox("Could not detect an available GEarth port.", MsgBoxStyle.Critical, "Error")
        Process.GetCurrentProcess.Kill()
        Return DefaultPort
    End Function

    Sub BotWelcome()
        ConsoleBot.BotSendMessage(AppTranslator.WelcomeMessage(CurrentLanguageInt))
        ConsoleBot.BotSendMessage(AppTranslator.BuyAdvice(CurrentLanguageInt))
        ConsoleBot.BotSendMessage(AppTranslator.RiskAdvice(CurrentLanguageInt))
        ConsoleBot.BotSendMessage("")
        ConsoleBot.BotSendMessage(AppTranslator.PurchaseStepsDelaySet(CurrentLanguageInt))
    End Sub

    Async Function TryToBuyLTD() As Task
        TaskCanBeStopped = False
        If TaskStarted = True Then
            Try
                Extension.SendToServerAsync(Extension.Out.GetCatalogIndex, "NORMAL")
                Dim CatalogIndexData = Await Extension.WaitForPacketAsync(Extension.In.CatalogIndex, 4000)
                ConsoleBot.BotSendMessage(AppTranslator.CatalogIndexLoaded(CurrentLanguageInt))
                Dim CatalogRoot As New Geode.Habbo.Packages.HCatalogNode(CatalogIndexData.Packet)
                Dim LTDCategory = FindCatalogCategory(CatalogRoot.Children, CatalogCategory(Convert.ToInt32(TestMode)))
                If LTDCategory.Visible = False Then
                    ConsoleBot.BotSendMessage(AppTranslator.LtdCategoryVisibilityError(CurrentLanguageInt))
                    Throw New Exception("LTDCategory is not yet visible!")
                End If
                Await Task.Delay(PurchaseStepsDelayMs)
                Extension.SendToServerAsync(Extension.Out.GetCatalogPage, LTDCategory.PageId, -1, "NORMAL")
                Dim LTDPageData = Await Extension.WaitForPacketAsync(Extension.In.CatalogPage, 2000)
                ConsoleBot.BotSendMessage(AppTranslator.CatalogPageLoaded(CurrentLanguageInt))
                Dim LTDPage As New Geode.Habbo.Packages.HCatalogPage(LTDPageData.Packet)
                If (CreditLimit < LTDPage.Offers(0).CreditCost) Or (DiamondLimit < LTDPage.Offers(0).OtherCurrencyCost) Then
                    ConsoleBot.BotSendMessage(AppTranslator.CurrencyLimitError(CurrentLanguageInt))
                    Throw New Exception("Maximum currency value exceeded!")
                End If
                Await Task.Delay(PurchaseStepsDelayMs)
                ConsoleBot.BotSendMessage(AppTranslator.TryingToBuy(CurrentLanguageInt))
                Extension.SendToServerAsync(Extension.Out.PurchaseFromCatalog, LTDPage.Id, LTDPage.Offers(0).Id, "", 1)
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

    Private Function ResetVariables()
        TaskStarted = False
        TaskBlocked = False
        TestMode = False
        TaskCanBeStopped = True
        PurchaseStepsDelayMs = -666
        CreditLimit = 0
        DiamondLimit = 0
    End Function

    Private Sub ConsoleBot_OnBotLoaded(e As String) Handles ConsoleBot.OnBotLoaded
        ResetVariables() 'Reset variables when ConsoleBot loaded
        BotWelcome() 'Show welcome message when ConsoleBot loaded
    End Sub

    Private Sub ConsoleBot_OnMessageReceived(e As String) Handles ConsoleBot.OnMessageReceived
        If PurchaseStepsDelayMs < 0 Then
            Dim PurchaseStepsDelayInput As Integer = PurchaseStepsDelayMs
            If Integer.TryParse(e, PurchaseStepsDelayInput) And PurchaseStepsDelayInput >= 0 Then
                PurchaseStepsDelayMs = PurchaseStepsDelayInput
                ConsoleBot.BotSendMessage(AppTranslator.CreditSet(CurrentLanguageInt))
                Return
            Else
                ConsoleBot.BotSendMessage(AppTranslator.PurchaseStepsDelaySet(CurrentLanguageInt))
                Return
            End If
        End If
        If CreditLimit = 0 Then
            Dim TempCreditInput As Integer = 0
            If Integer.TryParse(e, TempCreditInput) And TempCreditInput > 0 Then
                CreditLimit = TempCreditInput
                ConsoleBot.BotSendMessage(AppTranslator.DiamondSet(CurrentLanguageInt))
                Return
            Else
                ConsoleBot.BotSendMessage(AppTranslator.CreditSet(CurrentLanguageInt))
                Return
            End If
        End If
        If DiamondLimit = 0 Then
            Dim TempDiamondInput As Integer = 0
            If Integer.TryParse(e, TempDiamondInput) And TempDiamondInput > 0 Then
                DiamondLimit = TempDiamondInput
            Else
                ConsoleBot.BotSendMessage(AppTranslator.DiamondSet(CurrentLanguageInt))
                Return
            End If
        End If
        If TaskBlocked = False Then
            Select Case e.ToLower 'Handle received message
                Case "/test", "/probar", "/testar"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = True
                        TaskStarted = True
                        TryToBuyLTD()
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/force", "/forzar", "/forçar"
                    If TaskBlocked = False And TaskCanBeStopped = True Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = False
                        TaskStarted = True
                        TryToBuyLTD()
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/start", "/iniciar", "/começar"
                    If TaskStarted = False Then
                        ConsoleBot.BotSendMessage(AppTranslator.StartedMessage(CurrentLanguageInt))
                        TestMode = False
                        TaskStarted = True
                    Else
                        ConsoleBot.BotSendMessage(AppTranslator.ReducedCommandsList(CurrentLanguageInt))
                    End If
                Case "/stop", "/detener", "/parar"
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
                Case "/salir", "/sair"
                    ConsoleBot.CustomExitCommand = e
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
            If TaskStarted = True And TaskCanBeStopped = False Then
                e.IsBlocked = True
            End If
        End If
        If Extension.Out.GetCatalogIndex.Match(e) Then 'Ignore unhandled catalog index loading requests
            If TaskStarted = True And TaskCanBeStopped = True Then
                e.IsBlocked = True
            End If
        End If
        If Extension.In.CatalogPublished.Match(e) Then 'Catalog update request received from server
            If TaskStarted = True And TaskCanBeStopped = True Then
                ConsoleBot.BotSendMessage(AppTranslator.CatalogUpdateReceived(CurrentLanguageInt))
                TestMode = False
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

Public Class AppTranslator
    '0=English 1=Spanish 2=Portuguese-BR
    Public Shared WelcomeMessage As String() = {
        "Welcome |",
        "Bienvenidx |",
        "Bem-vindo |"
    }
    Public Shared FullCommandsList As String() = {
        "Available commands: /start /stop /force /test /exit",
        "Comandos disponibles: /iniciar /detener /forzar /probar /salir",
        "Comando disponíveis: /começar /parar /forçar /testar /sair"
    }
    Public Shared ReducedCommandsList As String() = {
        "Available commands: /stop /exit",
        "Comandos disponibles: /detener /salir",
        "Comandos disponíveis: /parar /sair"
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
        "[Error]" & vbCr & "We will try again if the catalog is updated.",
        "[Error]" & vbCr & "Volveremos a intentarlo si el catalogo se actualiza.",
        "[Erro]" & vbCr & "Tentaremos novamente se o catálogo for atualizado."
    }
    Public Shared ExitAdvice As String() = {
        "Use /exit to finish.",
        "Usa /exit para finalizar.",
        "Use /exit para finalizar."
    }
    Public Shared StartedMessage As String() = {
        "I will try to buy the LTD, you can use /stop to finish.",
        "Intentare comprar un LTD, puedes usar /detener para finalizar.",
        "Vou tentar comprar o LTD, você pode usar /parar para finalizar."
    }
    Public Shared StoppedMessage As String() = {
        "Stopped, you can use /start to try again.",
        "Detenida, puedes usar /iniciar para reintentar.",
        "Parado, você pode usar /começar para tentar novamente."
    }
    Public Shared CatalogIndexLoaded As String() = {
        "[Catalog index loaded]",
        "[Indice del catalogo cargado]",
        "[Índice do catálogo carregado]"
    }
    Public Shared CatalogPageLoaded As String() = {
        "[Catalog page loaded]",
        "[Pagina del catalogo cargada]",
        "[Página do catálogo carregada]"
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
    Public Shared PurchaseStepsDelaySet As String() = {
        "Enter the milliseconds of delay in the purchase steps (0=no delay)",
        "Ingresa los milisegundos de demora en los pasos de compra (0=sin demora)",
        "Insira os milissegundos de atraso nas etapas de compra (0=sem atraso)"
}
    Public Shared CreditSet As String() = {
        "Enter the maximum amount of credits you can spend.",
        "Ingresa la cantidad maxima de creditos que puedes gastar.",
        "Insira a quantidade máxima de créditos que você pode gastar."
}
    Public Shared DiamondSet As String() = {
        "Enter the maximum amount of diamonds you can spend.",
        "Ingresa la cantidad maxima de diamantes que puedes gastar.",
        "Insira a quantidade máxima de diamantes que você pode gastar."
}
    Public Shared CurrencyLimitError As String() = {
        "The LTD worth more than what you allowed to spend!",
        "El LTD vale mas caro de lo que permitiste gastar!",
        "O LTD vale mais do que você permitiu gastar!"
}
    Public Shared LtdCategoryVisibilityError As String() = {
        "The LTD category is not yet visible in the catalog!",
        "La categoria LTD aun no esta visible en el catalogo!",
        "A categoria LTD ainda não está visível no catálogo!"
}
End Class