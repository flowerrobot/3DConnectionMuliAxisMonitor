Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports PropertyChanged
Imports SolidWorks.Interop.sldworks
Imports SolidWorks.Interop.swconst

<ImplementPropertyChanged>
Class MainWindow
#Region "Properties"
    Dim PropLock As New Object
    Dim _MulitAxisIsOff As Boolean
    Dim Timer As New Forms.Timer
    Dim cfg As New ConfigMonitor(Me)
    Dim watcher As New FileSystemWatcher
    Dim Main As Process
    Dim ptr As IntPtr
    Dim hhook As IntPtr
    Dim TaskBarIcon As NotifyIcon

    Dim sld As SldWorks = Nothing

    Public Property MulitAxisIsOff As Boolean
        Get
            SyncLock PropLock
                Return _MulitAxisIsOff
            End SyncLock
        End Get
        Set(value As Boolean)
            SyncLock PropLock
                _MulitAxisIsOff = value
                If value Then
                    MainImage.Visibility = Visibility.Visible
                Else
                    MainImage.Visibility = Visibility.Collapsed
                End If


            End SyncLock
        End Set
    End Property

#End Region

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        Me.DataContext = Me

        ' Me.TaskbarItemInfo.Overlay = CType(Resources("OkImage"), ImageSource)


        TaskBarIcon = New NotifyIcon()
        TaskBarIcon.Visible = True

        Dim MenuItem As New MenuItem("Close")
        AddHandler MenuItem.Click, AddressOf MenuItem_Close_Click

        Dim MenuItem2 As New MenuItem("Refresh Postion")
        AddHandler MenuItem2.Click, AddressOf MenuItem_Refresh_Click

        TaskBarIcon.Text = "3D Connection Multi axis monitor"
        TaskBarIcon.Icon = My.Resources.rotation_circle_full_rotate_arrow
        TaskBarIcon.ContextMenu = New ContextMenu({MenuItem, MenuItem2})
        AddHandler TaskBarIcon.MouseClick, AddressOf TaskBarClick

        'Read 3D connection config file 
        cfg.ReadFile()

        'Watch the config file for changes
        watcher.Path = IO.Path.GetDirectoryName(ConfigMonitor.ConfigLocation)
        AddHandler watcher.Changed, AddressOf CheckConfig
        watcher.EnableRaisingEvents = True

        'Find the window to track
        FindProccess()

        'Not used as it does not find the correct main window.
        'hhook = SetWinEventHook(WM_MOVE, WM_SIZE, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT)

        'Use timer to unsure icon stays relative to software
        Timer.Interval = 10
        AddHandler Timer.Tick, AddressOf Ticker
        Timer.Start()
    End Sub

    Private Sub TaskBarClick(sender As Object, e As MouseEventArgs)
        'TaskBarIcon.ContextMenu.Show(TaskBarIcon.conn, e.Location)
        Dim mi As MethodInfo = GetType(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance Or BindingFlags.NonPublic)
        mi.Invoke(TaskBarIcon, Nothing)
    End Sub

    Private Sub MainForm_Deactivated(sender As Object, e As EventArgs)
        Me.Topmost = True
    End Sub

    Private Sub MenuItem_Close_Click(sender As Object, e As Object)
        My.Settings.Save()
        Me.Close()
    End Sub
    Private Sub MenuItem_Refresh_Click(sender As Object, e As Object)
        Main = Nothing
    End Sub
    Private Sub CheckConfig(sender As Object, e As FileSystemEventArgs)
        Try
            If e.FullPath.ToUpper = ConfigMonitor.ConfigLocation.ToUpper Then
                Me.Dispatcher.Invoke(New Action(Sub() cfg.ReadFile()))
            End If
        Catch
            MsgBox("Can not read config file")
        End Try
    End Sub
    Private Sub HostResized(rect As Rect)
        Me.Dispatcher.Invoke(
             New Action(Sub()
                            Me.Topmost = False



                            Dim Right, Bottom As Integer
                            Right = rect.Right
                            Bottom = rect.Bottom

                            'Dim Rec As System.Windows.Rect = System.Windows.SystemParameters.WorkArea
                            'not used, always keep it relative
                            'If Rec.BottomRight.X > rect.Right Then
                            '    Right = rect.Right
                            'Else
                            '    Right = Rec.BottomRight.X
                            'End If

                            'If Rec.BottomRight.Y > rect.Bottom Then
                            '    Bottom = rect.Bottom
                            'Else
                            '    Bottom = Rec.BottomRight.Y
                            'End If



                            Me.Left = (Right - 50 - Me.Width)
                            Me.Top = (Bottom - 50 - Me.Height)
                            Me.Topmost = True
                        End Sub))
    End Sub
    Private Function FindProccess() As Process
        Dim processes As Process() = Process.GetProcessesByName("SLDWORKS")
        If processes.Count > 1 Then
#If DEBUG Then
            Stop
#End If

        End If
        If processes.Count <> 0 Then
            Main = processes.FirstOrDefault
            ptr = Main.MainWindowHandle
            Return processes.FirstOrDefault
        End If
        Return Nothing
    End Function
    Public Sub GetSolidworks()
        sld = GetObject(, "SldWorks.Application")
        AddHandler sld.DestroyNotify, AddressOf SLDClosingEvent
    End Sub
    Private Function SldMethod(ByRef State As swWindowState_e) As Rect
        Try
            If (sld Is Nothing) Then Return Nothing

            Dim rets As New Rect
            rets.Left = sld.FrameLeft
            rets.Top = sld.FrameTop
            rets.Bottom = sld.FrameTop + sld.FrameHeight
            rets.Right = sld.FrameLeft + sld.FrameWidth

            State = sld.FrameState

            Return rets
        Catch
            Return Nothing
        End Try
    End Function

    Private Function SLDClosingEvent() As Integer
        RemoveHandler sld.DestroyNotify, AddressOf SLDClosingEvent
        sld = Nothing
    End Function

    Private Sub Ticker(sender As Object, e As EventArgs)
        Try
            Me.Topmost = True

            If (sld Is Nothing) Then GetSolidworks()

            Dim State As swWindowState_e
            Dim Window As Rect = SldMethod(State)

            If Window.Equals(Nothing) OrElse State = swWindowState_e.swWindowMinimized Then
                If (Me.Visibility = Visibility.Visible) Then
                    Me.Dispatcher.Invoke(New Action(Sub()
                                                        Me.Visibility = Visibility.Collapsed
                                                    End Sub))
                End If
            Else
                If Me.Visibility = Visibility.Collapsed Then
                    Me.Dispatcher.Invoke(New Action(Sub()
                                                        Me.Visibility = Visibility.Visible
                                                    End Sub))

                End If
                HostResized(Window)
            End If


            ''Dim NotepadRect As New Rect
            '''If Main Is Nothing OrElse Main.HasExited OrElse ptr.ToInt32 = 0 Then
            ''FindProccess()
            ''If GetWindowRect(ptr, NotepadRect) Then
            ''    If NotepadRect.Bottom = 0 AndAlso NotepadRect.Right = 0 Then
            ''        Main = Nothing
            ''    End If

            ''    If (NotepadRect.Top < -30000 OrElse NotepadRect.Left < -30000) Then
            ''        If (Me.Visibility = Visibility.Visible) Then
            ''            Me.Dispatcher.Invoke(New Action(Sub()
            ''                                                Me.Visibility = Visibility.Collapsed
            ''                                            End Sub))
            ''        End If
            ''    ElseIf Me.Visibility = Visibility.Collapsed Then
            ''        Me.Dispatcher.Invoke(New Action(Sub()
            ''                                            Me.Visibility = Visibility.Visible
            ''                                        End Sub))
            ''    End If
            ''    HostResized(NotepadRect)
            ''    'Me.Dispatcher.Invoke(
            ''    '    New Action(Sub()
            ''    '                   'cfg.ReadFile()
            ''    '                   Try

            ''    '                       Me.Left = (NotepadRect.Right - 50 - Me.Width)
            ''    '                       Me.Top = (NotepadRect.Bottom - 50 - Me.Height)

            ''    '                   Catch
            ''    '                   End Try
            ''    '               End Sub))
            ''Else
            ''    Me.Dispatcher.Invoke(New Action(Sub()
            ''                                        Me.Visibility = Visibility.Collapsed
            ''                                    End Sub))
            ''End If
        Catch
            Main = Nothing
        End Try
    End Sub

#Region "Win32"
    Public Delegate Sub WinEventDelegate(hWinEventHook As IntPtr, eventType As UInteger, hwnd As IntPtr, idObject As Integer, idChild As Integer, dwEventThread As UInteger, dwmsEventTime As UInteger)
    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Public Shared Function FindWindow(strClassName As String, strWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Shared Function GetWindowRect(hwnd As IntPtr, ByRef rectangle As Rect) As Boolean
    End Function
    <DllImport("user32.dll")>
    Public Shared Function SetWinEventHook(eventMin As UInteger, eventMax As UInteger, hmodWinEventProc As IntPtr, lpfnWinEventProc As WinEventDelegate, idProcess As UInteger, idThread As UInteger,
        dwFlags As UInteger) As IntPtr
    End Function
    <DllImport("user32.dll")>
    Private Shared Function UnhookWinEvent(hWinEventHook As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function
    Dim procDelegate As New WinEventDelegate(AddressOf WinEventProc)

    Private Const WM_MOVE As Integer = 3
    Private Const WM_SIZE As Integer = 5
    Const WINEVENT_OUTOFCONTEXT As UInteger = 0

    Private Sub WinEventProc(hWinEventHook As IntPtr, eventType As UInteger, hwnd As IntPtr, idObject As Integer, idChild As Integer, dwEventThread As UInteger,
        dwmsEventTime As UInteger)
        ' filter out non-HWND namechanges... (eg. items within a listbox)
        If idObject <> 0 OrElse idChild <> 0 Then
            Return
        End If

        Dim ID
        GetWindowThreadProcessId(hwnd, ID)
        Dim processes As Process = Process.GetProcessById(ID)

        If processes.ProcessName = "SLDWORKS" Then
            Dim NotepadRect As New Rect
            GetWindowRect(ptr, NotepadRect)
            If NotepadRect.Bottom <> 0 And NotepadRect.Right <> 0 Then
                HostResized(NotepadRect)
            End If
        End If
        '  Console.WriteLine("Text of hwnd changed {0:x8}", hwnd.ToInt32())

    End Sub


#End Region

End Class

<System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)>
Public Structure Rect
    Public Property Left() As Integer
    Public Property Top() As Integer
    Public Property Right() As Integer
    Public Property Bottom() As Integer
End Structure
