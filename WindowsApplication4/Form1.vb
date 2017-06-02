Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Microsoft.VisualBasic

Public Class Form1

    Dim listen_thread As Thread
    Dim listener As Socket
    Public allDone As New ManualResetEvent(False)
    Dim Thread_listen As Thread
    
    Dim So As New ArrayList
    Dim thread_lock As Object = New Object()
    Dim Time As Timers.Timer
    ' State object for reading client data asynchronously
    Public Class StateObject
        ' Client  socket.
        Public workSocket As Socket = Nothing
        ' Size of receive buffer.
        Public Const BufferSize As Integer = 1024
        ' Receive buffer.
        Public buffer(BufferSize) As Byte
        ' Received data string.
        Public sb As New StringBuilder
        Public size As Int32
    End Class 'StateObject


    Public Sub Time_hande()
        Dim str As String = ""
        SyncLock thread_lock '////////////
            For Each member As Socket In So
                str = str & member.RemoteEndPoint.ToString() & vbNewLine
            Next
        End SyncLock
        TextBox6.Text = str
    End Sub

    Public Sub Accept_callback(ar As IAsyncResult)
        allDone.Set()

        Dim waitDone As New ManualResetEvent(False)
        Dim socketi As Socket

        Try
            socketi = listener.EndAccept(ar)
        Catch ie As Exception
            Exit Sub
        End Try

        TextBox3.AppendText(vbNewLine & "加入一个" & socketi.RemoteEndPoint.ToString)
        TextBox3.ScrollToCaret()
        Dim State As New StateObject()
        State.workSocket = socketi
        SyncLock thread_lock '/////////////////////
            So.Add(socketi)
        End SyncLock
        State.size = 0
        Dim err As Int32 = 0
        While 1
            Try
                err = socketi.Receive(State.buffer)
            Catch e As Exception
                TextBox3.AppendText(vbNewLine & "客户端终止")
                TextBox3.ScrollToCaret()
                socketi.Close()
                SyncLock thread_lock
                    So.Remove(socketi)
                End SyncLock
                Exit Sub
            End Try
            If err > 0 Then
                State.sb.Clear()
                State.sb.Append(Encoding.ASCII.GetString(State.buffer, 0, err))
                Dim str As String
                str = State.sb.ToString()
                TextBox1.AppendText(vbNewLine & str)
                TextBox1.ScrollToCaret()
            End If
        End While
    End Sub

    Public Sub listen_handle()
        TextBox3.AppendText(vbNewLine & "服务器启动")
        TextBox3.ScrollToCaret()
        While 1

            allDone.Reset()
            Try
                listener.BeginAccept(New AsyncCallback(AddressOf Accept_callback), listener)
            Catch e As Exception
                Exit Sub
            End Try
            allDone.WaitOne()
        End While
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Dim listenr As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Dim localEndpoint As IPEndPoint
        Dim Ip As String
        Dim Port As String

        Ip = TextBox4.Text()
        If Ip = "" Then
            MsgBox("please ensure your ip")
            Return
        End If
        Port = TextBox5.Text()
        If Port = "" Then
            MsgBox("please ensure your port")
            Return
        End If

        localEndpoint = New IPEndPoint(IPAddress.Parse(Ip), Port)
        listener = listenr
        listenr.Bind(localEndpoint)
        listenr.Listen(1024)
        listener = listenr
        Dim Th As Thread
        Th = New Thread(AddressOf listen_handle)
        Thread_listen = Th
        Th.Start()
        Time = New Timers.Timer(500)
        AddHandler Time.Elapsed, AddressOf Time_hande
        Time.Start()

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim str As String = TextBox2.Text
        If str = "" Then
            MsgBox("please input data")
            Return
        End If
        Dim encText As New System.Text.UTF8Encoding()
        Dim btText() As Byte
        btText = encText.GetBytes(str)
        SyncLock thread_lock
            For Each member As Socket In So
                Try
                    member.Send(btText)
                Catch pe As Exception
                    TextBox1.AppendText("send failed" & member.RemoteEndPoint.ToString())
                End Try
            Next
        End SyncLock
        TextBox1.AppendText(vbNewLine & str)
        TextBox1.ScrollToCaret()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Try
            Thread_listen.Abort()
        Catch ex As Exception

        End Try
        listener.Close()
        SyncLock thread_lock
            For Each member As Socket In So
                Try
                    member.Close()
                Catch ie As Exception
                End Try

            Next
        End SyncLock
        Time.Stop()
        TextBox3.AppendText(vbNewLine & "服务器关闭")
        TextBox3.ScrollToCaret()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox4.Text = "127.0.0.1"
        TextBox5.Text = "8888"
    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Try
            Thread_listen.Abort()
        Catch ex As Exception

        End Try
        listener.Close()
        For Each member As Socket In So
            Try
                member.Close()
            Catch ie As Exception
            End Try

        Next
        Try
            Time.Stop()
        Catch fe As Exception
        End Try
    End Sub
End Class
