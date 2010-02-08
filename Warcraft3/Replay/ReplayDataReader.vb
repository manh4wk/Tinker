﻿Namespace WC3.Replay
    ''' <summary>
    ''' Exposes warcraft 3 replay data as an IRandomReadableStream.
    ''' </summary>
    Public NotInheritable Class ReplayDataReader
        Inherits FutureDisposable
        Implements IRandomReadableStream

        Private Const BlockHeaderSize As Integer = 8

        Private Structure BlockInfo
            Private ReadOnly _blockPosition As Long
            Private ReadOnly _blockLength As Long
            Private ReadOnly _dataPosition As Long
            Private ReadOnly _dataLength As Long

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_blockPosition >= 0)
                Contract.Invariant(_blockLength > 0)
                Contract.Invariant(_dataPosition >= 0)
                Contract.Invariant(_dataLength > 0)
            End Sub

            Public Sub New(ByVal blockPosition As Long,
                           ByVal blockLength As Long,
                           ByVal dataPosition As Long,
                           ByVal dataLength As Long)
                Contract.Requires(blockPosition >= 0)
                Contract.Requires(blockLength > 0)
                Contract.Requires(dataPosition >= 0)
                Contract.Requires(dataLength > 0)
                Me._blockPosition = blockPosition
                Me._blockLength = blockLength
                Me._dataPosition = dataPosition
                Me._dataLength = dataLength
            End Sub

            Public ReadOnly Property BlockPosition As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() >= 0)
                    Return _blockPosition
                End Get
            End Property
            Public ReadOnly Property BlockLength As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() > 0)
                    Return _blockLength
                End Get
            End Property
            Public ReadOnly Property DataPosition As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() >= 0)
                    Return _dataPosition
                End Get
            End Property
            Public ReadOnly Property DataLength As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() > 0)
                    Return _dataLength
                End Get
            End Property
            Public ReadOnly Property NextBlockPosition As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() = BlockPosition + BlockLength)
                    Return BlockPosition + BlockLength
                End Get
            End Property
            Public ReadOnly Property NextDataPosition As Long
                Get
                    Contract.Ensures(Contract.Result(Of Long)() = DataPosition + DataLength)
                    Return DataPosition + DataLength
                End Get
            End Property
        End Structure

        Private ReadOnly _stream As IRandomReadableStream
        Private ReadOnly _blockCount As UInt32
        Private ReadOnly _length As Long
        Private ReadOnly _blockInfoTable As New List(Of BlockInfo)()

        Private _position As Long
        Private _loadedBlockData As IReadableList(Of Byte)
        Private _loadedBlockIndex As Integer

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_blockInfoTable IsNot Nothing)
            Contract.Invariant(_blockInfoTable.Count <= _blockCount)
            Contract.Invariant(_length >= 0)
            Contract.Invariant(_position >= 0)
            Contract.Invariant(_position <= _length)
            Contract.Invariant(_loadedBlockIndex >= 0)
            'Contract.Invariant(_loadedBlockData Is Nothing OrElse _loadedBlockIndex < _blockInfoTable.Count)
            'Contract.Invariant(_loadedBlockData Is Nothing OrElse _position >= _blockInfoTable(_loadedBlockIndex).DataPosition)
            'Contract.Invariant(_loadedBlockData Is Nothing OrElse _position <= _blockInfoTable(_loadedBlockIndex).NextDataPosition)
        End Sub

        Public Sub New(ByVal subStream As IRandomReadableStream,
                       ByVal blockCount As UInt32,
                       ByVal firstBlockOffset As UInt32,
                       ByVal decompressedSize As UInt32)
            Contract.Requires(subStream IsNot Nothing)
            Contract.Requires(blockCount >= 0)
            Contract.Requires(firstBlockOffset >= 0)
            Me._stream = subStream
            Me._blockCount = blockCount
            Me._length = decompressedSize
            If blockCount > 0 Then
                LoadNextBlockInfo(blockPosition:=firstBlockOffset, dataPosition:=0)
            End If
        End Sub

        ''' <summary>
        ''' Reads the header of the next unexplored block and adds the details to the block info table.
        ''' </summary>
        ''' <param name="blockPosition">The starting position of the block, as determined by the previous block's end.</param>
        ''' <param name="dataPosition">The logical starting position of the data stored in the block.</param>
        <ContractVerification(False)>
        Private Sub LoadNextBlockInfo(ByVal blockPosition As Long, ByVal dataPosition As Long)
            Contract.Requires(_blockInfoTable.Count < _blockCount)
            Contract.Ensures(_blockInfoTable.Count = Contract.OldValue(_blockInfoTable.Count) + 1)
            'Read block header
            _stream.Position = blockPosition
            Dim compressedDataSize = _stream.ReadUInt16()
            Dim decompressedDataSize = _stream.ReadUInt16()
            Dim checksum = _stream.ReadUInt32()
            'Remember
            Dim block = New BlockInfo(blockPosition:=blockPosition,
                                      blockLength:=BlockHeaderSize + compressedDataSize,
                                      dataPosition:=dataPosition,
                                      dataLength:=decompressedDataSize)
            _blockInfoTable.Add(block)
            If _blockInfoTable.Count = _blockCount AndAlso block.NextDataPosition < _length Then
                Throw New IO.InvalidDataException("Less data than indicated by header.")
            End If
        End Sub

        ''' <summary>
        ''' Determines the block info for the given block, filling the block info table as necessary.
        ''' </summary>
        Private Function ReadBlockInfo(ByVal blockIndex As Integer) As BlockInfo
            Contract.Requires(blockIndex >= 0)
            Contract.Requires(blockIndex < _blockCount)
            Contract.Ensures(_blockInfoTable.Count > blockIndex)
            'Add to table until it contains the desired block
            While _blockInfoTable.Count <= blockIndex
                Dim prev = _blockInfoTable.Last
                LoadNextBlockInfo(prev.NextBlockPosition, prev.NextDataPosition)
            End While
            'Retrieve from table
            Return _blockInfoTable(blockIndex)
        End Function
        ''' <summary>
        ''' Determines the block data for the given block, filling the block info table as necessary.
        ''' </summary>
        <ContractVerification(False)>
        Private Function ReadBlockData(ByVal blockIndex As Integer) As IReadableList(Of Byte)
            Contract.Requires(blockIndex >= 0)
            Contract.Requires(blockIndex < _blockCount)
            Contract.Ensures(_blockInfoTable.Count > blockIndex)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            'Locate
            Dim block = ReadBlockInfo(blockIndex)
            _stream.Position = block.BlockPosition + BlockHeaderSize
            'Retrieve
            Dim dataStream = New ZLibStream(_stream.AsStream, IO.Compression.CompressionMode.Decompress)
            Contract.Assume(dataStream.CanRead)
            Return dataStream.ReadBytesExact(length:=CInt(block.DataLength)).AsReadableList
        End Function

        '''<summary>Determines the block which contains the given position.</summary>
        Private Function FindBlockIndexAt(ByVal position As Long) As Integer
            Contract.Requires(position >= 0)
            Contract.Requires(position < _length)
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            Contract.Ensures(Contract.Result(Of Integer)() < _blockCount)

            'Optimistic local check
            For i = _loadedBlockIndex - 1 To _loadedBlockIndex + 1
                If i < 0 Then Continue For
                If i >= _blockCount Then Exit For
                Dim block = ReadBlockInfo(i)
                If position >= block.DataPosition AndAlso position < block.NextDataPosition Then
                    Return i
                End If
            Next i

            'Binary search
            Dim min = 0
            Dim max = CInt(_blockCount) - 1
            Do Until max < min
                Dim med = (min + max) \ 2
                Dim block = ReadBlockInfo(med)
                If position < block.DataPosition Then
                    max = med - 1
                ElseIf position >= block.NextDataPosition Then
                    min = med + 1
                Else
                    Return med
                End If
            Loop

            Throw New UnreachableException("A valid position was not contained in any block.")
        End Function

        Public Function Read(ByVal maxCount As Integer) As IReadableList(Of Byte) Implements IReadableStream.Read
            Dim result = New List(Of Byte)
            If _blockCount = 0 Then Return result.AsReadableList

            'Load first block on first read
            If _loadedBlockData Is Nothing Then
                Contract.Assume(_loadedBlockIndex = 0)
                _loadedBlockData = ReadBlockData(0)
            End If

            While result.Count < maxCount AndAlso _position < _length
                Contract.Assume(_loadedBlockIndex < _blockInfoTable.Count)
                Dim blockInfo = _blockInfoTable(_loadedBlockIndex)

                'Advance to next block as necessary
                Contract.Assume(_position >= blockInfo.DataPosition)
                Contract.Assume(_position <= blockInfo.NextDataPosition)
                If _position = blockInfo.NextDataPosition Then
                    _loadedBlockIndex += 1
                    Contract.Assume(_loadedBlockIndex < _blockCount)
                    blockInfo = ReadBlockInfo(_loadedBlockIndex)
                    Contract.Assume(_loadedBlockIndex < _blockInfoTable.Count)
                    _loadedBlockData = ReadBlockData(_loadedBlockIndex)
                End If
                Contract.Assume(_position >= blockInfo.DataPosition)
                Contract.Assume(_position < blockInfo.NextDataPosition)

                'Append block data to result
                Dim relativePosition = CInt(_position - blockInfo.DataPosition)
                Contract.Assume(relativePosition < _loadedBlockData.Count)
                Dim remainingBlockData = _loadedBlockData.SubView(relativePosition)
                Dim n = Math.Min(Math.Min(maxCount - result.Count, remainingBlockData.Count), _length - _position)
                result.AddRange(remainingBlockData.SubView(0, CInt(n)))
                _position += n
            End While

            Contract.Assume(result.Count <= maxCount)
            Return result.AsReadableList
        End Function

        Public ReadOnly Property Length As Long Implements ISeekableStream.Length
            Get
                Return _length
            End Get
        End Property

        Public Property Position As Long Implements ISeekableStream.Position
            Get
                Contract.Assume(_position <= Length)
                Return _position
            End Get
            <ContractVerification(False)>
            Set(ByVal value As Long)
                If value < _length Then
                    Dim newBlockIndex = FindBlockIndexAt(value)
                    If _loadedBlockData Is Nothing OrElse _loadedBlockIndex <> newBlockIndex Then
                        _loadedBlockData = ReadBlockData(newBlockIndex)
                        _loadedBlockIndex = newBlockIndex
                    End If
                End If
                _position = value
            End Set
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            _stream.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace