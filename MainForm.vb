Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    ' Win32
    <DllImport("user32.dll")>
    Private Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Private Shared Function GetLastError() As UInteger
    End Function

    Private Const WM_HOTKEY As Integer = &H312
    Private Const MOD_CONTROL As UInteger = &H2UI
    Private Const MOD_NOREPEAT As UInteger = &H4000UI
    Private Const HOTKEY_ID As Integer = 9001

    Private _trayIcon As NotifyIcon
    Private _trayMenu As ContextMenuStrip
    Private _capturing As Boolean = False

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.Width = 1
        Me.Height = 1
        Me.Left = -200
        Me.Top = -200
        Me.Opacity = 0
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        SetupTray()
        RegisterCaptureHotkey()
    End Sub

    Private Sub SetupTray()
        _trayMenu = New ContextMenuStrip()
        _trayMenu.Items.Add("Capture  (Ctrl+E)", Nothing, Sub() TriggerCapture())
        _trayMenu.Items.Add(New ToolStripSeparator())
        _trayMenu.Items.Add("Exit", Nothing, Sub() ExitApp())

        _trayIcon = New NotifyIcon()
        _trayIcon.Text = "Screenshotter  —  Ctrl+E to capture"
        _trayIcon.ContextMenuStrip = _trayMenu
        _trayIcon.Visible = True

        Try
            Dim stream = GetType(MainForm).Assembly.GetManifestResourceStream("ScreenshotVB.app.ico")
            If stream IsNot Nothing Then _trayIcon.Icon = New Icon(stream)
        Catch
            _trayIcon.Icon = SystemIcons.Application
        End Try

        AddHandler _trayIcon.DoubleClick, Sub() TriggerCapture()
    End Sub

    Private Sub RegisterCaptureHotkey()
        Dim ok = RegisterHotKey(Me.Handle, HOTKEY_ID, MOD_CONTROL Or MOD_NOREPEAT, CUInt(Keys.E))
        If Not ok Then
            Dim err = GetLastError()
            Dim msg = If(err = 1409,
                "Ctrl+E is already taken by another app. Close it and try again.",
                $"RegisterHotKey failed (error {err}).")
            _trayIcon.ShowBalloonTip(5000, "Screenshotter — Hotkey Error", msg, ToolTipIcon.Warning)
        End If
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_HOTKEY AndAlso m.WParam.ToInt32() = HOTKEY_ID Then
            TriggerCapture()
        End If
        MyBase.WndProc(m)
    End Sub

    Public Sub TriggerCapture()
        If _capturing Then Return
        _capturing = True
        Try
            Using selector As New SelectorForm()
                selector.ShowDialog()
                If selector.SelectedBitmap IsNot Nothing Then
                    Dim preview As New PreviewForm(selector.SelectedBitmap)
                    preview.Show()
                End If
            End Using
        Finally
            _capturing = False
        End Try
    End Sub

    Private Sub ExitApp()
        _trayIcon.Visible = False
        UnregisterHotKey(Me.Handle, HOTKEY_ID)
        Application.Exit()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
        Else
            _trayIcon?.Dispose()
            UnregisterHotKey(Me.Handle, HOTKEY_ID)
            MyBase.OnFormClosing(e)
        End If
    End Sub

End Class
