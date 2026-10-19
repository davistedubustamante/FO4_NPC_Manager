Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json
Imports System.Security.Cryptography

Namespace ExperimentalTransformation
    ' Hard pre-write byte ceiling ONLY for files created through this gateway.
    ' This is NOT an OS quota: do not use it to admit the existing path-based ESP/FaceGen writers.
    Public NotInheritable Class ReservedWriteBudget
        Implements IDisposable
        Private Class Entry
            Public Property Kind As String = ""
            Public Property RelativePath As String = ""
            Public Property Length As Long
            Public Property Sha256 As String = ""
            Public Property Previous As String = ""
        End Class
        Private ReadOnly ledger As FileStream
        Private ReadOnly reservations As New List(Of Entry)
        Private ReadOnly root As String
        Private ceiling As Long
        Private reserved As Long
        Private previous As String = ""
        Private faulted As Boolean
        Public ReadOnly Property DirectoryPath As String
            Get
                Return root
            End Get
        End Property
        Public ReadOnly Property RemainingBytes As Long
            Get
                Return ceiling - reserved - ledger.Length
            End Get
        End Property
        Private pausedState As Boolean
        Public ReadOnly Property Paused As Boolean
            Get
                Return pausedState
            End Get
        End Property
        Public ReadOnly Property ReservedNames As String()
            Get
                Return reservations.Select(Function(e) e.RelativePath).ToArray()
            End Get
        End Property
        Public ReadOnly Property VerifiedNames As String()
            Get
                Return reservations.Where(Function(e) Verify(e)).Select(Function(e) e.RelativePath).ToArray()
            End Get
        End Property
        Public Shared Function Create(directory As String, budgetBytes As Long, protectedRoots As IEnumerable(Of String)) As ReservedWriteBudget
            If budgetBytes < 4096 OrElse budgetBytes > 1024L * 1024 * 1024 Then Throw New ArgumentOutOfRangeException(NameOf(budgetBytes))
            Dim full = Path.GetFullPath(directory)
            CheckPath(full)
            For Each protectedRoot In protectedRoots
                Dim original = Path.TrimEndingDirectorySeparator(Path.GetFullPath(protectedRoot))
                If full.Equals(original, StringComparison.OrdinalIgnoreCase) OrElse full.StartsWith(If(Path.EndsInDirectorySeparator(original), original, original & Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) Then Throw New IOException("Budget workspace must be outside original roots.")
            Next
            If IO.Directory.Exists(full) OrElse File.Exists(full) Then Throw New IOException("Budget workspace must be new.")
            IO.Directory.CreateDirectory(full)
            Dim workspace As New ReservedWriteBudget(full, FileMode.CreateNew)
            Try
                workspace.ceiling = budgetBytes
                workspace.Append(New Entry With {.Kind = "header", .Length = budgetBytes})
                Return workspace
            Catch
                workspace.Dispose() : Throw
            End Try
        End Function
        Public Shared Function Recover(directory As String) As ReservedWriteBudget
            Dim workspace As New ReservedWriteBudget(Path.GetFullPath(directory), FileMode.Open)
            Try
                If workspace.ledger.Length > 1024 * 1024 Then Throw New InvalidDataException("Budget ledger exceeds recovery limit.")
                Dim bytes(CInt(workspace.ledger.Length) - 1) As Byte
                workspace.ledger.ReadExactly(bytes)
                If bytes.Length = 0 OrElse bytes.Last() <> 10 Then Throw New InvalidDataException("Interrupted ledger reservation; fail closed and retain workspace.")
                Dim lines = Encoding.UTF8.GetString(bytes).Split(ChrW(10), StringSplitOptions.RemoveEmptyEntries)
                For Each line In lines
                    If line.Length < 66 OrElse line(64) <> ":"c OrElse Digest(Encoding.UTF8.GetBytes(line.Substring(65))) <> line.Substring(0, 64) Then Throw New InvalidDataException("Corrupt budget reservation checksum.")
                    Dim entry = JsonSerializer.Deserialize(Of Entry)(line.Substring(65))
                    If entry Is Nothing OrElse entry.Previous <> workspace.previous Then Throw New InvalidDataException("Broken budget ledger chain.")
                    If entry.Kind = "header" Then
                        If workspace.ceiling <> 0 OrElse entry.Length < 4096 OrElse entry.Length > 1024L * 1024 * 1024 Then Throw New InvalidDataException("Invalid budget header.")
                        workspace.ceiling = entry.Length
                    ElseIf entry.Kind = "reserve" Then
                        workspace.ValidateName(entry.RelativePath)
                        If entry.Length < 0 OrElse entry.Length > 32L * 1024 * 1024 OrElse entry.Sha256.Length <> 64 OrElse workspace.reservations.Any(Function(e) e.RelativePath.Equals(entry.RelativePath, StringComparison.OrdinalIgnoreCase)) Then Throw New InvalidDataException("Invalid or duplicate reservation.")
                        workspace.reservations.Add(entry) : workspace.reserved += entry.Length
                    ElseIf entry.Kind = "pause" Then
                        workspace.pausedState = True
                    ElseIf entry.Kind = "resume" Then
                        workspace.pausedState = False
                    Else
                        Throw New InvalidDataException("Unknown budget event.")
                    End If
                    workspace.previous = Digest(Encoding.UTF8.GetBytes(line & ChrW(10)))
                Next
                If workspace.ceiling = 0 OrElse workspace.RemainingBytes < 0 Then Throw New InvalidDataException("Budget ledger exceeds immutable ceiling.")
                workspace.ledger.Position = workspace.ledger.Length
                Return workspace
            Catch
                workspace.Dispose() : Throw
            End Try
        End Function
        Private Sub New(directory As String, mode As FileMode)
            root = directory
            CheckPath(root)
            CheckPath(Path.Combine(root, "budget-ledger.jsonl"))
            ledger = New FileStream(Path.Combine(root, "budget-ledger.jsonl"), mode, FileAccess.ReadWrite, FileShare.None)
        End Sub
        Public Sub SetPaused(value As Boolean)
            EnsureHealthy()
            If value = Paused Then Return
            Append(New Entry With {.Kind = If(value, "pause", "resume")})
            pausedState = value
        End Sub
        Public Sub WriteNew(relativePath As String, bytes As Byte())
            EnsureHealthy()
            If Paused Then Throw New InvalidOperationException("Workspace is paused.")
            ValidateName(relativePath)
            If bytes Is Nothing OrElse bytes.LongLength > 32L * 1024 * 1024 Then Throw New IOException("Controlled output exceeds 32 MiB.")
            If reservations.Count >= 2000 OrElse reservations.Any(Function(e) e.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase)) Then Throw New IOException("Reservation already exists or count budget exceeded.")
            Dim destination = Path.Combine(root, relativePath)
            CheckPath(destination)
            If File.Exists(destination) OrElse IO.Directory.Exists(destination) Then Throw New IOException("Controlled output must be new.")
            Dim entry As New Entry With {.Kind = "reserve", .RelativePath = relativePath, .Length = bytes.LongLength, .Sha256 = Digest(bytes)}
            ' Persist AND flush the full reservation before creating any payload file. Never refund a failed write.
            Append(entry, bytes.LongLength)
            reservations.Add(entry) : reserved += bytes.LongLength
            IO.Directory.CreateDirectory(Path.GetDirectoryName(destination))
            Using stream As New FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                stream.Write(bytes) : stream.Flush(True)
            End Using
        End Sub
        Private Sub Append(entry As Entry, Optional payload As Long = 0)
            EnsureHealthy()
            entry.Previous = previous
            Dim json = JsonSerializer.Serialize(entry)
            Dim bytes = Encoding.UTF8.GetBytes(Digest(Encoding.UTF8.GetBytes(json)) & ":" & json & ChrW(10))
            If bytes.LongLength + payload > RemainingBytes Then Throw New IOException("Hard controlled-write budget exhausted before writing; reservation and payload unchanged.")
            Try
                ledger.Write(bytes) : ledger.Flush(True)
                previous = Digest(bytes)
            Catch
                faulted = True : Throw
            End Try
        End Sub
        Private Function Verify(entry As Entry) As Boolean
            Dim destination = Path.Combine(root, entry.RelativePath)
            CheckPath(destination)
            If Not File.Exists(destination) OrElse New FileInfo(destination).Length <> entry.Length Then Return False
            Using stream = File.OpenRead(destination)
                Return Convert.ToHexString(SHA256.HashData(stream)) = entry.Sha256
            End Using
        End Function
        Private Sub ValidateName(name As String)
            If String.IsNullOrWhiteSpace(name) OrElse name.Length > 180 OrElse Path.IsPathRooted(name) OrElse name.Contains("\") OrElse name.Contains(":") OrElse name.Split("/"c).Any(Function(part) part = "" OrElse part = "." OrElse part = "..") OrElse name = "budget-ledger.jsonl" Then Throw New ArgumentException("Safe relative controlled output name required.")
            If Not System.Text.RegularExpressions.Regex.IsMatch(name, "\A[A-Za-z0-9_./-]+\z") Then Throw New ArgumentException("Unsupported output characters.")
        End Sub
        Private Sub EnsureHealthy()
            If faulted Then Throw New InvalidOperationException("Interrupted reservation; workspace requires recovery.")
        End Sub
        Private Shared Function Digest(bytes As Byte()) As String
            Return Convert.ToHexString(SHA256.HashData(bytes))
        End Function
        Private Shared Sub CheckPath(value As String)
            Dim current = Path.GetFullPath(value)
            While Not String.IsNullOrEmpty(current)
                If (File.Exists(current) OrElse IO.Directory.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse budget path rejected.")
                current = Path.GetDirectoryName(current)
            End While
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            ledger.Dispose()
        End Sub
    End Class
End Namespace
