Namespace Warcraft3
    Public NotInheritable Class W3Map
        Public ReadOnly playableWidth As Integer
        Public ReadOnly playableHeight As Integer
        Public ReadOnly isMelee As Boolean
        Public ReadOnly name As String
        Private ReadOnly _numPlayerSlots As Integer
        Private ReadOnly _fileSize As UInteger
        Private ReadOnly _fileChecksumCRC32 As UInt32
        Private ReadOnly _mapChecksumSHA1 As ViewableList(Of Byte)
        Private ReadOnly _mapChecksumXORO As UInt32
        Private ReadOnly _folder As String
        Private ReadOnly _relativePath As String
        Private ReadOnly _fullPath As String
        Public ReadOnly fileAvailable As Boolean
        Private ReadOnly _slots As New List(Of W3Slot)
        Public ReadOnly Property Slots As IList(Of W3Slot)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of W3Slot))() IsNot Nothing)
                Return _slots
            End Get
        End Property
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_fileSize > 0)
            Contract.Invariant(_mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(_mapChecksumSHA1.Length = 20)
            Contract.Invariant(_slots IsNot Nothing)
            Contract.Invariant(_folder IsNot Nothing)
            Contract.Invariant(_relativePath IsNot Nothing)
            Contract.Invariant(_fullPath IsNot Nothing)
            Contract.Invariant(_numPlayerSlots > 0)
            Contract.Invariant(_numPlayerSlots <= 12)
        End Sub
        Public ReadOnly Property NumPlayerSlots As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPlayerSlots
            End Get
        End Property
        Public ReadOnly Property FileSize As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _fileSize
            End Get
        End Property
        Public ReadOnly Property MapChecksumSHA1 As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))().Length = 20)
                Return _mapChecksumSHA1
            End Get
        End Property
        Public ReadOnly Property FileChecksumCRC32 As UInt32
            Get
                Return _fileChecksumCRC32
            End Get
        End Property
        Public ReadOnly Property MapChecksumXORO As UInt32
            Get
                Return _mapChecksumXORO
            End Get
        End Property
        Public ReadOnly Property Folder As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _folder
            End Get
        End Property
        Public ReadOnly Property RelativePath As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _relativePath
            End Get
        End Property
        Public ReadOnly Property FullPath As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _fullPath
            End Get
        End Property

        Public Enum SizeClass
            Huge
            Large
            Medium
            Small
            Tiny
        End Enum
        Public ReadOnly Property SizeClassification As SizeClass
            Get
                '[I don't know if area works for irregular sizes; might be max instead]
                Select Case playableWidth * playableHeight
                    Case Is <= 64 * 64 : Return SizeClass.Tiny
                    Case Is <= 128 * 128 : Return SizeClass.Small
                    Case Is <= 160 * 160 : Return SizeClass.Medium
                    Case Is <= 192 * 192 : Return SizeClass.Large
                    Case Else : Return SizeClass.Huge
                End Select
            End Get
        End Property
        Public ReadOnly Property GameType As GameTypes
            Get
                Dim f = GameTypes.MakerUser
                Select Case SizeClassification
                    Case SizeClass.Tiny, SizeClass.Small
                        f = f Or GameTypes.SizeSmall
                    Case SizeClass.Medium
                        f = f Or GameTypes.SizeMedium
                    Case SizeClass.Large, SizeClass.Huge
                        f = f Or GameTypes.SizeLarge
                End Select
                If isMelee Then
                    f = f Or GameTypes.TypeMelee
                Else
                    f = f Or GameTypes.TypeScenario
                End If
                Return f
            End Get
        End Property

#Region "New"
        Public Shared Function FromArgument(ByVal arg As String) As W3Map
            Contract.Requires(arg IsNot Nothing)
            Contract.Requires(arg.Length > 0)
            Contract.Ensures(Contract.Result(Of W3Map)() IsNot Nothing)
            If arg(0) = "-"c Then
                Throw New ArgumentException("Map argument begins with '-', is probably an option. (did you forget an argument?)")
            ElseIf arg.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) Then 'Map specified by HostMapInfo packet data
                'Parse
                If arg Like "0x*[!0-9a-fA-F]" OrElse arg.Length Mod 2 <> 0 Then
                    Throw New ArgumentException("Invalid map meta data. [0x prefix should be followed by hex HostMapInfo packet data].")
                End If
                Dim hexData = (From i In Enumerable.Range(1, arg.Length \ 2 - 1)
                               Select CByte(arg.Substring(i * 2, 2).FromHexToUInt64(ByteOrder.BigEndian))
                               ).ToArray
                Dim packet = W3Packet.FromData(W3PacketId.HostMapInfo, hexData.ToView)
                Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))

                'Extract values
                Dim path = CStr(vals("path")).AssumeNotNull
                Dim size = CUInt(vals("size"))
                Dim crc32 = CUInt(vals("crc32"))
                Dim xoro = CUInt(vals("xoro checksum"))
                Dim sha1 = CType(vals("sha1 checksum"), Byte()).AssumeNotNull
                If Not path.StartsWith("Maps\", StringComparison.InvariantCultureIgnoreCase) Then
                    Throw New IO.InvalidDataException("Invalid map path.")
                End If
                Contract.Assume(sha1.Length = 20)
                Contract.Assume(size > 0)

                Return New W3Map(My.Settings.mapPath.AssumeNotNull, path, size, crc32, sha1, xoro, slotCount:=3)
            Else 'Map specified by path
                Return New W3Map(My.Settings.mapPath.AssumeNotNull,
                                 FindFileMatching("*{0}*".Frmt(arg), "*.[wW]3[mxMX]", My.Settings.mapPath.AssumeNotNull),
                                 My.Settings.war3path.AssumeNotNull)
            End If
        End Function
        Public Sub New(ByVal folder As String,
                       ByVal relativePath As String,
                       ByVal fileSize As UInteger,
                       ByVal fileChecksumCRC32 As UInt32,
                       ByVal mapChecksumSHA1 As Byte(),
                       ByVal mapChecksumXORO As UInt32,
                       ByVal slotCount As Integer)
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(relativePath IsNot Nothing)
            Contract.Requires(relativePath.StartsWith("Maps\", StringComparison.InvariantCultureIgnoreCase))
            Contract.Requires(mapChecksumSHA1 IsNot Nothing)
            Contract.Requires(mapChecksumSHA1.Length = 20)
            Contract.Requires(slotCount > 0)
            Contract.Requires(slotCount <= 12)
            Contract.Requires(fileSize > 0)
            Contract.Ensures(Me.Slots.Count = slotCount)

            Me._fullPath = folder + relativePath.Substring(5)
            Me._relativePath = relativePath
            Me._folder = folder
            Me.playableHeight = 256
            Me.playableWidth = 256
            Me.isMelee = True
            Me.name = relativePath.Split("\"c).Last
            Me._numPlayerSlots = slotCount
            Me._fileSize = fileSize
            Me._fileChecksumCRC32 = fileChecksumCRC32
            Me._mapChecksumSHA1 = mapChecksumSHA1.ToView
            Me._mapChecksumXORO = mapChecksumXORO
            For slotId = 1 To slotCount
                Dim slot = New W3Slot(Nothing, CByte(slotId))
                slot.color = CType(slotId - 1, W3Slot.PlayerColor)
                slot.Contents = New W3SlotContentsOpen(slot)
                Slots.Add(slot)
                Contract.Assume(Slots.Count = slotId)
            Next slotId
            Contract.Assume(Slots.Count = slotCount)
        End Sub
        Public Sub New(ByVal folder As String,
                       ByVal relativePath As String,
                       ByVal wc3PatchMPQFolder As String)
            Me.fileAvailable = True
            Me._relativePath = relativePath
            Me._fullPath = folder + relativePath
            Me._folder = folder
            Using f = New IO.FileStream(FullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                Me._fileSize = CUInt(f.Length)
                Me._fileChecksumCRC32 = f.ToEnumerator.CRC32
            End Using
            Dim mapArchive = New MPQ.Archive(FullPath)
            Dim war3PatchArchive = OpenWar3PatchArchive(wc3PatchMPQFolder)
            Me._mapChecksumSHA1 = ComputeMapSha1Checksum(mapArchive, war3PatchArchive).ToView
            Me._mapChecksumXORO = CUInt(ComputeMapXoro(mapArchive, war3PatchArchive))

            Dim info = ReadMapInfo(mapArchive)
            Me._slots = info.slots
            Me.isMelee = info.isMelee
            Me._numPlayerSlots = info.slots.Count
            Me.name = info.name
            Me.playableHeight = info.playableHeight
            Me.playableWidth = info.playableWidth
        End Sub
#End Region

#Region "Read"
        Public Function ReadChunk(ByVal pos As Integer,
                                  Optional ByVal maxLength As Integer = 1442) As Byte()
            Contract.Requires(pos >= 0)
            Contract.Requires(maxLength >= 0)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            If pos > Me.FileSize Then Throw New InvalidOperationException("Attempted to read past end of map file.")
            If Not fileAvailable Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")

            Dim buffer(0 To maxLength - 1) As Byte
            Using f = New IO.FileStream(FullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                f.Seek(pos, IO.SeekOrigin.Begin)
                Dim n = f.Read(buffer, 0, maxLength)
                If n < buffer.Length Then ReDim Preserve buffer(0 To n - 1)
                Return buffer
            End Using
        End Function

        Private Shared Function OpenWar3PatchArchive(ByVal war3PatchFolder As String) As MPQ.Archive
            Contract.Requires(war3PatchFolder IsNot Nothing)
            Contract.Ensures(Contract.Result(Of MPQ.Archive)() IsNot Nothing)
            Dim normalPath = "{0}War3Patch.mpq".Frmt(war3PatchFolder)
            Dim copyPath = "{0}HostBotTempCopyWar3Patch{1}.mpq".Frmt(war3PatchFolder, My.Settings.exeVersion)
            If IO.File.Exists(copyPath) Then
                Return New MPQ.Archive(copyPath)
            ElseIf IO.File.Exists(normalPath) Then
                Try
                    Return New MPQ.Archive(normalPath)
                Catch e As IO.IOException
                    IO.File.Copy(normalPath, copyPath)
                    Return New MPQ.Archive(copyPath)
                End Try
            Else
                Throw New IO.IOException("Couldn't find War3Patch.mpq")
            End If
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapSha1Checksum(ByVal mapArchive As MPQ.Archive,
                                                       ByVal war3PatchArchive As MPQ.Archive) As Byte()
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())().Length = 20)
            Dim streams As New List(Of IO.Stream)

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Contract.Assume(filename IsNot Nothing)
                Dim mpqToUse = If(mapArchive.Hashtable.Contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                streams.Add(mpqToUse.OpenFileByName(filename))
            Next filename

            'Magic value
            streams.Add(New IO.MemoryStream(New Byte() {&H9E, &H37, &HF1, &H3}))

            'Important map files
            For Each fileset In {"war3map.j|scripts\war3map.j",
                                 "war3map.w3e",
                                 "war3map.wpm",
                                 "war3map.doo",
                                 "war3map.w3u",
                                 "war3map.w3b",
                                 "war3map.w3d",
                                 "war3map.w3a",
                                 "war3map.w3q"}
                Contract.Assume(fileset IsNot Nothing)
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.Hashtable.Contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    streams.Add(mapArchive.OpenFileByName(filenameToUse))
                End If
            Next fileset

            Using f = New IO.BufferedStream(New ConcatStream(streams))
                Using sha = New Security.Cryptography.SHA1Managed()
                    Dim result = sha.ComputeHash(f)
                    Contract.Assume(result IsNot Nothing)
                    Return result
                End Using
            End Using
        End Function

        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Shared Function ComputeStreamXoro(ByVal stream As IO.Stream) As ModInt32
            Contract.Requires(stream IsNot Nothing)
            Dim val As ModInt32 = 0

            With New IO.BinaryReader(New IO.BufferedStream(stream))
                'Process complete dwords
                For repeat = 1 To stream.Length \ 4
                    val = (val Xor .ReadUInt32()).ShiftRotateLeft(3)
                Next repeat

                'Process bytes not in a complete dword
                For repeat = 1 To stream.Length Mod 4
                    val = (val Xor .ReadByte()).ShiftRotateLeft(3)
                Next repeat
            End With

            Return val
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapXoro(ByVal mapArchive As MPQ.Archive,
                                               ByVal war3PatchArchive As MPQ.Archive) As ModInt32
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Dim val As ModInt32 = 0

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Contract.Assume(filename IsNot Nothing)
                Dim mpqToUse = If(mapArchive.Hashtable.Contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                Using f = mpqToUse.OpenFileByName(filename)
                    val = val Xor ComputeStreamXoro(f)
                End Using
            Next filename

            'Magic value
            val = val.ShiftRotateLeft(3)
            val = (val Xor &H3F1379E).ShiftRotateLeft(3)

            'Important map files
            For Each fileset In {"war3map.j|scripts\war3map.j",
                                 "war3map.w3e",
                                 "war3map.wpm",
                                 "war3map.doo",
                                 "war3map.w3u",
                                 "war3map.w3b",
                                 "war3map.w3d",
                                 "war3map.w3a",
                                 "war3map.w3q"}
                Contract.Assume(fileset IsNot Nothing)
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.Hashtable.Contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    Using f = mapArchive.OpenFileByName(filenameToUse)
                        val = (val Xor ComputeStreamXoro(f)).ShiftRotateLeft(3)
                    End Using
                End If
            Next fileset

            Return val
        End Function

        '''<summary>Finds a string in the war3map.wts file. Returns null if the string is not found.</summary>
        Private Shared Function TryGetMapString(ByVal mapArchive As MPQ.Archive,
                                                ByVal key As String) As String
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(key IsNot Nothing)

            'Open strings file and search for given key
            Using sr = New IO.StreamReader(New IO.BufferedStream(mapArchive.OpenFileByName("war3map.wts")))
                Do Until sr.EndOfStream
                    Dim itemKey = sr.ReadLine()
                    If sr.ReadLine <> "{" Then Continue Do
                    Dim itemLines = New List(Of String)
                    Do
                        Dim line = sr.ReadLine()
                        If line = "}" Then Exit Do
                        itemLines.Add(line)
                    Loop
                    If itemKey = key Then
                        Return itemLines.StringJoin(Environment.NewLine)
                    End If
                Loop
            End Using

            'Alternate key
            If key.StartsWith("TRIGSTR_", StringComparison.InvariantCultureIgnoreCase) Then
                Dim suffix = key.Substring("TRIGSTR_".Length)
                Dim id As UInteger
                If UInt32.TryParse(suffix, id) Then
                    Return TryGetMapString(mapArchive, "STRING {0}".Frmt(id))
                End If
            End If

            'Not found
            Return Nothing
        End Function

        ''' <summary>
        ''' Finds a string in the war3map.wts file.
        ''' Returns the key if the string is not found.
        ''' Returns the key and an error description if an exception occurs.
        ''' </summary>
        Private Shared Function SafeGetMapString(ByVal mapArchive As MPQ.Archive,
                                                 ByVal nameKey As String) As String
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(nameKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Try
                Return If(TryGetMapString(mapArchive, nameKey), nameKey)
            Catch e As Exception
                Return "{0} (error reading strings file: {1})".Frmt(nameKey, e.Message)
            End Try
        End Function

        <Flags()>
        Private Enum MapOptions As UInteger
            HideMinimap = 1 << 0
            ModifyAllyPriorities = 1 << 1
            Melee = 1 << 2

            RevealTerrain = 1 << 4
            FixedForces = 1 << 5
            CustomForces = 1 << 6
            CustomTechTree = 1 << 7
            CustomAbilities = 1 << 8
            CustomUpgrades = 1 << 9

            WaterWavesOnCliffShores = 1 << 11
            WaterWavesOnSlopeShores = 1 << 12
        End Enum
        Private Enum MapInfoFormatVersion As Integer
            ROC = 18
            TFT = 25
        End Enum
        Private Class ReadMapInfoResult
            Public ReadOnly playableWidth As Integer
            Public ReadOnly playableHeight As Integer
            Public ReadOnly isMelee As Boolean
            Public ReadOnly slots As List(Of W3Slot)
            Public ReadOnly name As String
            Public Sub New(ByVal name As String,
                           ByVal playableWidth As Integer,
                           ByVal playableHeight As Integer,
                           ByVal isMelee As Boolean,
                           ByVal slots As List(Of W3Slot))
                Me.playableHeight = playableHeight
                Me.playableWidth = playableWidth
                Me.isMelee = isMelee
                Me.slots = slots
                Me.name = name
            End Sub
        End Class
        '''<summary>Reads map information from the "war3map.w3i" file in the map mpq archive.</summary>
        '''<source>war3map.w3i format found at http://www.wc3campaigns.net/tools/specs/index.html by Zepir/PitzerMike</source>
        Private Shared Function ReadMapInfo(ByVal mapArchive As MPQ.Archive) As ReadMapInfoResult
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReadMapInfoResult)() IsNot Nothing)

            Using br = New IO.BinaryReader(New IO.BufferedStream(mapArchive.OpenFileByName("war3map.w3i")))
                Dim fileFormat = CType(br.ReadInt32(), MapInfoFormatVersion)
                If Not fileFormat.EnumValueIsDefined Then
                    Throw New IO.InvalidDataException("Unrecognized war3map.w3i format.")
                End If

                br.ReadInt32() 'number of saves (map version)
                br.ReadInt32() 'editor version (little endian)

                Dim mapName = SafeGetMapString(mapArchive, nameKey:=br.ReadNullTerminatedString()) 'map description key

                br.ReadNullTerminatedString() 'map author
                br.ReadNullTerminatedString() 'map description
                br.ReadNullTerminatedString() 'players recommended
                For repeat = 1 To 8
                    br.ReadSingle()  '"Camera Bounds" as defined in the JASS file
                Next repeat
                For repeat = 1 To 4
                    br.ReadInt32() 'camera bounds complements
                Next repeat

                Dim playableWidth = br.ReadInt32() 'map playable area width
                Dim playableHeight = br.ReadInt32() 'map playable area height
                Dim options = CType(br.ReadInt32(), MapOptions) 'flags

                br.ReadByte() 'map main ground type
                If fileFormat = MapInfoFormatVersion.ROC Then
                    br.ReadInt32() 'Campaign background number (-1 = none)
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'Loading screen background number which is its index in the preset list (-1 = none or custom imported file)
                    br.ReadNullTerminatedString() 'path of custom loading screen model (empty string if none or preset)
                End If
                br.ReadNullTerminatedString() 'Map loading screen text
                br.ReadNullTerminatedString() 'Map loading screen title
                br.ReadNullTerminatedString() 'Map loading screen subtitle
                If fileFormat = MapInfoFormatVersion.ROC Then
                    br.ReadInt32() 'Map loading screen number (-1 = none)
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'used game data set (index in the preset list, 0 = standard)
                    br.ReadNullTerminatedString() 'Prologue screen path
                End If
                br.ReadNullTerminatedString() 'Prologue screen text
                br.ReadNullTerminatedString() 'Prologue screen title
                br.ReadNullTerminatedString() 'Prologue screen subtitle
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'uses terrain fog (0 = not used, greater 0 = index of terrain fog style dropdown box)
                    br.ReadSingle() 'fog start z height
                    br.ReadSingle() 'fog end z height
                    br.ReadSingle() 'fog density
                    br.ReadByte() 'fog red value
                    br.ReadByte() 'fog green value
                    br.ReadByte() 'fog blue value
                    br.ReadByte() 'fog alpha value
                    br.ReadInt32() 'global weather id (0 = none, else it's set to the 4-letter-id of the desired weather found in TerrainArt\Weather.slk)
                    br.ReadNullTerminatedString() 'custom sound environment (set to the desired sound label)
                    br.ReadByte() 'tileset id of the used custom light environment
                    br.ReadByte() 'custom water tinting red value
                    br.ReadByte() 'custom water tinting green value
                    br.ReadByte() 'custom water tinting blue value
                    br.ReadByte() 'custom water tinting alpha value
                End If

                'Player Slots
                Dim numSlotsInFile = br.ReadInt32()
                If numSlotsInFile <= 0 OrElse numSlotsInFile > 12 Then
                    Throw New IO.InvalidDataException("Invalid number of slots.")
                End If
                Dim slots = New List(Of W3Slot)
                Dim slotColorMap = New Dictionary(Of W3Slot.PlayerColor, W3Slot)
                For repeat = 0 To numSlotsInFile - 1
                    Dim slot = New W3Slot(Nothing, CByte(slots.Count + 1))
                    'color
                    slot.color = CType(br.ReadInt32(), W3Slot.PlayerColor)
                    If Not slot.color.EnumValueIsDefined Then Throw New IO.InvalidDataException("Unrecognized map slot color.")
                    'type
                    Select Case br.ReadInt32() '0=?, 1=available, 2=cpu, 3=unused
                        Case 1 : slot.Contents = New W3SlotContentsOpen(slot)
                        Case 2 : slot.Contents = New W3SlotContentsComputer(slot, W3Slot.ComputerLevel.Normal)
                        Case 3 : slot = Nothing
                        Case Else
                            Throw New IO.InvalidDataException("Unrecognized map slot type.")
                    End Select
                    'race
                    Dim race = W3Slot.Races.Random
                    Select Case br.ReadInt32()
                        Case 1 : race = W3Slot.Races.Human
                        Case 2 : race = W3Slot.Races.Orc
                        Case 3 : race = W3Slot.Races.Undead
                        Case 4 : race = W3Slot.Races.NightElf
                        Case Else
                            Throw New IO.InvalidDataException("Unrecognized map slot race.")
                    End Select
                    'player
                    br.ReadInt32() 'fixed start position
                    br.ReadNullTerminatedString() 'slot player name
                    br.ReadSingle() 'start position x
                    br.ReadSingle() 'start position y
                    br.ReadInt32() 'ally low priorities
                    br.ReadInt32() 'ally high priorities

                    If slot IsNot Nothing Then
                        slots.Add(slot)
                        slot.race = race
                        slotColorMap(slot.color) = slot
                    End If
                Next repeat

                'Forces
                Dim numForces = br.ReadInt32()
                If numForces <= 0 OrElse numForces > 12 Then
                    Throw New IO.InvalidDataException("Invalid number of forces.")
                End If
                For teamIndex = CByte(0) To CByte(numForces - 1)
                    br.ReadInt32() 'force flags
                    Dim memberBitField = br.ReadUInt32() 'force members
                    br.ReadNullTerminatedString() 'force name

                    For Each color In EnumValues(Of W3Slot.PlayerColor)()
                        If Not CBool((memberBitField >> CInt(color)) And &H1) Then Continue For
                        If Not slotColorMap.ContainsKey(color) Then Continue For
                        Contract.Assume(slotColorMap(color) IsNot Nothing)
                        slotColorMap(color).Team = teamIndex
                    Next color
                Next teamIndex

                '... more data in the file but it isn't needed ...

                Dim isMelee = CBool(options And MapOptions.Melee)
                If isMelee Then
                    For i = 0 To slots.Count - 1
                        slots(i).Team = CByte(i)
                        slots(i).race = W3Slot.Races.Random
                    Next i
                End If
                Return New ReadMapInfoResult(mapName, playableWidth, playableHeight, isMelee, slots)
            End Using
        End Function
#End Region
    End Class
End Namespace