Imports System.Drawing
Imports System.Windows.Forms

Public Class SelectorForm
    Inherits Form

    Private _startPoint As Point
    Private _currentPoint As Point
    Private _isSelecting As Boolean = False
    Private _screenBmp As Bitmap

    Public SelectedBitmap As Bitmap = Nothing

    Public Sub New()
        Me.DoubleBuffered = True
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.Cursor = Cursors.Cross
        Me.KeyPreview = True
        Me.ShowInTaskbar = False
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Dim bounds = SystemInformation.VirtualScreen
        Me.Left = bounds.Left
        Me.Top = bounds.Top
        Me.Width = bounds.Width
        Me.Height = bounds.Height

        ' Capture the desktop before overlay appears
        _screenBmp = New Bitmap(bounds.Width, bounds.Height, Imaging.PixelFormat.Format32bppArgb)
        Using g = Graphics.FromImage(_screenBmp)
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size)
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        ' Draw desktop as background
        e.Graphics.DrawImage(_screenBmp, 0, 0)

        ' Dark overlay over entire screen
        Using overlay As New SolidBrush(Color.FromArgb(110, 0, 0, 0))
            e.Graphics.FillRectangle(overlay, Me.ClientRectangle)
        End Using

        If _isSelecting Then
            Dim rect = GetSelectionRect()
            If rect.Width > 0 AndAlso rect.Height > 0 Then
                ' Show selected region at full brightness (clear the dark overlay)
                e.Graphics.DrawImage(_screenBmp, rect, rect, GraphicsUnit.Pixel)
                ' Blue selection border
                Using pen As New Pen(Color.CornflowerBlue, 2)
                    e.Graphics.DrawRectangle(pen, rect)
                End Using
            End If
        End If
    End Sub

    Private Function GetSelectionRect() As Rectangle
        Dim x = Math.Min(_startPoint.X, _currentPoint.X)
        Dim y = Math.Min(_startPoint.Y, _currentPoint.Y)
        Dim w = Math.Abs(_currentPoint.X - _startPoint.X)
        Dim h = Math.Abs(_currentPoint.Y - _startPoint.Y)
        Return New Rectangle(x, y, w, h)
    End Function

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            _startPoint = e.Location
            _currentPoint = e.Location
            _isSelecting = True
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If _isSelecting Then
            _currentPoint = e.Location
            Me.Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        If _isSelecting AndAlso e.Button = MouseButtons.Left Then
            _isSelecting = False
            _currentPoint = e.Location
            Dim rect = GetSelectionRect()

            If rect.Width > 5 AndAlso rect.Height > 5 Then
                SelectedBitmap = New Bitmap(rect.Width, rect.Height)
                Using g = Graphics.FromImage(SelectedBitmap)
                    g.DrawImage(_screenBmp, New Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel)
                End Using
            End If
            Me.Close()
        End If
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _screenBmp?.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
