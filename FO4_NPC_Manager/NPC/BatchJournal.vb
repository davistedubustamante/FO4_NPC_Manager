Option Strict On
Option Explicit On
Option Infer On
Imports System.IO
Imports System.Linq
Imports System.Collections.Generic
Imports System.Security.Cryptography
Imports System.Text.Json

Namespace ExperimentalTransformation
    Public Class BatchOutput
        Public Property RelativePath As String = ""
        Public Property Sha256 As String = ""
    End Class

    Public Class BatchCheckpoint
        Public Property NpcKey As String = ""
        Public Property Outputs As New List(Of BatchOutput)
    End Class

    Public Class BatchJournalState
        Public Property FormatVersion As Integer = 1
        Public Property ProfileSha256 As String = ""
        Public Property Status As String = "EXPERIMENTAL-STAGING"
        Public Property QuarantineReason As String = ""
        Public Property ProtectedRoots As New List(Of String)
        Public Property Checkpoints As New List(Of BatchCheckpoint)
    End Class

    ' A staging journal, never an installer. Callers retain their original inputs.
    Public NotInheritable Class BatchJournal
        Private ReadOnly root As String
        Private ReadOnly journalRoot As String
        Private state As BatchJournalState
        Private sequence As Integer
        Private Shared ReadOnly pathComparison As StringComparison = If(OperatingSystem.IsWindows(), StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)
        Public ReadOnly Property StagingDirectory As String
            Get
                Return root
            End Get
        End Property
        Public ReadOnly Property CompletedNpcKeys As String()
            Get
                Return state.Checkpoints.Select(Function(c) c.NpcKey).ToArray()
            End Get
        End Property
        Public ReadOnly Property ProfileSha256 As String
            Get
                Return state.ProfileSha256
            End Get
        End Property
        Public ReadOnly Property StateName As String
            Get
                Return state.Status
            End Get
        End Property
        Public Function ContainsVerifiedCheckpoint(npcKey As String) As Boolean
            Dim entry = state.Checkpoints.FirstOrDefault(Function(c) c.NpcKey = npcKey)
            If entry Is Nothing OrElse state.Status <> "EXPERIMENTAL-STAGING" Then Return False
            For Each output In entry.Outputs
                If HashOutput(output.RelativePath) <> output.Sha256 Then Throw New InvalidDataException("Staged output hash mismatch.")
            Next
            Return True
        End Function
        Private Sub New(stagingPath As String, value As BatchJournalState, index As Integer)
            root = stagingPath
            journalRoot = Path.Combine(root, ".batch-journal")
            state = value
            sequence = index
        End Sub
        Public Shared Function ProfileIdentity(profileBytes As Byte()) As String
            If profileBytes Is Nothing OrElse profileBytes.Length = 0 Then Throw New ArgumentException("Profile bytes are required.")
            Return Convert.ToHexString(SHA256.HashData(profileBytes))
        End Function
        Public Shared Function Create(stagingParent As String, protectedRoots As IEnumerable(Of String), profileBytes As Byte()) As BatchJournal
            Dim identity = ProfileIdentity(profileBytes)
            Dim parent = Path.GetFullPath(stagingParent)
            CheckAncestors(parent)
            If Not Directory.Exists(parent) Then Throw New DirectoryNotFoundException(parent)
            Dim protectedPaths = protectedRoots.Select(Function(p) Path.GetFullPath(p)).ToList()
            If protectedPaths.Count = 0 Then Throw New ArgumentException("Explicit protectedPath/game/mod/save roots are required.")
            Dim stagingPath = Path.Combine(parent, "npc-experimental-" & Guid.NewGuid().ToString("N"))
            ValidateOutside(stagingPath, protectedPaths)
            If Directory.Exists(stagingPath) OrElse File.Exists(stagingPath) Then Throw New IOException("Staging already exists.")
            Directory.CreateDirectory(stagingPath)
            CheckAncestors(stagingPath)
            Dim value As New BatchJournalState With {.ProfileSha256 = identity, .ProtectedRoots = protectedPaths}
            Dim result As New BatchJournal(stagingPath, value, -1)
            Directory.CreateDirectory(result.journalRoot)
            result.Save(value)
            Return result
        End Function
        Public Shared Function Recover(stagingDirectory As String, profileBytes As Byte()) As BatchJournal
            Dim stagingPath = Path.GetFullPath(stagingDirectory)
            CheckAncestors(stagingPath)
            Dim journals = Path.Combine(stagingPath, ".batch-journal")
            CheckAncestors(journals)
            Dim files = Directory.GetFiles(journals, "*.json").OrderBy(Function(p) p, StringComparer.Ordinal).ToArray()
            If files.Length = 0 Then Throw New InvalidDataException("No durable entry exists.")
            Dim value As BatchJournalState = Nothing
            For i As Integer = 0 To files.Length - 1
                If Path.GetFileName(files(i)) <> i.ToString("D8") & ".json" Then Throw New InvalidDataException("Journal sequence is incomplete.")
                CheckAncestors(files(i))
                value = JsonSerializer.Deserialize(Of BatchJournalState)(File.ReadAllText(files(i)))
                If value Is Nothing OrElse value.FormatVersion <> 1 OrElse value.ProfileSha256 <> ProfileIdentity(profileBytes) Then Throw New InvalidDataException("Profile identity or journal format mismatch.")
                If value.ProtectedRoots Is Nothing OrElse value.ProtectedRoots.Count = 0 Then Throw New InvalidDataException("Missing protectedPaths roots.")
                ValidateOutside(stagingPath, value.ProtectedRoots)
            Next
            If value.Status <> "EXPERIMENTAL-STAGING" Then Throw New InvalidDataException("Batch is quarantined or has an unknown state.")
            Dim result As New BatchJournal(stagingPath, value, files.Length - 1)
            Dim keys As New HashSet(Of String)(StringComparer.Ordinal)
            For Each entry In value.Checkpoints
                If String.IsNullOrWhiteSpace(entry.NpcKey) OrElse Not keys.Add(entry.NpcKey) Then Throw New InvalidDataException("Duplicate or empty NPC entry.")
                For Each output In entry.Outputs
                    If result.HashOutput(output.RelativePath) <> output.Sha256 Then Throw New InvalidDataException("Staged output hash mismatch: " & output.RelativePath)
                Next
            Next
            Return result
        End Function
        Public Sub Checkpoint(npcKey As String, relativeOutputPaths As IEnumerable(Of String))
            If state.Status <> "EXPERIMENTAL-STAGING" Then Throw New InvalidOperationException("Batch is quarantined.")
            If String.IsNullOrWhiteSpace(npcKey) OrElse state.Checkpoints.Any(Function(c) c.NpcKey = npcKey) Then Throw New ArgumentException("NPC key must be unique and nonempty.")
            Dim entry As New BatchCheckpoint With {.NpcKey = npcKey}
            For Each relative In relativeOutputPaths
                entry.Outputs.Add(New BatchOutput With {.RelativePath = relative, .Sha256 = HashOutput(relative)})
            Next
            If entry.Outputs.Count = 0 Then Throw New ArgumentException("A entry requires durable output.")
            Dim nextState = CloneState()
            nextState.Checkpoints.Add(entry)
            Save(nextState)
        End Sub
        ' Logical rollback retains all staged files for diagnosis. It never touches protectedPath roots.
        Public Sub Quarantine(reason As String)
            If String.IsNullOrWhiteSpace(reason) Then Throw New ArgumentException("A rollback reason is required.")
            Dim nextState = CloneState()
            nextState.Status = "QUARANTINED-NOT-INSTALLED"
            nextState.QuarantineReason = reason
            Save(nextState)
        End Sub
        Private Function CloneState() As BatchJournalState
            Return JsonSerializer.Deserialize(Of BatchJournalState)(JsonSerializer.Serialize(state))
        End Function
        Private Sub Save(nextState As BatchJournalState)
            CheckAncestors(root)
            CheckAncestors(journalRoot)
            ValidateOutside(root, nextState.ProtectedRoots)
            Dim destination = Path.Combine(journalRoot, (sequence + 1).ToString("D8") & ".json")
            ' CreateNew forbids overwrites/concurrent writers. A torn snapshot fails closed on resume.
            Using stream As New FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                JsonSerializer.Serialize(stream, nextState)
                stream.Flush(True)
            End Using
            sequence += 1
            state = nextState
        End Sub
        Private Function HashOutput(relative As String) As String
            If String.IsNullOrWhiteSpace(relative) OrElse Path.IsPathRooted(relative) Then Throw New InvalidDataException("Output must be relative.")
            Dim full = Path.GetFullPath(Path.Combine(root, relative))
            If Not IsWithin(full, root) OrElse full.Equals(root, pathComparison) OrElse IsWithin(full, journalRoot) Then Throw New InvalidDataException("Output escapes staging or enters journal.")
            CheckAncestors(full)
            Using stream As New FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)
                Return Convert.ToHexString(SHA256.HashData(stream))
            End Using
        End Function
        Private Shared Sub ValidateOutside(stagingPath As String, protectedPaths As IEnumerable(Of String))
            For Each protectedPath In protectedPaths
                If IsWithin(stagingPath, protectedPath) OrElse IsWithin(protectedPath, stagingPath) Then Throw New InvalidDataException("Staging overlaps a protectedPaths root.")
                CheckAncestors(protectedPath)
            Next
        End Sub
        Private Shared Function IsWithin(candidate As String, parent As String) As Boolean
            parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent))
            candidate = Path.GetFullPath(candidate)
            Return candidate.Equals(parent, pathComparison) OrElse candidate.StartsWith(If(Path.EndsInDirectorySeparator(parent), parent, parent & Path.DirectorySeparatorChar), pathComparison)
        End Function
        Private Shared Sub CheckAncestors(value As String)
            Dim current = Path.GetFullPath(value)
            Do
                If (File.Exists(current) OrElse Directory.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New InvalidDataException("Reparse points are forbidden: " & current)
                current = Path.GetDirectoryName(current)
            Loop While Not String.IsNullOrEmpty(current)
        End Sub
    End Class
End Namespace
