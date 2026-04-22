Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Public Class PreviewForm
    Inherits Form

    Private _bitmap As Bitmap
    Private _tempPath As String

    Public Sub New(bmp As Bitmap)
        _bitmap = bmp
        Me.DoubleBuffered = True
        Me.Text = "Screenshot"
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ShowInTaskbar = True
        Me.MinimumSize = New Size(300, 200)

        ' Size window to fit image (capped at 900x650)
        Dim maxW = 900, maxH = 600
        Dim scale = Math.Min(CDbl(maxW) / bmp.Width, CDbl(maxH) / bmp.Height)
        scale = Math.Min(scale, 1.0)
        Dim imgW = Math.Max(300, CInt(bmp.Width * scale))
        Dim imgH = Math.Max(150, CInt(bmp.Height * scale))
        Me.ClientSize = New Size(imgW, imgH + 40)

        BuildUI()
        AutoSaveToTemp()
    End Sub

    Private Sub BuildUI()
        ' Toolbar panel
        Dim panel As New Panel()
        panel.Dock = DockStyle.Top
        panel.Height = 40
        panel.BackColor = Color.FromArgb(45, 45, 48)

        Dim btnCopy As New Button()
        btnCopy.Text = "Copy"
        btnCopy.FlatStyle = FlatStyle.Flat
        btnCopy.ForeColor = Color.White
        btnCopy.BackColor = Color.FromArgb(60, 60, 65)
        btnCopy.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85)
        btnCopy.Size = New Size(80, 28)
        btnCopy.Location = New Point(8, 6)
        AddHandler btnCopy.Click, AddressOf BtnCopy_Click

        Dim btnSave As New Button()
        btnSave.Text = "Save"
        btnSave.FlatStyle = FlatStyle.Flat
        btnSave.ForeColor = Color.White
        btnSave.BackColor = Color.FromArgb(60, 60, 65)
        btnSave.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85)
        btnSave.Size = New Size(80, 28)
        btnSave.Location = New Point(96, 6)
        AddHandler btnSave.Click, AddressOf BtnSave_Click

        Dim btnDrag As New Button()
        btnDrag.Text = "Drag && Drop"
        btnDrag.FlatStyle = FlatStyle.Flat
        btnDrag.ForeColor = Color.White
        btnDrag.BackColor = Color.FromArgb(60, 60, 65)
        btnDrag.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85)
        btnDrag.Size = New Size(100, 28)
        btnDrag.Location = New Point(184, 6)
        AddHandler btnDrag.MouseDown, AddressOf BtnDrag_MouseDown

        Dim btnOpenFolder As New Button()
        btnOpenFolder.Text = "Open Folder"
        btnOpenFolder.FlatStyle = FlatStyle.Flat
        btnOpenFolder.ForeColor = Color.White
        btnOpenFolder.BackColor = Color.FromArgb(60, 60, 65)
        btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85)
        btnOpenFolder.Size = New Size(100, 28)
        btnOpenFolder.Location = New Point(292, 6)
        AddHandler btnOpenFolder.Click, AddressOf BtnOpenFolder_Click

        panel.Controls.Add(btnCopy)
        panel.Controls.Add(btnSave)
        panel.Controls.Add(btnDrag)
        panel.Controls.Add(btnOpenFolder)

        ' Picture box
        Dim pic As New PictureBox()
        pic.Image = _bitmap
        pic.SizeMode = PictureBoxSizeMode.Zoom
        pic.Dock = DockStyle.Fill
        pic.BackColor = Color.FromArgb(30, 30, 30)

        Me.Controls.Add(pic)
        Me.Controls.Add(panel)
    End Sub

    Private Sub AutoSaveToTemp()
        Try
            Dim tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp", "ScreenshotApp")
            Directory.CreateDirectory(tempDir)
            _tempPath = Path.Combine(tempDir, "screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".png")
            _bitmap.Save(_tempPath, Imaging.ImageFormat.Png)
        Catch
            _tempPath = String.Empty
        End Try
    End Sub

    Private Sub BtnCopy_Click(sender As Object, e As EventArgs)
        Dim data As New DataObject()

        ' Image data — for pasting into Paint, Word, Discord, etc.
        Dim bmp24 As New Bitmap(_bitmap.Width, _bitmap.Height, Imaging.PixelFormat.Format24bppRgb)
        Using g = Graphics.FromImage(bmp24)
            g.DrawImage(_bitmap, 0, 0)
        End Using
        data.SetData(DataFormats.Bitmap, True, bmp24)

        ' File path — for Ctrl+V into File Explorer folders
        If Not String.IsNullOrEmpty(_tempPath) AndAlso File.Exists(_tempPath) Then
            Dim files As New System.Collections.Specialized.StringCollection()
            files.Add(_tempPath)
            data.SetFileDropList(files)
        End If

        Clipboard.SetDataObject(data, True)
        Dim btn = CType(sender, Button)
        Dim original = btn.Text
        btn.Text = "Copied!"
        btn.BackColor = Color.FromArgb(40, 120, 40)
        Dim t As New Timer()
        t.Interval = 1500
        AddHandler t.Tick, Sub()
                               btn.Text = original
                               btn.BackColor = Color.FromArgb(60, 60, 65)
                               t.Stop()
                               t.Dispose()
                           End Sub
        t.Start()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            sfd.Title = "Save Screenshot"
            sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg"
            sfd.DefaultExt = "png"
            sfd.FileName = "screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")
            If sfd.ShowDialog() = DialogResult.OK Then
                Dim fmt = If(sfd.FilterIndex = 2, Imaging.ImageFormat.Jpeg, Imaging.ImageFormat.Png)
                _bitmap.Save(sfd.FileName, fmt)
            End If
        End Using
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As EventArgs)
        Dim folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp", "ScreenshotApp")
        If Directory.Exists(folder) Then
            Process.Start("explorer.exe", folder)
        End If
    End Sub

    Private Sub BtnDrag_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        If String.IsNullOrEmpty(_tempPath) OrElse Not File.Exists(_tempPath) Then Return

        Dim data As New DataObject()
        Dim files As New System.Collections.Specialized.StringCollection()
        files.Add(_tempPath)
        data.SetFileDropList(files)

        CType(sender, Button).DoDragDrop(data, DragDropEffects.Copy)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _bitmap?.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
