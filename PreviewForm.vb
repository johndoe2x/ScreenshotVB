Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms

Public Class PreviewForm
    Inherits Form

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
        SelectTool
    End Enum

    ' Arrow object for move support
    Private Class ArrowShape
        Public Pt1 As Point
        Public Pt2 As Point
        Public Color As Color
        Public Size As Integer
    End Class

    Private _arrows As New List(Of ArrowShape)
    Private _selectedArrow As ArrowShape = Nothing
    Private _dragOffsetStart As Point
    Private _dragOffsetEnd As Point

    Private _tool As DrawTool = DrawTool.None
    Private _penColor As Color = Color.Red
    Private _penSize As Integer = 3
    Private _drawing As Boolean = False
    Private _startPt As Point
    Private _lastPt As Point
    Private _startPtPanel As Point
    Private _previewPt As Point

    Private _canvas As DoubleBufferedPanel
    Private _colorSwatch As Panel
    Private _toolBtns As New Dictionary(Of DrawTool, Button)
    Private _btnPin As Button

    Private ReadOnly ACTIVE_CLR As Color = Color.FromArgb(0, 122, 204)
    Private ReadOnly INACTIVE_CLR As Color = Color.FromArgb(58, 58, 58)
    Private ReadOnly TOOLBAR_CLR As Color = Color.FromArgb(28, 28, 28)

    ' Segoe MDL2 Assets icons (built into Windows 10)
    Private Const ICO_COPY   As String = ChrW(&HE8C8)
    Private Const ICO_SAVE   As String = ChrW(&HE74E)
    Private Const ICO_DRAG   As String = ChrW(&HE8A7)
    Private Const ICO_FOLDER As String = ChrW(&HE8B7)
    Private Const ICO_PIN    As String = ChrW(&HE840)
    Private Const ICO_PINNED As String = ChrW(&HE841)
    Private Const ICO_PEN    As String = ChrW(&HE70F)
    Private Const ICO_ARROW  As String = ChrW(&HE8DF)
    Private Const ICO_MOVE   As String = ChrW(&HE7C2)
    Private Const ICO_TEXT   As String = ChrW(&HE8D2)
    Private Const ICO_ERASE  As String = ChrW(&HE74D)
    Private Const ICO_UNDO   As String = ChrW(&HE7A7)

    Public Sub New(bmp As Bitmap)
        _bitmap = bmp
        _annotationLayer = New Bitmap(bmp.Width, bmp.Height, Imaging.PixelFormat.Format32bppArgb)
        Me.DoubleBuffered = True
        Me.Text = "Screenshot"
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ShowInTaskbar = True
        Me.BackColor = Color.FromArgb(20, 20, 20)
        Me.MinimumSize = New Size(780, 200)

        Dim maxW = 1000, maxH = 620
        Dim scale = Math.Min(CDbl(maxW) / bmp.Width, CDbl(maxH) / bmp.Height)
        scale = Math.Min(scale, 1.0)
        Dim imgW = Math.Max(780, CInt(bmp.Width * scale))
        Dim imgH = Math.Max(200, CInt(bmp.Height * scale))
        Me.ClientSize = New Size(imgW, imgH + 52)

        BuildUI()
        AutoSaveToTemp()
    End Sub

    Private Sub BuildUI()
        Dim tips As New ToolTip()
        Dim toolbar As New Panel() With {
            .Dock = DockStyle.Top, .Height = 52, .BackColor = TOOLBAR_CLR
        }
        toolbar.Controls.Add(New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 1, .BackColor = Color.FromArgb(55, 55, 55)
        })

        Dim x = 8

        ' ── Group 1: actions ──────────────────────────────────────────────
        Dim bCopy = TBtn("Copy", x) : tips.SetToolTip(bCopy, "Copy (includes annotations)")
        AddHandler bCopy.Click, AddressOf BtnCopy_Click : x += bCopy.Width + 3

        Dim bSave = TBtn("Save", x) : tips.SetToolTip(bSave, "Save as PNG / JPEG")
        AddHandler bSave.Click, AddressOf BtnSave_Click : x += bSave.Width + 3

        Dim bDrag = IBtn(ICO_DRAG, x) : tips.SetToolTip(bDrag, "Drag & Drop into any app")
        AddHandler bDrag.MouseDown, AddressOf BtnDrag_MouseDown : x += bDrag.Width + 3

        Dim bFolder = TBtn("Folder", x) : tips.SetToolTip(bFolder, "Open screenshots folder")
        AddHandler bFolder.Click, AddressOf BtnOpenFolder_Click : x += bFolder.Width + 8

        toolbar.Controls.Add(Divider(x)) : x += 12

        ' ── Group 2: drawing tools ────────────────────────────────────────
        Dim bPen = IBtn(ICO_PEN, x) : tips.SetToolTip(bPen, "Pen — freehand draw")
        _toolBtns(DrawTool.Pen) = bPen
        AddHandler bPen.Click, Sub(s, e) SetTool(DrawTool.Pen)
        x += bPen.Width + 2

        ' Pen color preset dropdown
        Dim penMenu As New ContextMenuStrip() With {.BackColor = Color.FromArgb(40, 40, 40), .ForeColor = Color.White}
        For Each item In {("● Red", Color.Red), ("● Blue", Color.DodgerBlue), ("● Black", Color.Black), ("● Yellow", Color.Yellow), ("● Green", Color.LimeGreen)}
            Dim c = item.Item2
            Dim mi = CType(penMenu.Items.Add(item.Item1), ToolStripMenuItem)
            mi.ForeColor = c
            AddHandler mi.Click, Sub(s, e) SetPenColor(c)
        Next
        penMenu.Items.Add("-")
        penMenu.Items.Add("Custom color...", Nothing, Sub()
                                                          Using cd As New ColorDialog() With {.Color = _penColor}
                                                              If cd.ShowDialog() = DialogResult.OK Then SetPenColor(cd.Color)
                                                          End Using
                                                      End Sub)
        Dim bDrop = New Button() With {
            .Text = ChrW(&HE70D), .Font = New Font("Segoe MDL2 Assets", 7),
            .FlatStyle = FlatStyle.Flat, .ForeColor = Color.FromArgb(150, 150, 150),
            .BackColor = INACTIVE_CLR, .Size = New Size(16, 38), .Location = New Point(x, 7)
        }
        bDrop.FlatAppearance.BorderColor = Color.FromArgb(65, 65, 65)
        AddHandler bDrop.Click, Sub(s, e) penMenu.Show(bDrop, New Point(0, bDrop.Height))
        x += bDrop.Width + 6

        Dim bArrow = TBtn("Arrow", x) : tips.SetToolTip(bArrow, "Arrow — click & drag")
        _toolBtns(DrawTool.Arrow) = bArrow
        AddHandler bArrow.Click, Sub(s, e) SetTool(DrawTool.Arrow)
        x += bArrow.Width + 3

        Dim bMove = TBtn("Move", x) : tips.SetToolTip(bMove, "Move — drag an arrow to reposition")
        _toolBtns(DrawTool.SelectTool) = bMove
        AddHandler bMove.Click, Sub(s, e) SetTool(DrawTool.SelectTool)
        x += bMove.Width + 3

        Dim bText = TBtn("Text", x) : tips.SetToolTip(bText, "Text — click to place, Enter to confirm")
        _toolBtns(DrawTool.Text) = bText
        AddHandler bText.Click, Sub(s, e) SetTool(DrawTool.Text)
        x += bText.Width + 3

        Dim bErase = IBtn(ICO_ERASE, x) : tips.SetToolTip(bErase, "Eraser")
        _toolBtns(DrawTool.Eraser) = bErase
        AddHandler bErase.Click, Sub(s, e) SetTool(DrawTool.Eraser)
        x += bErase.Width + 8

        toolbar.Controls.Add(Divider(x)) : x += 12

        ' ── Color swatch ─────────────────────────────────────────────────
        _colorSwatch = New Panel() With {
            .Size = New Size(30, 30), .Location = New Point(x, 11),
            .BackColor = Color.Transparent, .Cursor = Cursors.Hand
        }
        tips.SetToolTip(_colorSwatch, "Pick color")
        AddHandler _colorSwatch.Paint, Sub(s, e2)
                                           Dim g2 = e2.Graphics
                                           g2.SmoothingMode = SmoothingMode.AntiAlias
                                           Using br As New SolidBrush(_penColor)
                                               g2.FillEllipse(br, 2, 2, 26, 26)
                                           End Using
                                           Using pn As New Pen(Color.FromArgb(110, 110, 110), 1.5F)
                                               g2.DrawEllipse(pn, 2, 2, 26, 26)
                                           End Using
                                       End Sub
        AddHandler _colorSwatch.Click, Sub(s, e)
                                           Using cd As New ColorDialog() With {.Color = _penColor}
                                               If cd.ShowDialog() = DialogResult.OK Then SetPenColor(cd.Color)
                                           End Using
                                       End Sub
        x += 38

        ' ── Size buttons ─────────────────────────────────────────────────
        Dim bS = SzBtn("S", x, 2) : tips.SetToolTip(bS, "Small") : x += bS.Width + 2
        Dim bM = SzBtn("M", x, 4) : tips.SetToolTip(bM, "Medium") : x += bM.Width + 2
        Dim bL = SzBtn("L", x, 7) : tips.SetToolTip(bL, "Large") : x += bL.Width + 8

        toolbar.Controls.Add(Divider(x)) : x += 12

        ' ── Undo & Pin ───────────────────────────────────────────────────
        Dim bUndo = IBtn(ICO_UNDO, x) : tips.SetToolTip(bUndo, "Undo")
        AddHandler bUndo.Click, AddressOf BtnUndo_Click : x += bUndo.Width + 3

        _btnPin = IBtn(ICO_PIN, x) : tips.SetToolTip(_btnPin, "Pin window on top")
        AddHandler _btnPin.Click, Sub(s, e)
                                      Me.TopMost = Not Me.TopMost
                                      _btnPin.Text = If(Me.TopMost, ICO_PINNED, ICO_PIN)
                                      _btnPin.BackColor = If(Me.TopMost, ACTIVE_CLR, INACTIVE_CLR)
                                  End Sub

        toolbar.Controls.AddRange({bCopy, bSave, bDrag, bFolder, bPen, bDrop, bArrow, bMove, bText, bErase,
                                   _colorSwatch, bS, bM, bL, bUndo, _btnPin})

        ' ── Canvas ────────────────────────────────────────────────────────
        _canvas = New DoubleBufferedPanel() With {.Dock = DockStyle.Fill, .BackColor = Color.FromArgb(22, 22, 22)}
        AddHandler _canvas.Paint, AddressOf Canvas_Paint
        AddHandler _canvas.MouseDown, AddressOf Canvas_MouseDown
        AddHandler _canvas.MouseMove, AddressOf Canvas_MouseMove
        AddHandler _canvas.MouseUp, AddressOf Canvas_MouseUp

        Me.Controls.Add(toolbar)
        Me.Controls.Add(_canvas)
    End Sub

    ' Text label button
    Private Function TBtn(text As String, x As Integer) As Button
        Dim b As New Button()
        b.Text = text
        b.Font = New Font("Segoe UI", 9)
        b.FlatStyle = FlatStyle.Flat
        b.ForeColor = Color.FromArgb(210, 210, 210)
        b.BackColor = INACTIVE_CLR
        b.FlatAppearance.BorderColor = Color.FromArgb(65, 65, 65)
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 72)
        b.Size = New Size(Math.Max(44, text.Length * 8 + 16), 38)
        b.Location = New Point(x, 7)
        b.Cursor = Cursors.Hand
        Return b
    End Function

    ' Icon button using Segoe MDL2 Assets
    Private Function IBtn(icon As String, x As Integer, Optional w As Integer = 38) As Button
        Dim b As New Button()
        b.Text = icon
        b.Font = New Font("Segoe MDL2 Assets", 13)
        b.FlatStyle = FlatStyle.Flat
        b.ForeColor = Color.FromArgb(210, 210, 210)
        b.BackColor = INACTIVE_CLR
        b.FlatAppearance.BorderColor = Color.FromArgb(65, 65, 65)
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 72)
        b.Size = New Size(w, 38)
        b.Location = New Point(x, 7)
        b.Cursor = Cursors.Hand
        Return b
    End Function

    ' Size selector button (plain text, smaller)
    Private Function SzBtn(text As String, x As Integer, sz As Integer) As Button
        Dim b As New Button()
        b.Text = text
        b.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        b.FlatStyle = FlatStyle.Flat
        b.ForeColor = Color.FromArgb(180, 180, 180)
        b.BackColor = INACTIVE_CLR
        b.FlatAppearance.BorderColor = Color.FromArgb(65, 65, 65)
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 72)
        b.Size = New Size(26, 38)
        b.Location = New Point(x, 7)
        b.Cursor = Cursors.Hand
        AddHandler b.Click, Sub(s, e) _penSize = sz
        Return b
    End Function

    Private Function Divider(x As Integer) As Panel
        Return New Panel() With {
            .Size = New Size(1, 32), .Location = New Point(x, 9),
            .BackColor = Color.FromArgb(65, 65, 65)
        }
    End Function

    ' ── Tool & color ───────────────────────────────────────────────────────
    Private Sub SetTool(t As DrawTool)
        _tool = t
        For Each kvp In _toolBtns
            kvp.Value.BackColor = If(kvp.Key = t, ACTIVE_CLR, INACTIVE_CLR)
        Next
        Select Case t
            Case DrawTool.Eraser : _canvas.Cursor = Cursors.Cross
            Case DrawTool.SelectTool : _canvas.Cursor = Cursors.SizeAll
            Case DrawTool.None   : _canvas.Cursor = Cursors.Default
            Case Else            : _canvas.Cursor = Cursors.Cross
        End Select
    End Sub

    Private Sub SetPenColor(c As Color)
        _penColor = c
        _colorSwatch.Invalidate()
        If _tool = DrawTool.None Then SetTool(DrawTool.Pen)
    End Sub

    ' ── Coordinate helpers ─────────────────────────────────────────────────
    Private Function GetImageRect() As Rectangle
        Dim pw = _canvas.ClientSize.Width, ph = _canvas.ClientSize.Height
        Dim s = Math.Min(CDbl(pw) / _bitmap.Width, CDbl(ph) / _bitmap.Height)
        Dim dw = CInt(_bitmap.Width * s), dh = CInt(_bitmap.Height * s)
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

    Private Function ImageToPanel(p As Point) As Point
        Dim r = GetImageRect()
        Dim x = CInt(r.X + CDbl(p.X) * r.Width / _bitmap.Width)
        Dim y = CInt(r.Y + CDbl(p.Y) * r.Height / _bitmap.Height)
        Return New Point(x, y)
    End Function

    ' ── Arrow hit test ─────────────────────────────────────────────────────
    Private Function HitTestArrow(panelPt As Point) As ArrowShape
        Dim r = GetImageRect()
        If r.Width = 0 Then Return Nothing
        Dim scl = CDbl(r.Width) / _bitmap.Width
        For Each a In _arrows
            Dim p1 = ImageToPanel(a.Pt1)
            Dim p2 = ImageToPanel(a.Pt2)
            If DistToSegment(panelPt, p1, p2) < Math.Max(8, a.Size * scl + 4) Then Return a
        Next
        Return Nothing
    End Function

    Private Shared Function DistToSegment(pt As Point, a As Point, b As Point) As Double
        Dim dx = b.X - a.X, dy = b.Y - a.Y
        If dx = 0 AndAlso dy = 0 Then Return Math.Sqrt((pt.X - a.X) ^ 2 + (pt.Y - a.Y) ^ 2)
        Dim t = Math.Max(0, Math.Min(1, ((pt.X - a.X) * dx + (pt.Y - a.Y) * dy) / (dx * dx + dy * dy)))
        Return Math.Sqrt((pt.X - (a.X + t * dx)) ^ 2 + (pt.Y - (a.Y + t * dy)) ^ 2)
    End Function

    ' ── Canvas paint ───────────────────────────────────────────────────────
    Private Sub Canvas_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        Dim r = GetImageRect()
        g.DrawImage(_bitmap, r)
        g.DrawImage(_annotationLayer, r)

        ' Draw arrows
        Dim scl = If(r.Width > 0, CDbl(r.Width) / _bitmap.Width, 1.0)
        g.SmoothingMode = SmoothingMode.AntiAlias
        For Each a In _arrows
            DrawArrowGfx(g, ImageToPanel(a.Pt1), ImageToPanel(a.Pt2), a.Color, CSng(a.Size * scl), a Is _selectedArrow)
        Next

        ' Arrow preview
        If _drawing AndAlso _tool = DrawTool.Arrow Then
            DrawArrowGfx(g, _startPtPanel, _previewPt, _penColor, _penSize, False)
        End If
    End Sub

    Private Sub DrawArrowGfx(g As Graphics, p1 As Point, p2 As Point, c As Color, sz As Single, selected As Boolean)
        If p1 = p2 Then Return
        Using pen As New Pen(c, sz)
            Try
                Dim capSz = Math.Max(4.0F, sz * 2.5F)
                pen.CustomEndCap = New AdjustableArrowCap(capSz, capSz)
            Catch
            End Try
            g.DrawLine(pen, p1, p2)
        End Using
        If selected Then
            Using pen As New Pen(Color.White, 1) With {.DashStyle = DashStyle.Dot}
                g.DrawEllipse(pen, p1.X - 5, p1.Y - 5, 10, 10)
                g.DrawEllipse(pen, p2.X - 5, p2.Y - 5, 10, 10)
            End Using
        End If
    End Sub

    ' ── Canvas mouse events ────────────────────────────────────────────────
    Private Sub Canvas_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse _tool = DrawTool.None Then Return

        _startPt = PanelToImage(e.Location)
        _lastPt = _startPt
        _startPtPanel = e.Location
        _previewPt = e.Location

        Select Case _tool
            Case DrawTool.Text
                ShowTextInput(e.Location)

            Case DrawTool.SelectTool
                _selectedArrow = HitTestArrow(e.Location)
                If _selectedArrow IsNot Nothing Then
                    _drawing = True
                    _dragOffsetStart = New Point(_startPt.X - _selectedArrow.Pt1.X, _startPt.Y - _selectedArrow.Pt1.Y)
                    _dragOffsetEnd = New Point(_startPt.X - _selectedArrow.Pt2.X, _startPt.Y - _selectedArrow.Pt2.Y)
                End If
                _canvas.Invalidate()

            Case DrawTool.Pen, DrawTool.Eraser
                PushUndo()
                _drawing = True

            Case DrawTool.Arrow
                _drawing = True
        End Select
    End Sub

    Private Sub Canvas_MouseMove(sender As Object, e As MouseEventArgs)
        ' Update cursor for Select tool hover
        If _tool = DrawTool.SelectTool AndAlso Not _drawing Then
            _canvas.Cursor = If(HitTestArrow(e.Location) IsNot Nothing, Cursors.SizeAll, Cursors.Default)
        End If

        If Not _drawing Then Return
        _previewPt = e.Location
        Dim imgPt = PanelToImage(e.Location)

        Select Case _tool
            Case DrawTool.Pen
                Using g = Graphics.FromImage(_annotationLayer)
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    Using p As New Pen(_penColor, _penSize)
                        p.StartCap = LineCap.Round : p.EndCap = LineCap.Round
                        g.DrawLine(p, _lastPt, imgPt)
                    End Using
                End Using
                _lastPt = imgPt
                _canvas.Invalidate()

            Case DrawTool.Eraser
                Dim sz = Math.Max(10, _penSize * 8)
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

            Case DrawTool.SelectTool
                If _selectedArrow IsNot Nothing Then
                    _selectedArrow.Pt1 = New Point(imgPt.X - _dragOffsetStart.X, imgPt.Y - _dragOffsetStart.Y)
                    _selectedArrow.Pt2 = New Point(imgPt.X - _dragOffsetEnd.X, imgPt.Y - _dragOffsetEnd.Y)
                    _canvas.Invalidate()
                End If
        End Select
    End Sub

    Private Sub Canvas_MouseUp(sender As Object, e As MouseEventArgs)
        If Not _drawing Then Return
        _drawing = False

        If _tool = DrawTool.Arrow Then
            Dim imgPt = PanelToImage(e.Location)
            If imgPt <> _startPt Then
                PushUndo()
                _arrows.Add(New ArrowShape() With {
                    .Pt1 = _startPt, .Pt2 = imgPt,
                    .Color = _penColor, .Size = _penSize
                })
                _canvas.Invalidate()
            End If
        ElseIf _tool = DrawTool.SelectTool AndAlso _selectedArrow IsNot Nothing Then
            PushUndo()
        End If
    End Sub

    ' ── Text ───────────────────────────────────────────────────────────────
    Private Sub ShowTextInput(panelLoc As Point)
        Dim tb As New TextBox() With {
            .BackColor = Color.FromArgb(40, 40, 42),
            .ForeColor = _penColor,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BorderStyle = BorderStyle.FixedSingle,
            .Size = New Size(200, 32),
            .Location = New Point(Math.Min(panelLoc.X, _canvas.Width - 205), Math.Min(panelLoc.Y - 16, _canvas.Height - 36))
        }
        _canvas.Controls.Add(tb) : tb.BringToFront() : tb.Focus()
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
        RemoveTb(tb) : _canvas.Invalidate()
    End Sub

    Private Sub RemoveTb(tb As TextBox)
        If _canvas.Controls.Contains(tb) Then _canvas.Controls.Remove(tb) : tb.Dispose()
    End Sub

    Private Sub DrawTextOnLayer(text As String, imgPt As Point)
        Using g = Graphics.FromImage(_annotationLayer)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim r = GetImageRect()
            Dim scale = If(r.Width > 0, CDbl(_bitmap.Width) / r.Width, 1.0)
            Dim fontSize = CSng(Math.Max(14, _penSize * 5) * scale)
            Using f As New Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel)
                Using shadow As New SolidBrush(Color.FromArgb(140, 0, 0, 0))
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
        If _arrows.Count > 0 Then
            _arrows.RemoveAt(_arrows.Count - 1)
            _selectedArrow = Nothing
            _canvas.Invalidate()
            Return
        End If
        If _undoStack.Count = 0 Then Return
        _annotationLayer.Dispose()
        _annotationLayer = _undoStack.Pop()
        _canvas.Invalidate()
    End Sub

    ' ── Merge ──────────────────────────────────────────────────────────────
    Private Function GetMergedBitmap() As Bitmap
        Dim merged As New Bitmap(_bitmap.Width, _bitmap.Height, Imaging.PixelFormat.Format32bppArgb)
        Using g = Graphics.FromImage(merged)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.DrawImage(_bitmap, 0, 0)
            g.DrawImage(_annotationLayer, 0, 0)
            ' Bake arrows into merged output
            Dim r = GetImageRect()
            For Each a In _arrows
                DrawArrowGfx(g, a.Pt1, a.Pt2, a.Color, a.Size, False)
            Next
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

    ' ── Action handlers ────────────────────────────────────────────────────
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
        Dim btn = CType(sender, Button), orig = btn.Text
        btn.Text = "Copied!" : btn.BackColor = Color.FromArgb(35, 110, 35)
        Dim t As New Timer() With {.Interval = 1500}
        AddHandler t.Tick, Sub()
                               btn.Text = orig : btn.BackColor = INACTIVE_CLR
                               t.Stop() : t.Dispose()
                           End Sub
        t.Start()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog() With {
            .Title = "Save Screenshot", .Filter = "PNG|*.png|JPEG|*.jpg",
            .DefaultExt = "png", .FileName = "screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")
        }
            If sfd.ShowDialog() = DialogResult.OK Then
                Dim merged = GetMergedBitmap()
                merged.Save(sfd.FileName, If(sfd.FilterIndex = 2, Imaging.ImageFormat.Jpeg, Imaging.ImageFormat.Png))
                merged.Dispose()
            End If
        End Using
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As EventArgs)
        Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "ScreenshotApp")
        If Directory.Exists(folder) Then Process.Start("explorer.exe", folder)
    End Sub

    Private Sub BtnDrag_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse String.IsNullOrEmpty(_tempPath) OrElse Not File.Exists(_tempPath) Then Return
        Dim data As New DataObject()
        Dim files As New System.Collections.Specialized.StringCollection()
        files.Add(_tempPath)
        data.SetFileDropList(files)
        CType(sender, Button).DoDragDrop(data, DragDropEffects.Copy)
    End Sub

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

Friend Class DoubleBufferedPanel
    Inherits Panel
    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
    End Sub
End Class
