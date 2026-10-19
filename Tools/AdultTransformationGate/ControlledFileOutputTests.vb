Imports System.IO
Imports System.Linq
Imports System.Text
Imports FO4_NPC_Manager.ExperimentalTransformation
Module ControlledFileOutputTests
    Function Run() As Integer
        Dim root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "controlled-writers-" & Guid.NewGuid().ToString("N"))).FullName
        Dim original = Directory.CreateDirectory(Path.Combine(root, "original")).FullName
        Dim source = Path.Combine(original, "source.bin") : File.WriteAllBytes(source, New Byte() {1, 2, 3})
        Dim count = 0
        Dim check As Action(Of Boolean) = Sub(ok)
                                             If Not ok Then Throw New InvalidDataException("Controlled writer check " & count.ToString())
                                             count += 1
                                         End Sub
        Dim reject As Action(Of Action) = Sub(action)
                                             Dim failed = False
                                             Try
                                                 action()
                                             Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is ArgumentException
                                                 failed = True
                                             End Try
                                             check(failed)
                                         End Sub
        Dim workspace = Path.Combine(root, "workspace")
        Dim stage As String = Nothing
        Dim identity = Encoding.UTF8.GetBytes("fixture-profile")
        Using budget = ReservedWriteBudget.Create(workspace, 16384, {original})
            Using scope = ControlledFileOutput.Begin(budget)
                Dim journal = BatchJournal.Create(workspace, {original}, identity)
                stage = journal.StagingDirectory
                Dim output = Path.Combine(stage, "Data", "one.bin")
                ControlledFileOutput.EnsureDirectory(Path.GetDirectoryName(output))
                check(Not Directory.Exists(Path.GetDirectoryName(output)))
                ControlledFileOutput.CopyNew(source, output)
                check(ControlledFileOutput.IsVerified(output))
                journal.Checkpoint("npc", {Path.GetRelativePath(stage, output)})
                check(journal.CompletedNpcKeys.SequenceEqual({"npc"}))
                reject(Sub() ControlledFileOutput.CopyNew(source, output))
                reject(Sub() ControlledFileOutput.CopyNew(source, Path.Combine(original, "escape.bin")))
                check(Not File.Exists(Path.Combine(original, "escape.bin")))
                Dim interruptedPath = Path.Combine(stage, "interrupted.json")
                reject(Sub() ControlledFileOutput.WriteSerialized(interruptedPath, Sub(stream)
                                                                             stream.WriteByte(1)
                                                                             Throw New IOException("interrupted serializer")
                                                                         End Sub))
                check(Not File.Exists(interruptedPath))
                reject(Sub() ControlledFileOutput.WriteNew(Path.Combine(stage, "oversized.bin"), New Byte(16383) {}))
                check(Not File.Exists(Path.Combine(stage, "oversized.bin")))
            End Using
        End Using
        Using budget = ReservedWriteBudget.Recover(workspace)
            Using scope = ControlledFileOutput.Begin(budget)
                Dim journal = BatchJournal.Recover(stage, identity)
                check(journal.ContainsVerifiedCheckpoint("npc"))
                journal.Quarantine("controlled rollback")
                reject(Sub() journal.Checkpoint("forbidden", {"Data/one.bin"}))
                check(File.ReadAllBytes(Path.Combine(stage, "Data", "one.bin")).SequenceEqual(New Byte() {1, 2, 3}))
            End Using
        End Using
        reject(Sub() ControlledFileOutput.WriteNew(Path.Combine(workspace, "closed.bin"), New Byte() {1}))
        check(File.ReadAllBytes(source).SequenceEqual(New Byte() {1, 2, 3}))
        Return count
    End Function
End Module
