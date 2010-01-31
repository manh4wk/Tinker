﻿Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Tinker
Imports Tinker.WC3
Imports Tinker.WC3.Protocol
Imports Tinker.Bnet.Protocol

Friend Module SharedTestObjects
    Friend ReadOnly TestMap As New Map(
        folder:="Test:\Maps",
        relativePath:="test",
        fileChecksumCRC32:=1,
        filesize:=1,
        mapChecksumSHA1:=(From i In Enumerable.Range(0, 20) Select CByte(i)).ToArray.AsReadableList,
        mapChecksumXORO:=1,
        slotCount:=2)
    Friend ReadOnly TestArgument As New Tinker.Commands.CommandArgument("")
    Friend ReadOnly TestStats As New GameStats(
            Map:=TestMap,
            hostName:="StrilancHost",
            argument:=TestArgument)
    Friend ReadOnly TestDesc As New RemoteGameDescription(
            name:="test",
            GameStats:=TestStats,
            location:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
            gameid:=42,
            entrykey:=0,
            totalslotcount:=12,
            gameType:=GameTypes.AuthenticatedMakerBlizzard,
            state:=GameStates.Private,
            usedSlotCount:=0,
            baseage:=5.Seconds,
            clock:=New ManualClock())
    Friend ReadOnly TestSettings As New GameSettings(
            Map:=TestMap,
            GameDescription:=TestDesc,
            argument:=TestArgument)
    Friend ReadOnly TestPlayer As New Player(
        index:=New PID(1),
        settings:=TestSettings,
        name:="test")
End Module
