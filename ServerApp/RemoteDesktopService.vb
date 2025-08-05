Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.ServiceProcess
Imports System.Threading
Imports System.Windows.Forms
Imports System.Text

Public Class RemoteDesktopService
    Inherits ServiceBase

    Private listener As TcpListener
    Private running As Boolean = True
    Private ipAddress As String
    Private port As Integer

    Protected Overrides Sub OnStart(ByVal args() As String)
        ' Baca konfigurasi IP dan port dari file
        Dim config As String() = File.ReadAllLines("config.txt")
        ipAddress = config(0)
        port = Integer.Parse(config(1))

        listener = New TcpListener(Net.IPAddress.Parse(ipAddress), port)
        listener.Start()
        Dim t As New Thread(AddressOf ListenForClients)
        t.Start()
    End Sub

    Private Sub ListenForClients()
        While running
            Dim client As TcpClient = listener.AcceptTcpClient()
            Dim t As New Thread(AddressOf HandleClient)
            t.Start(client)
        End While
    End Sub

    Private Sub HandleClient(ByVal obj As Object)
        Dim client As TcpClient = CType(obj, TcpClient)
        Dim stream As NetworkStream = client.GetStream()
        Dim controlThread As New Thread(Sub() HandleControlEvents(stream))
        controlThread.Start()
        While running
            ' Capture screen
            Dim bmp As New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
            Dim g As Graphics = Graphics.FromImage(bmp)
            g.CopyFromScreen(0, 0, 0, 0, bmp.Size)
            Dim ms As New MemoryStream()
            bmp.Save(ms, Imaging.ImageFormat.Jpeg)
            Dim data As Byte() = ms.ToArray()
            ' Kirim data gambar ke client
            stream.Write(BitConverter.GetBytes(data.Length), 0, 4)
            stream.Write(data, 0, data.Length)
            Thread.Sleep(100) ' 10 fps
        End While
        client.Close()
    End Sub

    Private Sub HandleControlEvents(stream As NetworkStream)
        Dim reader As New StreamReader(stream)
        Dim writer As New StreamWriter(stream) With {.AutoFlush = True}
        While running
            Dim line As String = reader.ReadLine()
            If String.IsNullOrEmpty(line) Then Continue While
            Dim parts As String() = line.Split("|")
            Select Case CStr(parts(0))
                Case "MOUSEDOWN"
                    SimulateMouseDown(CInt(parts(1)), CInt(parts(2)), parts(3))
                Case "MOUSEMOVE"
                    SimulateMouseMove(CInt(parts(1)), CInt(parts(2)))
                Case "MOUSEUP"
                    SimulateMouseUp(CInt(parts(1)), CInt(parts(2)), parts(3))
                Case "KEYDOWN"
                    SimulateKeyDown(parts(1))
                Case "KEYUP"
                    SimulateKeyUp(parts(1))
                Case "CLIPBOARD"
                    Dim clipboardText As String = line.Substring(10).Replace("<PIPE>", "|")
                    SetClipboardText(clipboardText)
                Case "REQUEST_CLIPBOARD"
                    Dim text As String = GetClipboardText()
                    writer.WriteLine($"CLIPBOARDDATA|{text.Replace("|", "<PIPE>")}")
            End Select
        End While
    End Sub

    Private Sub SetClipboardText(text As String)
        Try
            Clipboard.SetText(text)
        Catch ex As Exception
            ' Handle error
        End Try
    End Sub

    Private Function GetClipboardText() As String
        Try
            If Clipboard.ContainsText() Then
                Return Clipboard.GetText()
            End If
        Catch ex As Exception
        End Try
        Return String.Empty
    End Function

    ' Fungsi simulasi input (gunakan library tambahan seperti InputSimulator atau WinAPI)
    Private Sub SimulateMouseDown(x As Integer, y As Integer, button As String)
        ' TODO: Implementasi simulasi mouse down
    End Sub
    Private Sub SimulateMouseMove(x As Integer, y As Integer)
        ' TODO: Implementasi simulasi mouse move
    End Sub
    Private Sub SimulateMouseUp(x As Integer, y As Integer, button As String)
        ' TODO: Implementasi simulasi mouse up
    End Sub
    Private Sub SimulateKeyDown(keyCode As String)
        ' TODO: Implementasi simulasi key down
    End Sub
    Private Sub SimulateKeyUp(keyCode As String)
        ' TODO: Implementasi simulasi key up
    End Sub

    Public Sub Main()
    End Sub

    Protected Overrides Sub OnStop()
        running = False
        listener.Stop()
    End Sub
End Class
