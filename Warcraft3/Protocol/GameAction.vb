﻿Imports Tinker.Pickling

Namespace WC3.Protocol
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class GameAction
        Private Shared ReadOnly ActionJar As PrefixSwitchJar(Of GameActionId) = MakeJar()
        Private ReadOnly _id As GameActionId
        Private ReadOnly _payload As IPickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As GameActionId, ByVal payload As IPickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub
        Private Sub New(ByVal payload As IPickle(Of PrefixPickle(Of GameActionId)))
            Me.New(payload.Value.Key, payload.Value.Payload)
            Contract.Requires(payload IsNot Nothing)
        End Sub

        Public Shared Function FromValue(Of T)(ByVal actionDefinition As GameActions.Definition(Of T),
                                               ByVal value As T) As GameAction
            Contract.Requires(actionDefinition IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(actionDefinition.Id, actionDefinition.Jar.Pack(value))
        End Function

        Public ReadOnly Property Id As GameActionId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As IPickle
            Get
                Contract.Ensures(Contract.Result(Of IPickle)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Private Shared Sub reg(Of T)(ByVal jar As PrefixSwitchJar(Of GameActionId),
                                     ByVal definition As GameActions.Definition(Of T))
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(definition IsNot Nothing)
            jar.AddPackerParser(definition.Id, definition.Jar.Weaken)
        End Sub

        Private Shared Function MakeJar() As PrefixSwitchJar(Of GameActionId)
            Contract.Ensures(Contract.Result(Of PrefixSwitchJar(Of GameActionId))() IsNot Nothing)
            Dim jar = New PrefixSwitchJar(Of GameActionId)("W3GameAction")
            reg(jar, GameActions.PauseGame)
            reg(jar, GameActions.ResumeGame)
            reg(jar, GameActions.SetGameSpeed)
            reg(jar, GameActions.IncreaseGameSpeed)
            reg(jar, GameActions.DecreaseGameSpeed)
            reg(jar, GameActions.SaveGameStarted)
            reg(jar, GameActions.SaveGameFinished)
            reg(jar, GameActions.SelfOrder)
            reg(jar, GameActions.PointOrder)
            reg(jar, GameActions.ObjectOrder)
            reg(jar, GameActions.DropOrGiveItem)
            reg(jar, GameActions.FogObjectOrder)
            reg(jar, GameActions.EnterChooseHeroSkillSubmenu)
            reg(jar, GameActions.EnterChooseBuildingSubmenu)
            reg(jar, GameActions.PressedEscape)
            reg(jar, GameActions.CancelHeroRevive)
            reg(jar, GameActions.DequeueBuildingOrder)
            reg(jar, GameActions.MinimapPing)
            reg(jar, GameActions.ChangeAllyOptions)
            reg(jar, GameActions.TransferResources)
            reg(jar, GameActions.ChangeSelection)
            reg(jar, GameActions.AssignGroupHotkey)
            reg(jar, GameActions.SelectGroupHotkey)
            reg(jar, GameActions.SelectSubGroup)
            reg(jar, GameActions.PreSubGroupSelection)
            reg(jar, GameActions.SelectGroundItem)
            reg(jar, GameActions.CheatDisableTechRequirements)
            reg(jar, GameActions.CheatDisableVictoryConditions)
            reg(jar, GameActions.CheatEnableResearch)
            reg(jar, GameActions.CheatFastCooldown)
            reg(jar, GameActions.CheatFastDeathDecay)
            reg(jar, GameActions.CheatGodMode)
            reg(jar, GameActions.CheatInstantDefeat)
            reg(jar, GameActions.CheatInstantVictory)
            reg(jar, GameActions.CheatNoDefeat)
            reg(jar, GameActions.CheatNoFoodLimit)
            reg(jar, GameActions.CheatRemoveFogOfWar)
            reg(jar, GameActions.CheatResearchUpgrades)
            reg(jar, GameActions.CheatSpeedConstruction)
            reg(jar, GameActions.CheatUnlimitedMana)
            reg(jar, GameActions.CheatSetTimeOfDay)
            reg(jar, GameActions.CheatGold)
            reg(jar, GameActions.CheatGoldAndLumber)
            reg(jar, GameActions.CheatLumber)
            reg(jar, GameActions.TriggerChatEvent)
            reg(jar, GameActions.TriggerWaitFinished)
            reg(jar, GameActions.TriggerMouseTouchedTrackable)
            reg(jar, GameActions.TriggerMouseClickedTrackable)
            reg(jar, GameActions.DialogAnyButtonClicked)
            reg(jar, GameActions.DialogButtonClicked)
            reg(jar, GameActions.TriggerArrowKeyEvent)
            reg(jar, GameActions.TriggerSelectionEvent)
            reg(jar, GameActions.GameCacheSyncInteger)
            reg(jar, GameActions.GameCacheSyncBoolean)
            reg(jar, GameActions.GameCacheSyncReal)
            reg(jar, GameActions.GameCacheSyncUnit)
            reg(jar, GameActions.GameCacheSyncString)
            reg(jar, GameActions.GameCacheSyncEmptyInteger)
            reg(jar, GameActions.GameCacheSyncEmptyBoolean)
            reg(jar, GameActions.GameCacheSyncEmptyReal)
            reg(jar, GameActions.GameCacheSyncEmptyUnit)
            reg(jar, GameActions.GameCacheSyncEmptyString)
            Return jar
        End Function

        Public Shared Function FromData(ByVal data As IReadableList(Of Byte)) As GameAction
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(ActionJar.Parse(data))
        End Function

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(id, Payload.Description.Value())
        End Function
    End Class

    Public NotInheritable Class GameActionJar
        Inherits BaseJar(Of GameAction)
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As GameAction)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of TValue)(Name, value, Concat({value.id}, value.Payload.Data.ToArray).AsReadableList)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameAction)
            Dim val = GameAction.FromData(data)
            Dim datum = data.SubView(0, val.Payload.Data.Count + 1) 'include the id
            Return New Pickle(Of GameAction)(Name, val, datum)
        End Function
    End Class

    Public Class PlayerActionSet
        Implements IEquatable(Of PlayerActionSet)

        Private ReadOnly _id As PlayerId
        Private ReadOnly _actions As IReadableList(Of GameAction)
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_actions IsNot Nothing)
        End Sub
        Public Sub New(ByVal id As PlayerId, ByVal actions As IReadableList(Of GameAction))
            Contract.Requires(actions IsNot Nothing)
            Me._id = id
            Me._actions = actions
        End Sub
        Public ReadOnly Property Id As PlayerId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Actions As IReadableList(Of GameAction)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of GameAction))() IsNot Nothing)
                Return _actions
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Id.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Dim other = TryCast(obj, PlayerActionSet)
            If other Is Nothing Then Return False
            Return Me.Equals(other)
        End Function
        Public Overloads Function Equals(ByVal other As PlayerActionSet) As Boolean Implements System.IEquatable(Of PlayerActionSet).Equals
            If other Is Nothing Then Return False
            If Me.Id <> other.Id Then Return False
            If Me.Actions.Count <> other.Actions.Count Then Return False
            If (From i In Enumerable.Range(0, Me.Actions.Count)
                Where Not Me.Actions(i).Payload.Data.SequenceEqual(other.Actions(i).Payload.Data)).Any Then Return False
            Return True
        End Function
    End Class
    Public Class PlayerActionSetJar
        Inherits BaseJar(Of PlayerActionSet)

        Private ReadOnly _dataJar As IJar(Of Dictionary(Of InvariantString, Object))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_dataJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
            _dataJar = New TupleJar("player action set",
                    New PlayerIdJar("source").Weaken,
                    New GameActionJar("action").Repeated(name:="actions").DataSizePrefixed(prefixSize:=2).Weaken)
        End Sub

        Public Overrides Function Pack(Of TValue As PlayerActionSet)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = _dataJar.Pack(New Dictionary(Of InvariantString, Object) From {
                                            {"source", value.Id},
                                            {"actions", value.Actions}
                                       })
            Return New Pickle(Of TValue)(value, pickle.Data, pickle.Description)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of PlayerActionSet)
            Dim pickle = _dataJar.Parse(data)
            Dim id = CType(pickle.Value("source"), PlayerId)
            Dim actions = CType(pickle.Value("actions"), IReadableList(Of GameAction)).AssumeNotNull
            If id.Index < 1 OrElse id.Index > 12 Then Throw New IO.InvalidDataException("Invalid pid.")
            Dim value = New PlayerActionSet(id, actions)
            Return New Pickle(Of PlayerActionSet)(value, pickle.Data, pickle.Description)
        End Function
    End Class
End Namespace
