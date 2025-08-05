Imports System.Net.Sockets
Imports System.IO
Imports System.Drawing

Public Class MainForm
    Private client As TcpClient
    Private stream As NetworkStream

    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        client = New TcpClient(txtIP.Text, Integer.Parse(txtPort.Text))
        stream = client.GetStream()
        Dim t As New Threading.Thread(AddressOf ReceiveScreen)
        t.Start()
    End Sub

    Private Sub ReceiveScreen()
        Dim reader As New StreamReader(stream)
        While True
            ' Cek apakah ada data clipboard dari server
            If stream.DataAvailable Then
                Dim peek = stream.ReadByte()
                If peek = AscW("C") Then ' CLIPBOARDDATA|...
                    Dim line = reader.ReadLine()
                    If line.StartsWith("CLIPBOARDDATA|") Then
                        Dim clipboardText = line.Substring(14).Replace("<PIPE>", "|")
                        If Not String.IsNullOrEmpty(clipboardText) Then
                            Clipboard.SetText(clipboardText)
                        End If
                    End If
                Else
                    Dim lenBytes(3) As Byte
                    lenBytes(0) = CByte(peek)
                    stream.Read(lenBytes, 1, 3)
                    Dim len = BitConverter.ToInt32(lenBytes, 0)
                    Dim imgBytes(len - 1) As Byte
                    Dim read = 0
                    While read < len
                        read += stream.Read(imgBytes, read, len - read)
                    End While
                    Dim ms As New MemoryStream(imgBytes)
                    Dim img = Image.FromStream(ms)
                    PictureBox1.Invoke(Sub() PictureBox1.Image = img)
                End If
            End If
            Threading.Thread.Sleep(10)
        End While
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler PictureBox1.MouseDown, AddressOf PictureBox1_MouseDown
        AddHandler PictureBox1.MouseMove, AddressOf PictureBox1_MouseMove
        AddHandler PictureBox1.MouseUp, AddressOf PictureBox1_MouseUp
        AddHandler Me.KeyDown, AddressOf MainForm_KeyDown
        AddHandler Me.KeyUp, AddressOf MainForm_KeyUp
        Me.KeyPreview = True
    End Sub

    Private Sub PictureBox1_MouseDown(sender As Object, e As MouseEventArgs)
        SendControlEvent($"MOUSEDOWN|{e.X}|{e.Y}|{e.Button}")
    End Sub

    Private Sub PictureBox1_MouseMove(sender As Object, e As MouseEventArgs)
        SendControlEvent($"MOUSEMOVE|{e.X}|{e.Y}")
    End Sub

    Private Sub PictureBox1_MouseUp(sender As Object, e As MouseEventArgs)
        SendControlEvent($"MOUSEUP|{e.X}|{e.Y}|{e.Button}")
    End Sub

    Private Sub MainForm_KeyDown(sender As Object, e As KeyEventArgs)
        SendControlEvent($"KEYDOWN|{e.KeyCode}")
    End Sub

    Private Sub MainForm_KeyUp(sender As Object, e As KeyEventArgs)
        SendControlEvent($"KEYUP|{e.KeyCode}")
    End Sub

    Private Sub SendControlEvent(data As String)
        If stream IsNot Nothing AndAlso stream.CanWrite Then
            Dim bytes = System.Text.Encoding.UTF8.GetBytes(data & vbCrLf)
            stream.Write(bytes, 0, bytes.Length)
        End If
    End Sub

    Private Sub btnCopyClipboard_Click(sender As Object, e As EventArgs) Handles btnCopyClipboard.Click
        If Clipboard.ContainsText() Then
            Dim text = Clipboard.GetText()
            SendControlEvent($"CLIPBOARD|{text.Replace("|", "<PIPE>")}")
        End If
    End Sub

    Private Sub btnPasteClipboard_Click(sender As Object, e As EventArgs) Handles btnPasteClipboard.Click
        SendControlEvent("REQUEST_CLIPBOARD")
    End Sub
End Class
