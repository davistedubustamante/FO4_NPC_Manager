Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports FO4_NPC_Manager.ExperimentalTransformation

Module Program
    Private checks As Integer

    Private Sub Check(condition As Boolean, name As String)
        If Not condition Then Throw New Exception("FAIL: " & name)
        checks += 1
        Console.WriteLine("PASS: " & name)
    End Sub

    Private Sub Reject(action As Action, name As String)
        Dim rejected = False
        Try
            action()
        Catch ex As ArgumentException
            rejected = True
        End Try
        Check(rejected, name)
    End Sub

    Private Function Npc(key As String) As SyntheticNpc
        Return New SyntheticNpc With {
            .Key = key, .RaceKey = "Synthetic.esm|000001", .Age = AgeEvidence.Adult,
            .TraitsResolved = True, .Flags = &H800000A2UI, .Appearance = "original",
            .Voice = "unique-quest-voice", .Equipment = New List(Of String) From {"quest-outfit"},
            .Preserved = New Dictionary(Of String, String) From {
                {"VMAD", "opaque-script-bytes"}, {"factions", "opaque-factions"},
                {"packages", "opaque-packages"}, {"identity", key}}}
    End Function

    Function Main(args As String()) As Integer
        Try
            If args.Length <> 1 Then Throw New ArgumentException("Usage: AdultTransformationGate <new-report.json>")
            Dim profile As New SyntheticProfile With {
                .Id = "synthetic-adult-female", .RaceKey = "Synthetic.esm|000001",
                .Age = AgeEvidence.Adult, .Female = True, .Appearance = "female-head-fixture", .Body = "female-body-fixture"}
            Dim male = Npc("Synthetic.esm|000101")
            Dim child = Npc("Synthetic.esm|000102") : child.Age = AgeEvidence.Child
            Dim unknown = Npc("Synthetic.esm|000103") : unknown.Age = AgeEvidence.Unknown
            Dim inherited = Npc("Synthetic.esm|000104") : inherited.TraitsResolved = False
            Dim otherRace = Npc("Synthetic.esm|000105") : otherRace.RaceKey = "other-race"
            Dim female = Npc("Synthetic.esm|000106") : female.Flags = female.Flags Or 1UI
            Dim batch = TransformSynthetic({male, child, unknown, inherited, otherRace, female}, profile)
            Check(Not batch.CanWriteGamePatch AndAlso batch.PendingValidation.Contains("facegen") AndAlso
                  batch.Status = "EXPERIMENTAL-SYNTHETIC-NOT-A-GAME-PATCH", "synthetic success cannot authorize game patch output")
            Check(batch.Changes.Count = 6 AndAlso batch.Changes.Where(Function(c) c.Applied).Count() = 1, "one eligible adult in mixed batch")
            Dim transformed = batch.Changes(0).Transformed
            Check(transformed.Flags = (male.Flags Or 1UI), "only female bit changes, including high flags")
            Check((male.Flags And 1UI) = 0UI AndAlso male.Appearance = "original", "source snapshot unchanged")
            Check(transformed.Key = male.Key AndAlso transformed.RaceKey = male.RaceKey, "identity and race preserved")
            Check(transformed.Voice = male.Voice, "quest voice preserved, no automatic voice substitution")
            Check(transformed.Equipment.SequenceEqual(male.Equipment), "equipment preserved")
            Check(transformed.Preserved.OrderBy(Function(p) p.Key).SequenceEqual(male.Preserved.OrderBy(Function(p) p.Key)), "opaque script, faction and package data preserved")
            Check(transformed.Appearance = profile.Appearance AndAlso batch.Changes(0).Body = profile.Body, "explicit synthetic appearance and body selected")
            Check(batch.Changes(1).Reason = "adult-evidence-required", "child excluded")
            Check(batch.Changes(2).Reason = "adult-evidence-required", "unknown age excluded")
            Check(batch.Changes(3).Reason = "unresolved-template-traits", "inherited traits fail closed")
            Check(batch.Changes(4).Reason = "race-profile-mismatch", "incompatible race excluded")
            Check(batch.Changes(5).Reason = "already-female", "female excluded")
            Check(Not TransformSynthetic({transformed}, profile).Changes(0).Applied, "repeat application is a no-op")
            transformed.Equipment.Add("new") : transformed.Preserved("VMAD") = "changed"
            Check(male.Equipment.Count = 1 AndAlso male.Preserved("VMAD") = "opaque-script-bytes", "result owns mutable collections")
            Reject(Sub() TransformSynthetic({}, profile), "empty batch rejected")
            Reject(Sub() TransformSynthetic(Enumerable.Range(1, 11).Select(Function(i) Npc(i.ToString())), profile), "batch limit enforced")
            Reject(Sub() TransformSynthetic({male, male}, profile), "duplicate identity rejected")
            Reject(Sub() TransformSynthetic({male, Npc(male.Key.ToUpperInvariant())}, profile), "case-insensitive duplicate rejected")
            Reject(Sub() TransformSynthetic({Nothing}, profile), "null snapshot rejected")
            Reject(Sub() TransformSynthetic({Npc(" ")}, profile), "blank identity rejected")
            Dim incomplete = Npc("bad") : incomplete.Preserved = Nothing
            Reject(Sub() TransformSynthetic({incomplete}, profile), "missing preserved data rejected")
            profile.Age = AgeEvidence.Unknown
            Reject(Sub() TransformSynthetic({male}, profile), "unknown-age target rejected")
            profile.Age = AgeEvidence.Child
            Reject(Sub() TransformSynthetic({male}, profile), "child target rejected")
            profile.Age = AgeEvidence.Adult : profile.Female = False
            Reject(Sub() TransformSynthetic({male}, profile), "male target rejected")
            profile.Female = True : profile.Body = ""
            Reject(Sub() TransformSynthetic({male}, profile), "missing body rejected")
            profile.Body = "female-body-fixture"
            Check(TransformSynthetic(Enumerable.Range(1, 10).Select(Function(i) Npc(i.ToString())), profile).Changes.All(Function(c) c.Applied), "maximum supported batch transforms all eligible NPCs")
            Dim cleanFlags = Npc("clean") : cleanFlags.Flags = 0UI
            Check(TransformSynthetic({cleanFlags}, profile).Changes(0).Transformed.Flags = 1UI, "no unrelated flags introduced")
            Dim options As New JsonSerializerOptions With {.WriteIndented = True}
            Dim first = JsonSerializer.Serialize(TransformSynthetic({male, child}, profile), options)
            Dim second = JsonSerializer.Serialize(TransformSynthetic({male, child}, profile), options)
            Check(first = second, "identical ordered inputs produce deterministic JSON")
            Dim restored = JsonSerializer.Deserialize(Of SyntheticBatch)(first)
            Check(restored.Changes(0).Transformed.Preserved("VMAD") = male.Preserved("VMAD") AndAlso
                  restored.Changes(1).Transformed Is Nothing, "report round-trip preserves data and rejected status")
            Dim quotaChecks = ReservedWriteBudgetTests.Run()
            Console.WriteLine($"{quotaChecks} controlled write budget assertions passed")
            Dim campaignChecks = GlobalCampaignTests.Run()
            Console.WriteLine($"{campaignChecks} global campaign assertions passed")
            Dim recoveryChecks = BatchJournalTests.Run()
            Console.WriteLine($"{recoveryChecks} batch journal assertions passed")
            Dim controlledChecks = ControlledFileOutputTests.Run()
            Console.WriteLine($"{controlledChecks} controlled writer assertions passed")
            Dim report = TransformSynthetic({male, child, unknown, inherited, otherRace, female}, profile)
            Dim json = JsonSerializer.Serialize(New With {.schemaVersion = 1, .checksPassed = checks, .campaignChecksPassed = campaignChecks, .quotaChecksPassed = quotaChecks, .controlledWriterChecksPassed = controlledChecks, .recoveryChecksPassed = recoveryChecks, .batch = report}, New JsonSerializerOptions With {.WriteIndented = True})
            ' Never overwrite an existing report, and never write a plugin or game asset.
            Using output As New FileStream(args(0), FileMode.CreateNew, FileAccess.Write)
                Using writer As New StreamWriter(output)
                    writer.WriteLine(json)
                End Using
            End Using
            Console.WriteLine($"{checks} checks passed; EXPERIMENTAL synthetic report: {args(0)}")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.Message)
            Return 1
        End Try
    End Function
End Module
