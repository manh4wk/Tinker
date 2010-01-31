﻿Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values
Imports Tinker.Pickling
Imports System.Collections.Generic

<TestClass()>
Public Class PicklingTest
#Region "Numeric Jars"
    <TestMethod()>
    Public Sub ByteJarTest()
        Dim jar = New ByteJar("jar")
        JarTest(jar, 0, {0})
        JarTest(jar, 1, {1})
        JarTest(jar, Byte.MaxValue, {Byte.MaxValue})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))

        jar = New ByteJar("jar", showHex:=True)
        JarTest(jar, 0, {0})
        JarTest(jar, 1, {1})
        JarTest(jar, Byte.MaxValue, {Byte.MaxValue})
    End Sub
    <TestMethod()>
    Public Sub UInt16JarTest()
        Dim jar = New UInt16Jar("jar")
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {1, 0})
        JarTest(jar, 256, {0, 1})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))

        jar = New UInt16Jar("jar", showHex:=True)
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {1, 0})
        JarTest(jar, 256, {0, 1})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})

        jar = New UInt16Jar("jar", ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {0, 1})
        JarTest(jar, 256, {1, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub UInt32JarTest()
        Dim jar = New UInt32Jar("jar")
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))

        jar = New UInt32Jar("jar", showHex:=True)
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})

        jar = New UInt32Jar("jar", ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {0, 0, 0, 1})
        JarTest(jar, 256, {0, 0, 1, 0})
        JarTest(jar, UInt16.MaxValue, {0, 0, &HFF, &HFF})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub UInt64JarTest()
        Dim jar = New UInt64Jar("jar")
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF, 0, 0, 0, 0})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5, 6, 7}.AsReadableList))

        jar = New UInt64Jar("jar", showHex:=True)
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF, 0, 0, 0, 0})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})

        jar = New UInt64Jar("jar", ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {0, 0, 0, 0, 0, 0, 0, 1})
        JarTest(jar, 256, {0, 0, 0, 0, 0, 0, 1, 0})
        JarTest(jar, UInt16.MaxValue, {0, 0, 0, 0, 0, 0, &HFF, &HFF})
        JarTest(jar, UInt32.MaxValue, {0, 0, 0, 0, &HFF, &HFF, &HFF, &HFF})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub Float32JarTest()
        Dim jar = New Float32Jar("jar")
        Dim m = New IO.MemoryStream()
        Dim br = New IO.BinaryWriter(m)

        m.Position = 0
        br.Write(CSng(1))
        JarTest(jar, 1, m.ToArray)

        m.Position = 0
        br.Write(CSng(2))
        JarTest(jar, 2, m.ToArray)

        m.Position = 0
        br.Write(Single.PositiveInfinity)
        JarTest(jar, Single.PositiveInfinity, m.ToArray)

        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub Float64JarTest()
        Dim jar = New Float64Jar("jar")
        Dim m = New IO.MemoryStream()
        Dim br = New IO.BinaryWriter(m)

        m.Position = 0
        br.Write(CDbl(1))
        JarTest(jar, 1, m.ToArray)

        m.Position = 0
        br.Write(CDbl(2))
        JarTest(jar, 2, m.ToArray)

        m.Position = 0
        br.Write(Double.PositiveInfinity)
        JarTest(jar, Double.PositiveInfinity, m.ToArray)

        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5, 6, 7}.AsReadableList))
    End Sub
#End Region

#Region "Enum"
    Private Enum E8 As Byte
        E0 = 0
        E3 = 3
    End Enum
    <Flags()>
    Private Enum F8 As Byte
        F1 = 1 << 1
        F7 = 1 << 7
    End Enum
    <TestMethod()>
    Public Sub EnumByteJarTest_Value()
        Dim jar = New EnumByteJar(Of E8)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E0, {0})
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E3, {3})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {2}.AsReadableList)

        jar = New EnumByteJar(Of E8)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E0, {0})
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E3, {3})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumByteJarTest_Flags()
        Dim jar = New EnumByteJar(Of F8)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1, {2})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F7, {128})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1 Or F8.F7, {130})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {1}.AsReadableList)
        jar.Parse(New Byte() {3}.AsReadableList)

        jar = New EnumByteJar(Of F8)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1, {2})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F7, {128})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1 Or F8.F7, {130})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3}.AsReadableList))
    End Sub
    Private Enum E16 As UInt16
        E0 = 0
        E3 = 3
        EM = UInt16.MaxValue
    End Enum
    <Flags()>
    Private Enum F16 As UInt16
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1US << 15
    End Enum
    <TestMethod()>
    Public Sub EnumUInt16JarTest_Value()
        Dim jar = New EnumUInt16Jar(Of E16)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E0, {0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E3, {3, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.EM, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        jar.Parse(New Byte() {2, 0}.AsReadableList)

        jar = New EnumUInt16Jar(Of E16)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E0, {0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E3, {3, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.EM, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt16JarTest_Flags()
        Dim jar = New EnumUInt16Jar(Of F16)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1, {2, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F7, {128, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1 Or F16.F7, {130, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.FM, {0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        jar.Parse(New Byte() {1, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0}.AsReadableList)

        jar = New EnumUInt16Jar(Of F16)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1, {2, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F7, {128, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1 Or F16.F7, {130, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.FM, {0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0}.AsReadableList))
    End Sub
    Private Enum E32 As UInt32
        E0 = 0
        E3 = 3
        EM = UInt32.MaxValue
    End Enum
    <Flags()>
    Private Enum F32 As UInt32
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1UI << 31
    End Enum
    <TestMethod()>
    Public Sub EnumUInt32JarTest_Value()
        Dim jar = New EnumUInt32Jar(Of E32)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E0, {0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E3, {3, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.EM, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {2, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt32Jar(Of E32)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E0, {0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E3, {3, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.EM, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0, 0, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt32JarTest_Flags()
        Dim jar = New EnumUInt32Jar(Of F32)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1, {2, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F7, {128, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1 Or F32.F7, {130, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.FM, {0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {1, 0, 0, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt32Jar(Of F32)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1, {2, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F7, {128, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1 Or F32.F7, {130, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.FM, {0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0, 0, 0}.AsReadableList))
    End Sub
    Private Enum E64 As UInt64
        E0 = 0
        E3 = 3
        EM = UInt64.MaxValue
    End Enum
    <Flags()>
    Private Enum F64 As UInt64
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1UL << 63
    End Enum
    <TestMethod()>
    Public Sub EnumUInt64JarTest_Value()
        Dim jar = New EnumUInt64Jar(Of E64)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E3, {3, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.EM, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {2, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt64Jar(Of E64)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E3, {3, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.EM, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt64JarTest_Flags()
        Dim jar = New EnumUInt64Jar(Of F64)("jar", checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1, {2, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F7, {128, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1 Or F64.F7, {130, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.FM, {0, 0, 0, 0, 0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {1, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt64Jar(Of F64)("jar", checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1, {2, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F7, {128, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1 Or F64.F7, {130, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.FM, {0, 0, 0, 0, 0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
    End Sub
#End Region

    <TestMethod()>
    Public Sub RawDataJarTest()
        Dim jar = New RawDataJar("jar", size:=0)
        Dim equater As Func(Of IReadableList(Of Byte), IReadableList(Of Byte), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New Byte() {}.AsReadableList, {})

        jar = New RawDataJar("jar", size:=3)
        JarTest(jar, equater, New Byte() {1, 2, 3}.AsReadableList, {1, 2, 3})
        JarTest(jar, equater, New Byte() {7, 6, 5}.AsReadableList, {7, 6, 5})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub SizePrefixedDataJarTest()
        Dim jar = New SizePrefixedDataJar("jar", prefixSize:=1)
        Dim equater As Func(Of IReadableList(Of Byte), IReadableList(Of Byte), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New Byte() {}.AsReadableList, {0})
        JarTest(jar, equater, New Byte() {1, 2, 9}.AsReadableList, {3, 1, 2, 9})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))

        jar = New SizePrefixedDataJar("jar", prefixSize:=3)
        JarTest(jar, equater, New Byte() {}.AsReadableList, {0, 0, 0})
        JarTest(jar, equater, New Byte() {1, 2, 3}.AsReadableList, {3, 0, 0, 1, 2, 3})
        JarTest(jar, equater, New Byte() {7, 6, 5, 4}.AsReadableList, {4, 0, 0, 7, 6, 5, 4})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub RemainingDataJarTest()
        Dim jar = New RemainingDataJar("jar")
        Dim equater As Func(Of IReadableList(Of Byte), IReadableList(Of Byte), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New Byte() {}.AsReadableList, {}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {1}.AsReadableList, {1}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {1, 2, 3}.AsReadableList, {1, 2, 3}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {7, 6, 5}.AsReadableList, {7, 6, 5}, appendSafe:=False)
    End Sub

    <TestMethod()>
    Public Sub ListJarTest()
        Dim jar = New ListJar(Of UInt16)("jar", New UInt16Jar("jar"), prefixSize:=1)
        Dim equater As Func(Of IList(Of UInt16), IList(Of UInt16), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, {}, {0})
        JarTest(jar, equater, {0}, {1, 0, 0})
        JarTest(jar, equater, {1}, {1, 1, 0})
        JarTest(jar, equater, {1, 2, UInt16.MaxValue}, {3, 1, 0, 2, 0, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))

        jar = New ListJar(Of UInt16)("jar", New UInt16Jar("jar"), prefixSize:=4)
        JarTest(jar, equater, {}, {0, 0, 0, 0})
        JarTest(jar, equater, {0}, {1, 0, 0, 0, 0, 0})
        JarTest(jar, equater, {1}, {1, 0, 0, 0, 1, 0})
        JarTest(jar, equater, {1, 2, UInt16.MaxValue}, {3, 0, 0, 0, 1, 0, 2, 0, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0, 5}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub RepeatingJarTest()
        Dim jar = New RepeatingJar(Of UInt16)("jar", New UInt16Jar("jar"))
        Dim equater As Func(Of IList(Of UInt16), IList(Of UInt16), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, {}, {}, appendSafe:=False)
        JarTest(jar, equater, {0}, {0, 0}, appendSafe:=False)
        JarTest(jar, equater, {1}, {1, 0}, appendSafe:=False)
        JarTest(jar, equater, {1, 2, UInt16.MaxValue}, {1, 0, 2, 0, &HFF, &HFF}, appendSafe:=False)
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub NullTerminatedStringJarTest()
        Dim jar = New NullTerminatedStringJar("jar")
        JarTest(jar, "", {0})
        JarTest(jar, "a", {Asc("a"), 0})
        JarTest(jar, "ab", {Asc("a"), Asc("b"), 0})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {Asc("a"), Asc("b")}.AsReadableList))
        jar = New NullTerminatedStringJar("jar", maximumContentSize:=1)
        JarTest(jar, "", {0})
        JarTest(jar, "a", {Asc("a"), 0})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {Asc("a"), Asc("b"), 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub FixedSizeStringJarTest()
        Dim jar = New FixedSizeStringJar("jar", size:=1)
        JarTest(jar, "1", {Asc("1")})
        JarTest(jar, "a", {Asc("a")})
        Assert.IsTrue(jar.Parse(New Byte() {Asc("a"), Asc("b")}.AsReadableList).Data.Count = 1)
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub TupleJarTest()
        Dim jar = New TupleJar("jar", New UInt32Jar("32").Weaken, New UInt16Jar("16").Weaken)
        Dim equater As Func(Of Dictionary(Of InvariantString, Object), Dictionary(Of InvariantString, Object), Boolean)
        equater = Function(x, y)
                      For Each key In x.Keys.Concat(y.Keys).Distinct()
                          If Not x.ContainsKey(key) OrElse Not y.ContainsKey(key) Then Return False
                          If Not x(key).Equals(y(key)) Then Return False
                      Next key
                      Return True
                  End Function
        JarTest(jar, equater, New Dictionary(Of InvariantString, Object)() From {{"32", 0UI}, {"16", 0US}}, {0, 0, 0, 0, 0, 0})
        JarTest(jar, equater, New Dictionary(Of InvariantString, Object)() From {{"32", UInt32.MaxValue}, {"16", 0US}}, {&HFF, &HFF, &HFF, &HFF, 0, 0})
        JarTest(jar, equater, New Dictionary(Of InvariantString, Object)() From {{"32", 0UI}, {"16", UInt16.MaxValue}}, {0, 0, 0, 0, &HFF, &HFF})
        JarTest(jar, equater, New Dictionary(Of InvariantString, Object)() From {{"32", 1UI}, {"16", 2US}}, {1, 0, 0, 0, 2, 0})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5}.AsReadableList))
    End Sub
End Class
