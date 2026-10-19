Imports System.IO
Imports System.Linq
Imports System.Text
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
        check(File.ReadAllText(Path.Combine(original, "game.txt")) = "unchanged", "original unchanged")
        File.AppendAllText(Path.Combine(stage, "budget-ledger.jsonl"), "interrupted reservation")
        reject(Sub() ReservedWriteBudget.Recover(stage), "torn ledger fails closed without refund")
        Return checks
    End Function
End Module
