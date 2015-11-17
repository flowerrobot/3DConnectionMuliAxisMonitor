Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports PropertyChanged

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
    Public Property MulitAxisIsOff As Boolean
        Get
            SyncLock PropLock
                Return _MulitAxisIsOff
            End SyncLock
        End Get
        Set(value As Boolean)
            SyncLock PropLock
                _MulitAxisIsOff = value

            End SyncLock
        End Set
    End Property

#End Region

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        Me.DataContext = Me
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


    Private Sub MainForm_Deactivated(sender As Object, e As EventArgs)
        Me.Topmost = True
    End Sub
    Private Sub MenuItem_Close_Click(sender As Object, e As RoutedEventArgs)
        My.Settings.Save()
        Me.Close()
    End Sub
    Private Sub MenuItem_Refresh_Click(sender As Object, e As RoutedEventArgs)
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
                            Dim Rec As System.Windows.Rect = System.Windows.SystemParameters.WorkArea
                            Dim Right, Bottom As Integer
                            If Rec.BottomRight.X > rect.Right Then
                                Right = Rec.Right
                            Else
                                Right = Rec.BottomRight.X
                            End If

                            If Rec.BottomRight.Y > rect.Bottom Then
                                Bottom = Rec.Bottom
                            Else
                                Bottom = Rec.BottomRight.Y
                            End If


                            Me.Left = (Right - 50 - Me.Width)
                            Me.Top = (Bottom - 50 - Me.Height)
                        End Sub))
    End Sub
    Private Sub FindProccess()
        Dim processes As Process() = Process.GetProcessesByName("SLDWORKS")
        If processes.Count > 1 Then
            Stop
        End If
        If processes.Count <> 0 Then
            Main = processes(0)
            ptr = Main.MainWindowHandle

            'Dim NotepadRect As New Rect
            'GetWindowRect(ptr, NotepadRect)
            'HostResized(NotepadRect)
        End If
    End Sub
    Private Sub Ticker(sender As Object, e As EventArgs)
        Try
            Dim NotepadRect As New Rect
            If Main Is Nothing OrElse Main.HasExited OrElse ptr.ToInt32 = 0 Then
                FindProccess()
            End If



            GetWindowRect(ptr, NotepadRect)
            HostResized(NotepadRect)
            'Me.Dispatcher.Invoke(
            '    New Action(Sub()
            '                   'cfg.ReadFile()
            '                   Try

            '                       Me.Left = (NotepadRect.Right - 50 - Me.Width)
            '                       Me.Top = (NotepadRect.Bottom - 50 - Me.Height)

            '                   Catch
            '                   End Try
            '               End Sub))
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
