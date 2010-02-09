Namespace Pickling
    '''<summary>Pickles values where the serialized form is prefixed with the number of bytes used (not counting the prefix).</summary>
    Public NotInheritable Class DataSizePrefixedJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal prefixSize As Integer)
            MyBase.New(subJar.Name)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(prefixSize <= 8)
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
            Me._prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Dim sizeBytes = CULng(pickle.Data.Count).Bytes.Take(_prefixSize)
            If sizeBytes.Take(_prefixSize).ToUInt64 <> pickle.Data.Count Then Throw New PicklingException("Unable to fit byte count into size prefix.")
            Dim data = sizeBytes.Concat(pickle.Data).ToArray.AsReadableList
            Return New Pickle(Of TValue)(value, data, pickle.Description)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException()
            Dim dataSize = data.SubView(0, _prefixSize).ToUInt64
            If data.Count < _prefixSize + dataSize Then Throw New PicklingNotEnoughDataException()

            Dim datum = data.SubView(0, CInt(_prefixSize + dataSize))
            Dim pickle = _subJar.Parse(datum.SubView(_prefixSize))
            If pickle.Data.Count < dataSize Then Throw New PicklingException("Fragmented data.")
            Return New Pickle(Of T)(pickle.Value, datum, pickle.Description)
        End Function
    End Class
End Namespace