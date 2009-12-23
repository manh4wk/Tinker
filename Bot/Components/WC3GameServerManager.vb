﻿Namespace Components
    Public Class WC3GameServerManager
        Inherits FutureDisposable
        Implements IBotComponent

        Private Shared ReadOnly ServerCommands As New Commands.Specializations.ServerCommands()
        Public Shared ReadOnly TypeName As String = "Server"

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
        Private ReadOnly _name As InvariantString
        Private ReadOnly _gameServer As WC3.GameServer
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
        Private ReadOnly _control As Control
        Private ReadOnly _bot As MainBot
        Private _listener As Net.Sockets.TcpListener
        Private _portHandle As PortPool.PortHandle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_gameServer IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_listener IsNot Nothing)
            Contract.Invariant(_portHandle IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal gameServer As WC3.GameServer,
                       ByVal bot As MainBot)
            Contract.Requires(gameServer IsNot Nothing)
            Contract.Requires(bot IsNot Nothing)

            Me._portHandle = bot.PortPool.TryAcquireAnyPort()
            If Me._portHandle Is Nothing Then Throw New InvalidOperationException("There are no ports for the server available in the pool.")
            Me._name = name
            Me._gameServer = gameServer

            Dim control = New W3ServerControl(Me)
            Me._control = control
            Me._bot = bot
            Me._listener = New Net.Sockets.TcpListener(New Net.IPEndPoint(Net.IPAddress.Any, Me._portHandle.Port))
            Me._listener.Start()

            AddHandler _gameServer.PlayerTalked, AddressOf OnPlayerTalked
            _hooks.Add(New DelegatedDisposable(Sub() RemoveHandler _gameServer.PlayerTalked, AddressOf OnPlayerTalked).Futurized)

            BeginAccepting()
        End Sub

        Public ReadOnly Property Server As WC3.GameServer
            Get
                Contract.Ensures(Contract.Result(Of WC3.GameServer)() IsNot Nothing)
                Return _gameServer
            End Get
        End Property

        Private Sub BeginAccepting()
            Dim listener = Me._listener
            AsyncProduceConsumeUntilError(
                producer:=Function() listener.AsyncAcceptConnection(),
                consumer:=Function(tcpClient)
                              Dim socket = New WC3.W3Socket(New PacketSocket(
                                                                stream:=tcpClient.GetStream,
                                                                localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                                remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                                timeout:=60.Seconds,
                                                                Logger:=Logger))
                              Return _gameServer.QueueAcceptSocket(socket)
                          End Function,
                errorHandler:=Sub(exception)
                                  If listener IsNot Me._listener Then Return 'not an error; listener was just closed or changed
                                  exception.RaiseAsUnexpected("Accepting connections for game server.")
                                  Logger.Log("Error accepting connections: {0}".Frmt(exception.Message), LogMessageType.Problem)
                              End Sub)
        End Sub

        Private Sub ChangeListenPort(ByVal portHandle As PortPool.PortHandle)
            Contract.Requires(portHandle IsNot Nothing)
            If portHandle.Port = Me._portHandle.Port Then Return

            'Try new port before disposing the old one
            Dim oldListener = Me._listener
            Dim newListener = New Net.Sockets.TcpListener(New Net.IPEndPoint(Net.IPAddress.Any, portHandle.Port))
            newListener.Start()

            'Switch handles
            Me._portHandle.Dispose()
            Me._portHandle = portHandle
            Me._listener = newListener

            'Continue accepting
            BeginAccepting()
            oldListener.Stop()
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return TypeName
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _gameServer.Logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public ReadOnly Property Bot As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return ServerCommands.IsArgumentPrivate(argument)
        End Function
        Public ReadOnly Property Control As System.Windows.Forms.Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IBotComponent.InvokeCommand
            Return ServerCommands.Invoke(_gameServer, user, argument)
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _gameServer.Dispose()
            _control.Dispose()
            Return Nothing
        End Function

        Private Sub OnPlayerTalked(ByVal sender As WC3.GameServer,
                                   ByVal game As WC3.Game,
                                   ByVal player As WC3.Player,
                                   ByVal text As String)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(text IsNot Nothing)

            Dim prefix = My.Settings.commandPrefix
            Contract.Assume(prefix IsNot Nothing)
            If Not text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) AndAlso text <> MainBot.TriggerCommandText Then
                Return
            End If

            '?trigger command
            If text = MainBot.TriggerCommandText Then
                game.QueueSendMessageTo("Command prefix is '{0}'".Frmt(prefix), player)
                Return
            End If

            'Normal commands
            Dim commandText = text.Substring(My.Settings.commandPrefix.Length)
            Throw New NotImplementedException() 'nothing arg follows
            game.QueueCommandProcessText(Nothing, player, commandText).CallWhenValueReady(
                Sub(message, messageException)
                    Contract.Assume(player IsNot Nothing)
                    If messageException IsNot Nothing Then
                        game.QueueSendMessageTo("Failed: {0}".Frmt(messageException.Message), player)
                    ElseIf message IsNot Nothing Then
                        game.QueueSendMessageTo(message, player)
                    Else
                        game.QueueSendMessageTo("Command Succeeded", player)
                    End If
                End Sub
            )
        End Sub

        Private allocId As UInteger
        Private ReadOnly r As New Random()
        Private Function AllocateGameId() As UInt32
            Contract.Ensures(Contract.Result(Of UInt32)() > 0)
            allocId += 1UI
            If allocId > 1000 Then allocId = 1
            Dim result = allocId * 10000UI + CUInt(r.Next(1000))
            Contract.Assume(result > 0)
            Return result
        End Function
        Private Function AsyncAddGameFromArguments(ByVal argument As Commands.CommandArgument,
                                                   ByVal user As BotUser) As IFuture(Of WC3.GameSet)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of WC3.GameSet))() IsNot Nothing)

            Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))

            Dim hostName = If(user Is Nothing, Application.ProductName, user.Name.Value)
            Contract.Assume(hostName IsNot Nothing)
            Dim gameStats = New WC3.GameStats(map, hostname, argument)

            Dim totalSlotCount = map.NumPlayerSlots
            Select Case gameStats.observers
                Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                    totalSlotCount = 12
            End Select

            Dim gameDescription = New WC3.LocalGameDescription(
                                            Name:=argument.NamedValue("name"),
                                            gameStats:=gameStats,
                                            hostport:=_portHandle.Port,
                                            gameId:=AllocateGameId(),
                                            EntryKey:=0,
                                            totalSlotCount:=totalSlotCount,
                                            GameType:=map.GameType,
                                            state:=0,
                                            UsedSlotCount:=0)

            Dim gameSettings = New WC3.GameSettings(map, gameDescription, argument)

            Return _gameServer.QueueAddGameSet(gameSettings)
        End Function
        Public Function QueueAddGameFromArguments(ByVal argument As Commands.CommandArgument,
                                                  ByVal user As BotUser) As IFuture(Of WC3.GameSet)
            Contract.Requires(argument IsNot Nothing)
            Contract.Requires(user IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of WC3.GameSet))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncAddGameFromArguments(argument, user)).Defuturized
        End Function
        Public Function QueueChangeListenPort(ByVal portHandle As PortPool.PortHandle) As IFuture
            Contract.Requires(portHandle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ChangeListenPort(portHandle))
        End Function
        Public Function QueueGetListenPort() As IFuture(Of UShort)
            Contract.Ensures(Contract.Result(Of IFuture(Of UShort))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _portHandle.Port)
        End Function
    End Class
End Namespace