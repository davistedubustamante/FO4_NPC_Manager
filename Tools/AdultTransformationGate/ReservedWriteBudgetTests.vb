Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Security.Cryptography
Imports FO4_NPC_Manager.ExperimentalTransformation
Module ReservedWriteBudgetTests
    Function Run() As Integer
        Dim root = Path.Combine(Path.GetTempPath(), "controlled-quota-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(root)
        Dim checks As Integer = 0
        Dim check As Action(Of Boolean, String) = Sub(ok, name)
                                                     If Not ok Then Throw New InvalidDataException("FAIL: " & name)
                                                     checks += 1
                                                 End Sub
        Dim reject As Action(Of Action, String) = Sub(action, name)
                                                      Dim failed As Boolean = False
                                                      Try
                                                          action()
                                                      Catch ex As Exception When TypeOf ex Is InvalidDataException OrElse TypeOf ex Is IOException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is ArgumentException
                                                          failed = True
                                                      End Try
                                                      check(failed, name)
                                                  End Sub
        Dim stage = Path.Combine(root, "stage")
        Dim original = Directory.CreateDirectory(Path.Combine(root, "original")).FullName
        File.WriteAllText(Path.Combine(original, "game.txt"), "unchanged")
        reject(Sub() ReservedWriteBudget.Create(Path.Combine(original, "nested"), 8192, {original}), "protected output rejected")
        Using workspace = ReservedWriteBudget.Create(stage, 8192, {original})
            workspace.WriteNew("page-0000.json", Encoding.UTF8.GetBytes("first verified page"))
            check(workspace.VerifiedNames.SequenceEqual({"page-0000.json"}), "new controlled output verifies")
            reject(Sub() ReservedWriteBudget.Recover(stage), "exclusive lock prevents concurrent budget writers")
            Dim before = workspace.RemainingBytes
            reject(Sub() workspace.WriteNew("exhaustion.bin", New Byte(8191) {}), "quota exhaustion occurs before reservation or payload write")
            check(workspace.RemainingBytes = before AndAlso Not File.Exists(Path.Combine(stage, "exhaustion.bin")), "exhaustion writes zero payload bytes")
            reject(Sub() workspace.WriteNew("../original/game.txt", New Byte() {1}), "traversal rejected before writing original")
            reject(Sub() workspace.WriteNew("page-0000.json", New Byte() {1}), "verified output cannot be overwritten")
            workspace.SetPaused(True)
            reject(Sub() workspace.WriteNew("paused.json", New Byte() {1}), "paused state forbids writes")
        End Using
        Using workspace = ReservedWriteBudget.Recover(stage)
            check(workspace.Paused AndAlso workspace.VerifiedNames.Length = 1, "pause and checkpoints survive restart")
            workspace.SetPaused(False)
            workspace.WriteNew("page-0001.json", New Byte() {1, 2, 3})
            check(workspace.VerifiedNames.Length = 2, "resumed workspace continues sequential outputs")
        End Using
        File.WriteAllBytes(Path.Combine(stage, "page-0001.json"), New Byte() {1})
        Using workspace = ReservedWriteBudget.Recover(stage)
            check(workspace.ReservedNames.Length = 2 AndAlso workspace.VerifiedNames.Length = 1, "partial interrupted payload retains charge and is never verified")
            reject(Sub() workspace.WriteNew("page-0001.json", New Byte() {1, 2, 3}), "interrupted attempt retained without overwriting")
            workspace.WriteNew("page-0001-attempt2.json", New Byte() {1, 2, 3})
            check(workspace.VerifiedNames.Length = 2, "new attempt retains partial attempt")
            Dim fileBytes = Directory.EnumerateFiles(stage, "*", SearchOption.AllDirectories).Sum(Function(p) New FileInfo(p).Length)
            check(fileBytes <= 8192 AndAlso workspace.RemainingBytes >= 0, "ledger plus controlled files under byte ceiling")
        End Using
        Dim streamed = Path.Combine(root, "streams")
        Dim payload = New Byte() {1, 2, 3, 4}
        Dim hash = Convert.ToHexString(SHA256.HashData(payload))
        Using workspace = ReservedWriteBudget.Create(streamed, 16384, {original})
            workspace.WriteStream("Data/one.esp", 4, hash, Sub(output) output.Write(payload))
            check(workspace.VerifiedNames.Contains("Data/one.esp"), "streamed output checkpoint follows hash and flush")
            reject(Sub() workspace.WriteStream("Data/overrun.dds", 4, hash, Sub(output) output.Write(New Byte(4) {})), "stream overrun prevented before first payload byte")
            check(New FileInfo(Path.Combine(streamed, "Data/overrun.dds")).Length = 0, "oversized stream writes zero bytes")
            reject(Sub() workspace.WriteStream("temp/extend.nif", 4, hash, Sub(output) output.SetLength(5)), "SetLength cannot bypass quota")
            reject(Sub() workspace.WriteStream("cache/seek.bin", 4, hash, Sub(output) output.Seek(Long.MaxValue, SeekOrigin.Begin)), "seek cannot bypass quota or overflow")
            reject(Sub() workspace.WriteStream("staging/partial.nif", 4, hash, Sub(output)
                                                                               output.WriteByte(1)
                                                                               Throw New IOException("interrupted")
                                                                           End Sub), "interrupted stream propagates failure")
            reject(Sub() workspace.WriteStream("packages/full-but-interrupted.bin", 4, hash, Sub(output)
                                                                                               output.Write(payload)
                                                                                               Throw New IOException("interrupted after payload")
                                                                                           End Sub), "exception after complete bytes is not success")
            check(workspace.VerifiedNames.Length = 1, "failed streams never checkpoint even with matching full bytes")
            reject(Sub() workspace.WriteStream("resources/wrong-hash.bin", 4, New String("0"c, 64), Sub(output) output.Write(payload)), "wrong streamed hash cannot checkpoint")
            Dim remaining = workspace.RemainingBytes
            reject(Sub() workspace.WriteStream("temp/exhausted.bin", 16384, hash, Sub(output) output.Write(payload)), "aggregate quota rejects streaming reservation before creating file")
            check(workspace.RemainingBytes = remaining AndAlso Not File.Exists(Path.Combine(streamed, "temp/exhausted.bin")), "failed reservation consumes no payload or ledger")
        End Using
        Using workspace = ReservedWriteBudget.Recover(streamed)
            check(workspace.VerifiedNames.SequenceEqual({"Data/one.esp"}) AndAlso workspace.ReservedNames.Length = 7, "stream completions and charged partial attempts recover independently")
            workspace.WriteStream("staging/retry.nif", 4, hash, Sub(output) output.Write(payload))
            check(workspace.VerifiedNames.Length = 2, "recovery retries in a fresh reserved stream")
            check(Directory.EnumerateFiles(streamed, "*", SearchOption.AllDirectories).Sum(Function(p) New FileInfo(p).Length) <= 16384, "all controlled stream categories and ledger fit ceiling")
        End Using
        Dim completionStage = Path.Combine(root, "completion-exhaustion")
        Using workspace = ReservedWriteBudget.Create(completionStage, 4096, {original})
            Dim nearFull(CInt(workspace.RemainingBytes - 500) - 1) As Byte
            Dim nearHash = Convert.ToHexString(SHA256.HashData(nearFull))
            reject(Sub() workspace.WriteStream("full.bin", nearFull.LongLength, nearHash, Sub(output) output.Write(nearFull)), "completion event exhaustion fails closed after charged payload")
            check(File.Exists(Path.Combine(completionStage, "full.bin")) AndAlso workspace.VerifiedNames.Length = 0 AndAlso workspace.RemainingBytes >= 0, "matching full bytes without durable completion are not a checkpoint")
        End Using
        Using workspace = ReservedWriteBudget.Recover(completionStage)
            check(workspace.ReservedNames.Length = 1 AndAlso workspace.VerifiedNames.Length = 0, "completion exhaustion recovery retains full charge without certifying output")
        End Using
        Using workspace = ReservedWriteBudget.Create(Path.Combine(root, "resource-names"), 8192, {original})
            workspace.WriteNew("Data/FaceGeom/Mod adultas (versión 2).esp/head.nif", payload)
            check(workspace.VerifiedNames.Length = 1, "realistic spaced Unicode plugin resource path admitted")
            reject(Sub() workspace.WriteNew("Data/CON.dds", payload), "Windows device name cannot bypass resource reservation")
            reject(Sub() workspace.WriteNew("Data/head.nif.", payload), "Windows trailing dot alias rejected")
            reject(Sub() workspace.WriteNew("Data/FaceGeom /head.nif", payload), "Windows trailing directory space alias rejected")
        End Using
        check(File.ReadAllText(Path.Combine(original, "game.txt")) = "unchanged", "original unchanged")
        File.AppendAllText(Path.Combine(stage, "budget-ledger.jsonl"), "interrupted reservation")
        reject(Sub() ReservedWriteBudget.Recover(stage), "torn ledger fails closed without refund")
        Return checks
    End Function
End Module
