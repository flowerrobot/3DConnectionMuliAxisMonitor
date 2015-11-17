Imports System.Xml

Public Class ConfigMonitor
    Public Const ConfigLocation As String = "C:\Users\setruh\AppData\Roaming\3Dconnexion\3DxWare\Cfg\SolidWorks.xml"

    Sub New(EntryPoint As MainWindow)
        _EntryPoint = EntryPoint
    End Sub
    Private _EntryPoint As MainWindow

    Public Sub ReadFile()
        Try
            If IO.File.Exists(ConfigLocation) Then
                Dim XmlFile = New XmlDocument()
                XmlFile.Load(ConfigLocation)
                For Each TopNodes As XmlNode In XmlFile.ChildNodes(1).ChildNodes
                    If TopNodes.Name = "Devices" Then
                        For Each ChildNode As XmlNode In TopNodes.ChildNodes
                            If ChildNode.Name = "Device" Then
                                If CheckDevise(ChildNode) Then
                                    Exit Sub
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch
        End Try
    End Sub
    Public Function CheckDevise(DeviseNode As XmlNode) As Boolean
        Dim IsEnabled As Boolean
        Dim ActionIDFound As Boolean
        For Each TopNode As XmlNode In DeviseNode.ChildNodes
            If TopNode.Name = "Axis" Then
                For Each ChildNode As XmlNode In TopNode.ChildNodes

                    Select Case ChildNode.Name
                        Case "Enabled"
                            IsEnabled = ChildNode.ChildNodes(0).Value
                        Case "Input"
                            For Each SubNode As XmlNode In ChildNode.ChildNodes
                                If SubNode.Name = "ActionID" Then
                                    If SubNode.ChildNodes(0).Value = "HIDMultiAxis_Rx" Then
                                        ActionIDFound = True
                                    End If
                                End If
                            Next
                    End Select
                Next
            End If
            If ActionIDFound Then
                _EntryPoint.MulitAxisIsOff = Not IsEnabled
                Return ActionIDFound
            End If
        Next
        Return False
    End Function

End Class
