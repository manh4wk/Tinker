﻿Public Class HoldPoint(Of TArg)
    Private ReadOnly _handlers As New List(Of Func(Of TArg, IFuture))
    Private ReadOnly _lock As New Object

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_handlers IsNot Nothing)
        Contract.Invariant(_lock IsNot Nothing)
    End Sub

    ''' <summary>
    ''' Includes a handler which is run before the resulting future from calling 'Hold' will become ready.
    ''' Returns an IDisposable which removes the handler when disposed.
    ''' </summary>
    Public Function IncludeActionHandler(ByVal handler As Action(Of TArg)) As IDisposable
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Return IncludeFutureHandler(Function(arg)
                                        Dim result = New FutureAction()
                                        result.SetByCalling(Sub() handler(arg))
                                        Return result
                                    End Function)
    End Function
    ''' <summary>
    ''' Includes a handler whose future result must become ready before the resulting future from calling 'Hold' will become ready.
    ''' Returns an IDisposable which removes the handler when disposed.
    ''' </summary>
    Public Function IncludeFutureHandler(ByVal handler As Func(Of TArg, IFuture)) As IDisposable
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Dim safeHandler = Function(arg As TArg)
                              Dim result = New FutureFunction(Of IFuture)
                              result.SetByEvaluating(Function() handler(arg))
                              Return result.Defuturized
                          End Function
        SyncLock _lock
            _handlers.Add(safeHandler)
        End SyncLock
        Return New DelegatedDisposable(Sub()
                                           SyncLock _lock
                                               _handlers.Remove(safeHandler)
                                           End SyncLock
                                       End Sub)
    End Function

    '''<summary>Evaluates all handlers and returns a future which becomes ready once all handlers have finished.</summary>
    Public Function Hold(ByVal argument As TArg) As IFuture
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Dim handlers As IEnumerable(Of Func(Of TArg, IFuture))
        SyncLock _lock
            handlers = _handlers.ToList
        End SyncLock
        Return (From handler In handlers Select handler(argument)).Defuturized
    End Function
End Class