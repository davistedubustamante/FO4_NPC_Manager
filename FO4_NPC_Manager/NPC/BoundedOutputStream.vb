Option Strict On
Imports System.IO

Namespace ExperimentalTransformation
    ' Non-owning capability; the underlying file handle and path never reach the serializer.
    Friend NotInheritable Class BoundedOutputStream
        Inherits Stream
        Private ReadOnly output As Stream
        Private ReadOnly ceiling As Long
        Private closed As Boolean
        Public Sub New(stream As Stream, maxBytes As Long)
            output = stream : ceiling = maxBytes
        End Sub
        Private Sub CheckPosition(value As Long)
            If closed Then Throw New ObjectDisposedException(NameOf(BoundedOutputStream))
            If value < 0 OrElse value > ceiling Then Throw New IOException("Reserved stream byte boundary exceeded before writing.")
        End Sub
        Public Overrides ReadOnly Property CanRead As Boolean = False
        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return Not closed
            End Get
        End Property
        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return Not closed
            End Get
        End Property
        Public Overrides ReadOnly Property Length As Long
            Get
                CheckPosition(0) : Return output.Length
            End Get
        End Property
        Public Overrides Property Position As Long
            Get
                CheckPosition(0) : Return output.Position
            End Get
            Set(value As Long)
                CheckPosition(value) : output.Position = value
            End Set
        End Property
        Public Overrides Sub Write(buffer As Byte(), offset As Integer, count As Integer)
            ArgumentNullException.ThrowIfNull(buffer)
            If offset < 0 OrElse count < 0 OrElse offset > buffer.Length - count Then Throw New ArgumentOutOfRangeException(NameOf(count))
            If count > ceiling - Position Then Throw New IOException("Reserved stream byte boundary exceeded before writing.")
            output.Write(buffer, offset, count)
        End Sub
        Public Overrides Sub WriteByte(value As Byte)
            If Position >= ceiling Then Throw New IOException("Reserved stream byte boundary exceeded before writing.")
            output.WriteByte(value)
        End Sub
        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Dim basis As Long
            Select Case origin
                Case SeekOrigin.Begin : basis = 0
                Case SeekOrigin.Current : basis = Position
                Case SeekOrigin.End : basis = Length
                Case Else : Throw New ArgumentOutOfRangeException(NameOf(origin))
            End Select
            ' Avoid arithmetic overflow before checking the requested seek.
            If offset < -basis OrElse offset > ceiling - basis Then Throw New IOException("Reserved stream seek boundary exceeded.")
            Position = basis + offset : Return Position
        End Function
        Public Overrides Sub SetLength(value As Long)
            CheckPosition(value) : output.SetLength(value)
        End Sub
        Public Overrides Sub Flush()
            CheckPosition(0) : output.Flush()
        End Sub
        Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
            Throw New NotSupportedException("Output capability is write-only.")
        End Function
        Protected Overrides Sub Dispose(disposing As Boolean)
            closed = True
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
