Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms

Public Class PreviewForm
    Inherits Form

    ' ── core state ─────────────────────────────────────────────────────────
    Private _bitmap As Bitmap
    Private _annotationLayer As Bitmap
    Private _tempPath As String
    Private _undoStack As New Stack(Of Bitmap)

    Private Enum DrawTool
        None
        Pen
        Arrow
        Text
        Eraser
    End Enum

    Private _tool As DrawTool = DrawTool.None
    Private _penColor As Color = Color.Red
    Private _penSize As Integer = 3
    Private _drawing As Boolean = False
    Private _startPt As Point      ' image coords
    Private _lastPt As Point       ' image coords
    Private _startPtPanel As Point ' panel coords (arrow live preview)
    Private _previewPt As Point    ' panel coords (arrow live preview)

    ' ── UI refs ────────────────────────────────────────────────────────────
    Private _canvas As DoubleBufferedPanel
    Private _btnPin As Button
    Private _colorSwatch As Panel
    Private _toolBtns As New Dictionary(Of DrawTool, Button)

    Private ReadOnly ACTIVE_CLR As Color = Color.FromArgb(0, 120, 215)
    Private ReadOnly INACTIVE_CLR As Color = Color.FromArgb(60, 60, 65)

    ' ── constructor ────────────────────────────────────────────────────────
    Public Sub New(bmp As Bitmap)
        _bitmap = bmp
        _annotationLayer = New Bitmap(bmp.Width, bmp.Height, Imaging.PixelFormat.Format32bppArgb)
        Me.DoubleBuffered = True
        Me.Text = "Screenshot"
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ShowInTaskbar = True
        Me.MinimumSize = New Size(500, 320)

        Dim maxW = 900, maxH = 540
        Dim scale = Math.Min(CDbl(maxW) / bmp.Width, CDbl(maxH) / bmp.Height)
        scale = Math.Min(scale, 1.0)
        Dim imgW = Math.Max(500, CInt(bmp.Width * scale))
        Dim imgH = Math.Max(160, CInt(bmp.Height * scale))
        Me.ClientSize = New Size(imgW, imgH + 80)

        BuildUI()
        AutoSaveToTemp()
    End Sub

    ' ── UI build ───────────────────────────────────────────────────────────
    Private Sub BuildUI()
        ' ── Row 1: action buttons ──────────────────────────────────────────
        Dim row1 As New Panel() With {.Dock = DockStyle.Top, .Height = 40, .BackColor = Color.FromArgb(45, 45, 48)}

        Dim bCopy = Btn("Copy", 8) : AddHandler bCopy.Click, AddressOf BtnCopy_Click
        Dim bSave = Btn("Save", 96) : AddHandler bSave.Click, AddressOf BtnSave_Click
        Dim bDrag = Btn("Drag && Drop", 184, 100) : AddHandler bDrag.MouseDown, AddressOf BtnDrag_MouseDown
        Dim bFolder = Btn("Open Folder", 292, 100) : AddHandler bFolder.Click, AddressOf BtnOpenFolder_Click

        _btnPin = Btn("Pin", 400, 58)
        AddHandler _btnPin.Click, Sub(s, e)
                                      Me.TopMost = Not Me.TopMost
                                      _btnPin.Text = If(Me.TopMost, "Pinned", "Pin")
                                      _btnPin.BackColor = If(Me.TopMost, ACTIVE_CLR, INACTIVE_CLR)
                                  End Sub

        row1.Controls.AddRange({bCopy, bSave, bDrag, bFolder, _btnPin})

        ' ── Row 2: drawing tools ───────────────────────────────────────────
        Dim row2 As New Panel() With {.Dock = DockStyle.Top, .Height = 40, .BackColor = Color.FromArgb(35, 35, 38)}

        ' Pen button
        Dim bPen = Btn("Pen", 8, 52)
        _toolBtns(DrawTool.Pen) = bPen
        AddHandler bPen.Click, Sub(s, e) SetTool(DrawTool.Pen)

        ' Pen color preset dropdown arrow
        Dim penMenu As New ContextMenuStrip()
        penMenu.Items.Add("Red",    Nothing, Sub() SetPenColor(Color.Red))
        penMenu.Items.Add("Blue",   Nothing, Sub() SetPenColor(Color.DodgerBlue))
        penMenu.Items.Add("Black",  Nothing, Sub() SetPenColor(Color.Black))
        penMenu.Items.Add("Yellow", Nothing, Sub() SetPenColor(Color.Yellow))
        penMenu.Items.Add("Green",  Nothing, Sub() SetPenColor(Color.LimeGreen))
        penMenu.Items.Add("-")
        penMenu.Items.Add("Custom...", Nothing, Sub()
                                                    Using cd As New ColorDialog() With {.Color = _penColor}
                                                        If cd.ShowDialog() = DialogResult.OK Then SetPenColor(cd.Color)
                                                    End Using
                                                End Sub)
        Dim bDrop = Btn("v", 62, 16)
        bDrop.Font = New Font("Segoe UI", 7)
        AddHandler bDrop.Click, Sub(s, e) penMenu.Show(bDrop, New Point(0, bDrop.Height))

        ' Arrow
        Dim bArrow = Btn("Arrow", 86, 58)
        _toolBtns(DrawTool.Arrow) = bArrow
        AddHandler bArrow.Click, Sub(s, e) SetTool(DrawTool.Arrow)

        ' Text
        Dim bText = Btn("Text", 152, 52)
        _toolBtns(DrawTool.Text) = bText
        AddHandler bText.Click, Sub(s, e) SetTool(DrawTool.Text)

        ' Eraser
        Dim bErase = Btn("Erase", 212, 58)
        _toolBtns(DrawTool.Eraser) = bErase
        AddHandler bErase.Click, Sub(s, e) SetTool(DrawTool.Eraser)

        ' Color swatch
        _colorSwatch = New Panel() With {
            .Size = New Size(26, 26),
            .Location = New Point(280, 7),
            .BackColor = _penColor,
            .BorderStyle = BorderStyle.FixedSingle,
            .Cursor = Cursors.Hand
        }
        AddHandler _colorSwatch.Click, Sub(s, e)
                                           Using cd As New ColorDialog() With {.Color = _penColor}
                                               If cd.ShowDialog() = DialogResult.OK Then SetPenColor(cd.Color)
                                           End Using
                                       End Sub

        ' Size S / M / L
        Dim bS = Btn("S", 315, 22) : AddHandler bS.Click, Sub(s, e) _penSize = 2
        Dim bM = Btn("M", 339, 22) : AddHandler bM.Click, Sub(s, e) _penSize = 4
        Dim bL = Btn("L", 363, 22) : AddHandler bL.Click, Sub(s, e) _penSize = 7

        ' Undo
        Dim bUndo = Btn("Undo", 395, 58)
        AddHandler bUndo.Click, AddressOf BtnUndo_Click

        row2.Controls.AddRange({bPen, bDrop, bArrow, bText, bErase, _colorSwatch, bS, bM, bL, bUndo})

        ' ── Canvas ────────────────────────────────────────────────────────
        _canvas = New DoubleBufferedPanel() With {.Dock = DockStyle.Fill, .BackColor = Color.FromArgb(30, 30, 30)}
        AddHandler _canvas.Paint, AddressOf Canvas_Paint
        AddHandler _canvas.MouseDown, AddressOf Canvas_MouseDown
        AddHandler _canvas.MouseMove, AddressOf Canvas_MouseMove
        AddHandler _canvas.MouseUp, AddressOf Canvas_MouseUp

        Me.Controls.Add(_canvas)
        Me.Controls.Add(row2)
        Me.Controls.Add(row1)
    End Sub

    Private Function Btn(text As String, x As Integer, Optional w As Integer = 80) As Button
        Dim b As New Button()
        b.Text = text
        b.FlatStyle = FlatStyle.Flat
        b.ForeColor = Color.White
        b.BackColor = INACTIVE_CLR
        b.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85)
        b.Size = New Size(w, 28)
        b.Location = New Point(x, 6)
        Return b
    End Function

    ' ── Tool helpers ───────────────────────────────────────────────────────
    Private Sub SetTool(t As DrawTool)
        _tool = t
        For Each kvp In _toolBtns
            kvp.Value.BackColor = If(kvp.Key = t, ACTIVE_CLR, INACTIVE_CLR)
        Next
        _canvas.Cursor = If(t = DrawTool.None, Cursors.Default, Cursors.Cross)
    End Sub

    Private Sub SetPenColor(c As Color)
        _penColor = c
        _colorSwatch.BackColor = c
        If _tool = DrawTool.None Then SetTool(DrawTool.Pen)
    End Sub

    ' ── Coordinate helpers ─────────────────────────────────────────────────
    Private Function GetImageRect() As Rectangle
        Dim pw = _canvas.ClientSize.Width
        Dim ph = _canvas.ClientSize.Height
        Dim s = Math.Min(CDbl(pw) / _bitmap.Width, CDbl(ph) / _bitmap.Height)
        Dim dw = CInt(_bitmap.Width * s)
        Dim dh = CInt(_bitmap.Height * s)
        Return New Rectangle((pw - dw) \ 2, (ph - dh) \ 2, dw, dh)
    End Function

    Private Function PanelToImage(p As Point) As Point
        Dim r = GetImageRect()
        If r.Width = 0 OrElse r.Height = 0 Then Return p
        Dim x = CInt(CDbl(_bitmap.Width) * (p.X - r.X) / r.Width)
        Dim y = CInt(CDbl(_bitmap.Height) * (p.Y - r.Y) / r.Height)
        Return New Point(Math.Max(0, Math.Min(_bitmap.Width - 1, x)),
                         Math.Max(0, Math.Min(_bitmap.Height - 1, y)))
    End Function

    ' ── Canvas events ──────────────────────────────────────────────────────
    Private Sub Canvas_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        Dim r = GetImageRect()
        g.DrawImage(_bitmap, r)
        g.DrawImage(_annotationLayer, r)

        ' Live arrow preview while dragging
        If _drawing AndAlso _tool = DrawTool.Arrow Then
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using p As New Pen(_penColor, _penSize)
                Try
                    Dim capSz = Math.Max(4.0F, _penSize * 2.0F)
                    p.CustomEndCap = New AdjustableArrowCap(capSz, capSz)
                Catch
                End Try
                g.DrawLine(p, _startPtPanel, _previewPt)
            End Using
        End If
    End Sub

    Private Sub Canvas_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse _tool = DrawTool.None Then Return

        _startPt = PanelToImage(e.Location)
        _lastPt = _startPt
        _startPtPanel = e.Location
        _previewPt = e.Location

        If _tool = DrawTool.Text Then
            ShowTextInput(e.Location)
            Return
        End If

        _drawing = True
        If _tool = DrawTool.Pen OrElse _tool = DrawTool.Eraser Then PushUndo()
    End Sub

    Private Sub Canvas_MouseMove(sender As Object, e As MouseEventArgs)
        If Not _drawing Then Return
        _previewPt = e.Location
        Dim imgPt = PanelToImage(e.Location)

        Select Case _tool
            Case DrawTool.Pen
                Using g = Graphics.FromImage(_annotationLayer)
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    Using p As New Pen(_penColor, _penSize)
                        p.StartCap = LineCap.Round
                        p.EndCap = LineCap.Round
                        g.DrawLine(p, _lastPt, imgPt)
                    End Using
                End Using
                _lastPt = imgPt
                _canvas.Invalidate()

            Case DrawTool.Eraser
                Dim sz = Math.Max(8, _penSize * 8)
                Using g = Graphics.FromImage(_annotationLayer)
                    g.CompositingMode = CompositingMode.SourceCopy
                    Using b As New SolidBrush(Color.FromArgb(0, 0, 0, 0))
                        g.FillEllipse(b, imgPt.X - sz \ 2, imgPt.Y - sz \ 2, sz, sz)
                    End Using
                End Using
                _lastPt = imgPt
                _canvas.Invalidate()

            Case DrawTool.Arrow
                _canvas.Invalidate()
        End Select
    End Sub

    Private Sub Canvas_MouseUp(sender As Object, e As MouseEventArgs)
        If Not _drawing Then Return
        _drawing = False

        If _tool = DrawTool.Arrow Then
            Dim imgPt = PanelToImage(e.Location)
            If imgPt <> _startPt Then
                PushUndo()
                DrawArrowOnLayer(_startPt, imgPt)
            End If
            _canvas.Invalidate()
        End If
    End Sub

    ' ── Drawing on annotation layer ────────────────────────────────────────
    Private Sub DrawArrowOnLayer(from As Point, [to] As Point)
        Using g = Graphics.FromImage(_annotationLayer)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using p As New Pen(_penColor, _penSize)
                Try
                    Dim capSz = Math.Max(4.0F, _penSize * 2.5F)
                    p.CustomEndCap = New AdjustableArrowCap(capSz, capSz)
                Catch
                End Try
                g.DrawLine(p, from, [to])
            End Using
        End Using
    End Sub

    Private Sub ShowTextInput(panelLoc As Point)
        Dim tb As New TextBox() With {
            .BackColor = Color.FromArgb(50, 50, 55),
            .ForeColor = _penColor,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BorderStyle = BorderStyle.FixedSingle,
            .Size = New Size(200, 30),
            .Location = New Point(
                Math.Min(panelLoc.X, _canvas.Width - 205),
                Math.Min(panelLoc.Y - 15, _canvas.Height - 35))
        }
        _canvas.Controls.Add(tb)
        tb.BringToFront()
        tb.Focus()

        AddHandler tb.KeyDown, Sub(s, e2)
                                   If e2.KeyCode = Keys.Enter AndAlso Not e2.Shift Then
                                       e2.SuppressKeyPress = True
                                       CommitText(tb, panelLoc)
                                   ElseIf e2.KeyCode = Keys.Escape Then
                                       RemoveTb(tb)
                                   End If
                               End Sub
        AddHandler tb.LostFocus, Sub(s, e2)
                                     If _canvas.Controls.Contains(tb) Then CommitText(tb, panelLoc)
                                 End Sub
    End Sub

    Private Sub CommitText(tb As TextBox, panelLoc As Point)
        If Not String.IsNullOrWhiteSpace(tb.Text) Then
            PushUndo()
            DrawTextOnLayer(tb.Text, PanelToImage(panelLoc))
        End If
        RemoveTb(tb)
        _canvas.Invalidate()
    End Sub

    Private Sub RemoveTb(tb As TextBox)
        If _canvas.Controls.Contains(tb) Then
            _canvas.Controls.Remove(tb)
            tb.Dispose()
        End If
    End Sub

    Private Sub DrawTextOnLayer(text As String, imgPt As Point)
        Using g = Graphics.FromImage(_annotationLayer)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim r = GetImageRect()
            Dim scale = If(r.Width > 0, CDbl(_bitmap.Width) / r.Width, 1.0)
            Dim fontSize = CSng(Math.Max(14, _penSize * 5) * scale)
            Using f As New Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel)
                Using shadow As New SolidBrush(Color.FromArgb(160, 0, 0, 0))
                    g.DrawString(text, f, shadow, imgPt.X + 2, imgPt.Y + 2)
                End Using
                Using fill As New SolidBrush(_penColor)
                    g.DrawString(text, f, fill, imgPt.X, imgPt.Y)
                End Using
            End Using
        End Using
    End Sub

    ' ── Undo ───────────────────────────────────────────────────────────────
    Private Sub PushUndo()
        Dim copy As New Bitmap(_annotationLayer.Width, _annotationLayer.Height, Imaging.PixelFormat.Format32bppArgb)
        Using g = Graphics.FromImage(copy)
            g.DrawImage(_annotationLayer, 0, 0)
        End Using
        _undoStack.Push(copy)
    End Sub

    Private Sub BtnUndo_Click(sender As Object, e As EventArgs)
        If _undoStack.Count = 0 Then Return
        _annotationLayer.Dispose()
        _annotationLayer = _undoStack.Pop()
        _canvas.Invalidate()
    End Sub

    ' ── Merge screenshot + annotations ─────────────────────────────────────
    Private Function GetMergedBitmap() As Bitmap
        Dim merged As New Bitmap(_bitmap.Width, _bitmap.Height, Imaging.PixelFormat.Format32bppArgb)
        Using g = Graphics.FromImage(merged)
            g.DrawImage(_bitmap, 0, 0)
            g.DrawImage(_annotationLayer, 0, 0)
        End Using
        Return merged
    End Function

    ' ── Auto-save ──────────────────────────────────────────────────────────
    Private Sub AutoSaveToTemp()
        Try
            Dim dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "ScreenshotApp")
            Directory.CreateDirectory(dir)
            _tempPath = Path.Combine(dir, "screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".png")
            _bitmap.Save(_tempPath, Imaging.ImageFormat.Png)
        Catch
            _tempPath = String.Empty
        End Try
    End Sub

    ' ── Button handlers ────────────────────────────────────────────────────
    Private Sub BtnCopy_Click(sender As Object, e As EventArgs)
        Dim merged = GetMergedBitmap()
        Dim data As New DataObject()
        Dim bmp24 As New Bitmap(merged.Width, merged.Height, Imaging.PixelFormat.Format24bppRgb)
        Using g = Graphics.FromImage(bmp24)
            g.DrawImage(merged, 0, 0)
        End Using
        merged.Dispose()
        data.SetData(DataFormats.Bitmap, True, bmp24)
        If Not String.IsNullOrEmpty(_tempPath) AndAlso File.Exists(_tempPath) Then
            Dim files As New System.Collections.Specialized.StringCollection()
            files.Add(_tempPath)
            data.SetFileDropList(files)
        End If
        Clipboard.SetDataObject(data, True)
        Dim btn = CType(sender, Button)
        Dim orig = btn.Text
        btn.Text = "Copied!" : btn.BackColor = Color.FromArgb(40, 120, 40)
        Dim t As New Timer() With {.Interval = 1500}
        AddHandler t.Tick, Sub()
                               btn.Text = orig : btn.BackColor = INACTIVE_CLR
                               t.Stop() : t.Dispose()
                           End Sub
        t.Start()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog() With {
            .Title = "Save Screenshot",
            .Filter = "PNG Image|*.png|JPEG Image|*.jpg",
            .DefaultExt = "png",
            .FileName = "screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")
        }
            If sfd.ShowDialog() = DialogResult.OK Then
                Dim merged = GetMergedBitmap()
                Dim fmt = If(sfd.FilterIndex = 2, Imaging.ImageFormat.Jpeg, Imaging.ImageFormat.Png)
                merged.Save(sfd.FileName, fmt)
                merged.Dispose()
            End If
        End Using
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As EventArgs)
        Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "ScreenshotApp")
        If Directory.Exists(folder) Then Process.Start("explorer.exe", folder)
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

    ' ── Dispose ────────────────────────────────────────────────────────────
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _bitmap?.Dispose()
            _annotationLayer?.Dispose()
            Do While _undoStack.Count > 0
                _undoStack.Pop().Dispose()
            Loop
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class

' ── Double-buffered panel to prevent flicker ───────────────────────────────
Friend Class DoubleBufferedPanel
    Inherits Panel
    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or
                    ControlStyles.UserPaint, True)
    End Sub
End Class
