﻿Namespace WC3
    <DebuggerDisplay("{ToString()}")>
    Public Structure Slot
        Implements IEquatable(Of Slot)

        Public Const ObserverTeamIndex As Byte = 12
        Public Enum LockState
            Unlocked = 0
            Sticky = 1
            Frozen = 2
        End Enum

        Private ReadOnly _index As Byte
        Private ReadOnly _color As Protocol.PlayerColor
        Private ReadOnly _team As Byte
        Private ReadOnly _handicap As Byte
        Private ReadOnly _race As Protocol.Races
        Private ReadOnly _raceUnlocked As Boolean
        Private ReadOnly _locked As LockState
        Private ReadOnly _contents As SlotContents

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_index < 12)
            Contract.Invariant(_team <= 12)
        End Sub

        '''<summary>Trivial constructor.</summary>
        Public Sub New(index As Byte,
                       contents As SlotContents,
                       color As Protocol.PlayerColor,
                       raceUnlocked As Boolean,
                       team As Byte,
                       Optional race As Protocol.Races = Protocol.Races.Random,
                       Optional handicap As Byte = 100,
                       Optional locked As LockState = LockState.Unlocked)
            Contract.Requires(index < 12)
            Contract.Requires(team <= 12)
            Contract.Requires(contents IsNot Nothing)
            Me._index = index
            Me._color = color
            Me._team = team
            Me._handicap = handicap
            Me._race = race
            Me._locked = locked
            Me._raceUnlocked = raceUnlocked
            Me._contents = contents
        End Sub

        Public ReadOnly Property Index As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() < 12)
                Return _index
            End Get
        End Property
        Public ReadOnly Property Color As Protocol.PlayerColor
            Get
                Return _color
            End Get
        End Property
        Public ReadOnly Property Team As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _team
            End Get
        End Property
        Public ReadOnly Property Handicap As Byte
            Get
                Return _handicap
            End Get
        End Property
        Public ReadOnly Property Race As Protocol.Races
            Get
                Return _race
            End Get
        End Property
        Public ReadOnly Property RaceUnlocked As Boolean
            Get
                Return _raceUnlocked
            End Get
        End Property
        Public ReadOnly Property Locked As LockState
            Get
                Return _locked
            End Get
        End Property
        Public ReadOnly Property Contents As SlotContents
            Get
                Contract.Ensures(Contract.Result(Of SlotContents)() IsNot Nothing)
                Return If(_contents, New SlotContentsOpen())
            End Get
        End Property

        '''<summary>Returns a slot based on the current slot, with the specified properties changed.</summary>
        '''<remarks>Verification disabled because the verifier doesn't seem to understand null coalescing.</remarks>
        <Pure()>
        <ContractVerification(False)>
        Public Function [With](Optional index As Byte? = Nothing,
                               Optional contents As SlotContents = Nothing,
                               Optional color As Protocol.PlayerColor? = Nothing,
                               Optional raceUnlocked As Boolean? = Nothing,
                               Optional team As Byte? = Nothing,
                               Optional race As Protocol.Races? = Nothing,
                               Optional handicap As Byte? = Nothing,
                               Optional locked As LockState? = Nothing) As Slot
            '[assumed instead of required due to stupid verifier]
            Contract.Assume(If(index, Me.Index) < 12)
            Contract.Assume(If(team, Me.Team) <= 12)
            Return New Slot(index:=If(index, Me.Index),
                            color:=If(color, Me.Color),
                            team:=If(team, Me.Team),
                            handicap:=If(handicap, Me.Handicap),
                            race:=If(race, Me.Race),
                            locked:=If(locked, Me.Locked),
                            raceUnlocked:=If(raceUnlocked, Me.RaceUnlocked),
                            contents:=If(contents, Me.Contents))
        End Function

        Public Enum Match
            None = 0
            Team = 1
            Contents = 2
            Color = 3
            Index = 4
        End Enum
        <Pure()>
        Public Function Matches(query As InvariantString) As Match
            '[checked in decreasing order of importance]
            If query = (Index + 1).ToString(CultureInfo.InvariantCulture) Then Return Match.Index
            If query = Color.ToString Then Return Match.Color
            If Contents.Matches(query) Then Return Match.Contents
            If query = "team{0}".Frmt(Me.Team + 1) Then Return Match.Team
            If Me.Team = ObserverTeamIndex AndAlso (query = "obs" OrElse query = "observer") Then Return Match.Team
            Return Match.None
        End Function
        Public ReadOnly Property MatchableId() As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return (Index + 1).ToString(CultureInfo.InvariantCulture)
            End Get
        End Property
        <Pure()>
        Public Async Function AsyncGenerateDescription() As Task(Of String)
            'Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Dim result = ""
            If Locked <> LockState.Unlocked Then result += "({0}) ".Frmt(Locked)
            result += If(Team = ObserverTeamIndex, "Observer", "Team {0}, {1}, {2}".Frmt(Team + 1, Race, Color))
            result = result.Padded(30)
            Dim desc = Await Contents.AsyncGenerateDescription
            Return result + desc
        End Function

        Public Overloads Function Equals(other As Slot) As Boolean Implements IEquatable(Of Slot).Equals
            Return Me._index = other._index AndAlso
                   Me._color = other._color AndAlso
                   Me._team = other._team AndAlso
                   Me._handicap = other._handicap AndAlso
                   Me._race = other._race AndAlso
                   Me._raceUnlocked = other._raceUnlocked AndAlso
                   Me._locked = other._locked AndAlso
                   Me._contents Is other._contents
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is Slot AndAlso Me.Equals(DirectCast(obj, Slot))
        End Function
        Public Shared Operator =(slot1 As Slot, slot2 As Slot) As Boolean
            Return slot1.Equals(slot2)
        End Operator
        Public Shared Operator <>(slot1 As Slot, slot2 As Slot) As Boolean
            Return Not slot1.Equals(slot2)
        End Operator
        Public Overrides Function GetHashCode() As Integer
            Return _index.GetHashCode
        End Function
        Public Overrides Function ToString() As String
            Return "Index:{0}, Team:{1}, ContentType:{2}".Frmt(Index, Team, Contents.GetType)
        End Function
    End Structure
End Namespace
