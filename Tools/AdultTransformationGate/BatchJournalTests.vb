Option Strict On
Option Explicit On
Option Infer On
Imports System.IO
Imports System.Text
Imports FO4_NPC_Manager.ExperimentalTransformation

Public Module BatchJournalTests
    Public Function Run() As Integer
        Dim count As Integer = 0
        Dim root = Path.Combine(Path.GetTempPath(), "npc-journal-tests-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(root)
        Dim original = Path.Combine(root, "original")
        Directory.CreateDirectory(original)
        Dim originalFile = Path.Combine(original, "input.esp")
        File.WriteAllText(originalFile, "original fixture")
        Dim profile = Encoding.UTF8.GetBytes("adult-profile-v1")
        Dim journal = BatchJournal.Create(root, {original}, profile)
        Check(Directory.Exists(journal.StagingDirectory), count)
        Check(journal.ProfileSha256 = BatchJournal.ProfileIdentity(profile), count)
        Dim output = Path.Combine(journal.StagingDirectory, "patch.esp")
        File.WriteAllText(output, "synthetic-output")
        journal.Checkpoint("Fallout4.esm|000123", {"patch.esp"})
        Check(journal.ContainsVerifiedCheckpoint("Fallout4.esm|000123"), count)
        Check(Not journal.ContainsVerifiedCheckpoint("missing"), count)
        Check(journal.VerifiedOutputPaths("Fallout4.esm|000123").Single() = output, count)
        Reject(Sub() journal.VerifiedOutputPaths("missing"), count)
        Dim recovered = BatchJournal.Recover(journal.StagingDirectory, profile)
        Check(recovered.CompletedNpcKeys.Length = 1, count)
        Reject(Sub() BatchJournal.Recover(journal.StagingDirectory, Encoding.UTF8.GetBytes("changed")), count)
        Reject(Sub() journal.Checkpoint("Fallout4.esm|000123", {"patch.esp"}), count)
        Reject(Sub() journal.Checkpoint("escape", {"../original/input.esp"}), count)
        Reject(Sub() journal.Checkpoint("absolute", {originalFile}), count)
        Reject(Sub() journal.Checkpoint("empty", Array.Empty(Of String)()), count)
        Reject(Sub() BatchJournal.Create(original, {original}, profile), count)
        File.WriteAllText(output, "tampered")
        Reject(Sub() BatchJournal.Recover(journal.StagingDirectory, profile), count)
        Reject(Sub() recovered.ContainsVerifiedCheckpoint("Fallout4.esm|000123"), count)
        recovered.Quarantine("Hash changed; retain evidence")
        Check(recovered.StateName = "QUARANTINED-NOT-INSTALLED", count)
        Reject(Sub() recovered.Checkpoint("new", {"patch.esp"}), count)
        Reject(Sub() BatchJournal.Recover(recovered.StagingDirectory, profile), count)
        Check(File.Exists(output) AndAlso File.ReadAllText(originalFile) = "original fixture", count)
        Dim linkJournal = BatchJournal.Create(root, {original}, profile)
        Dim link = Path.Combine(linkJournal.StagingDirectory, "linked.esp")
        If OperatingSystem.IsWindows() Then
            ' Junctions need no developer-mode symbolic-link privilege on Windows CI.
            Dim junction = Path.Combine(linkJournal.StagingDirectory, "linked")
            Dim start As New System.Diagnostics.ProcessStartInfo("cmd.exe") With {.UseShellExecute = False, .CreateNoWindow = True}
            start.ArgumentList.Add("/c")
            start.ArgumentList.Add("mklink")
            start.ArgumentList.Add("/J")
            start.ArgumentList.Add(junction)
            start.ArgumentList.Add(original)
            Using process = System.Diagnostics.Process.Start(start)
                process.WaitForExit()
                If process.ExitCode <> 0 Then Throw New Exception("Cannot create Windows junction fixture")
            End Using
            Reject(Sub() linkJournal.Checkpoint("linked", {Path.Combine("linked", "input.esp")}), count)
        Else
            File.CreateSymbolicLink(link, originalFile)
            Reject(Sub() linkJournal.Checkpoint("linked", {"linked.esp"}), count)
        End If
        Dim torn = BatchJournal.Create(root, {original}, profile)
        File.WriteAllText(Path.Combine(torn.StagingDirectory, ".batch-journal", "00000001.json"), "{")
        Reject(Sub() BatchJournal.Recover(torn.StagingDirectory, profile), count)
        Return count
    End Function
    Private Sub Check(condition As Boolean, ByRef count As Integer)
        If Not condition Then Throw New Exception("Batch journal assertion failed")
        count += 1
    End Sub
    Private Sub Reject(action As Action, ByRef count As Integer)
        Try
            action()
        Catch ex As Exception When TypeOf ex Is System.Text.Json.JsonException OrElse TypeOf ex Is InvalidDataException OrElse TypeOf ex Is IOException OrElse TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException
            count += 1
            Return
        End Try
        Throw New Exception("Batch journal accepted unsafe operation")
    End Sub
End Module
