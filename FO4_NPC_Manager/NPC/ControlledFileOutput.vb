Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography

Namespace ExperimentalTransformation
    ' Dedicated headless process. No ambient fallback after a controlled operation has closed.
    Public Module ControlledFileOutput
        Private active As ReservedWriteBudget
        Private closed As Boolean
        Private ReadOnly gate As New Object
        Public ReadOnly Property IsActive As Boolean
            Get
                Return active IsNot Nothing
            End Get
        End Property
        Public Function Begin(budget As ReservedWriteBudget) As IDisposable
            SyncLock gate
                ArgumentNullException.ThrowIfNull(budget)
                If active IsNot Nothing Then Throw New InvalidOperationException("Controlled writer already active.")
                active = budget : closed = False
                Return New Scope()
            End SyncLock
        End Function
        Private Class Scope
            Implements IDisposable
            Private done As Boolean
            Public Sub Dispose() Implements IDisposable.Dispose
                SyncLock gate
                    If done Then Return
                    done = True : active = Nothing : closed = True
                End SyncLock
            End Sub
        End Class
        Public Function ToBudgetRelativePath(path As String) As String
            If active Is Nothing Then Throw New InvalidOperationException("No controlled workspace.")
            Dim value = IO.Path.GetRelativePath(active.DirectoryPath, IO.Path.GetFullPath(path))
            If IO.Path.IsPathRooted(value) OrElse value = ".." OrElse value.StartsWith(".." & IO.Path.DirectorySeparatorChar, StringComparison.Ordinal) Then Throw New IOException("Controlled destination escapes workspace.")
            Return value.Replace(IO.Path.DirectorySeparatorChar, "/"c)
        End Function
        Public Function EnsureDirectory(path As String) As String
            SyncLock gate
                If active IsNot Nothing Then
                    ToBudgetRelativePath(path) ' Gateway creates parents only after reservation.
                Else
                    If closed Then Throw New InvalidOperationException("Controlled output closed.")
                    Directory.CreateDirectory(path)
                End If
                Return IO.Path.GetFullPath(path)
            End SyncLock
        End Function
        Public Sub WriteNew(path As String, bytes As Byte())
            SyncLock gate
                If active IsNot Nothing Then
                    active.WriteNew(ToBudgetRelativePath(path), bytes)
                Else
                    If closed Then Throw New InvalidOperationException("Controlled output closed.")
                    Using output As New FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                        output.Write(bytes) : output.Flush(True)
                    End Using
                End If
            End SyncLock
        End Sub
        Public Sub WriteSerialized(path As String, writer As Action(Of Stream))
            SyncLock gate
                If active Is Nothing Then
                    If closed Then Throw New InvalidOperationException("Controlled output closed.")
                    Using output As New FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                        writer(output) : output.Flush(True)
                    End Using
                    Return
                End If
                ToBudgetRelativePath(path)
                Using memory As New MemoryStream()
                    Using bounded As New BoundedOutputStream(memory, 32L * 1024 * 1024)
                        writer(bounded)
                    End Using
                    active.WriteNew(ToBudgetRelativePath(path), memory.ToArray())
                End Using
            End SyncLock
        End Sub
        Public Sub CopyNew(source As String, destination As String)
            SyncLock gate
                If active Is Nothing Then
                    If closed Then Throw New InvalidOperationException("Controlled output closed.")
                    File.Copy(source, destination, False) : Return
                End If
                Dim name = ToBudgetRelativePath(destination)
                Using input As New FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read)
                    If input.Length > 32L * 1024 * 1024 Then Throw New IOException("Controlled resource exceeds 32 MiB.")
                    Dim hash = Convert.ToHexString(SHA256.HashData(input)) : input.Position = 0
                    active.WriteStream(name, input.Length, hash, Sub(output) input.CopyTo(output))
                End Using
            End SyncLock
        End Sub
        Public Function IsVerified(path As String) As Boolean
            If active Is Nothing Then Return Not closed
            Return active.VerifiedNames.Contains(ToBudgetRelativePath(path))
        End Function
    End Module
End Namespace
