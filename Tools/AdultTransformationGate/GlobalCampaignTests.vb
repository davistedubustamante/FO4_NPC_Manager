Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports FO4_NPC_Manager.ExperimentalTransformation
Module GlobalCampaignTests
    Function Run() As Integer
        Dim checks = 0
        Dim policy As New CampaignPolicy With {.HumanRaces = New List(Of String) From {"BASE.ESM|000801"}, .RobotRaces = New List(Of String) From {"ROBOT|000001"}, .CreatureRaces = New List(Of String) From {"CREATURE|000001"}, .FemalePresets = New List(Of String) From {"b.json", "a.json"}}
        Dim all = Enumerable.Range(1, 10000).Select(Function(i) New DiscoveredNpc With {.Key = "BASE.ESM|" & i.ToString("X6"), .Origin = "Base.esm", .ObjectId = CUInt(i), .RaceKey = "BASE.ESM|000801", .RaceOrigin = "Base.esm", .RaceObjectId = &H801UI, .RaceFlagsKnown = True}).ToList()
        For Each n In all
            policy.AdultEvidence(n.Key) = "reviewed test evidence"
        Next
        all(0).ChildRace = True
        all(1).Unique = True
        policy.CompanionKeys.Add(all(2).Key)
        all(3).RaceKey = "ROBOT|000001"
        all(4).RaceKey = "CREATURE|000001"
        policy.AdultEvidence.Remove(all(5).Key)
        all(6).TemplateFlags = 1US
        all(7).Female = True
        all(8).HasScripts = True
        all(9).RaceFlagsKnown = False
        all(10).Deleted = True
        all(11).ErrorDetail = "corrupt record"
        all(12).RaceKey = "unknown"
        Dim plan = GlobalCampaign.Build(all, policy, "input-hash")
        Check(plan.Decisions.Count = 10000 AndAlso plan.Pages.Sum(Function(p) p.Count) = 9987, checks)
        Check(plan.Pages.All(Function(p) p.Count <= 10) AndAlso plan.Coverage.Values.Sum() = 10000, checks)
        Check(plan.Decisions(0).Category = "child" AndAlso plan.Decisions(0).Route = "blocked", checks)
        Check(plan.Decisions(1).Route = "individual-regression-review" AndAlso plan.Decisions(2).Category = "companion", checks)
        Check(plan.Decisions(3).Route = "robot-redesign-pending" AndAlso plan.Decisions(4).Route = "creature-redesign-pending", checks)
        Check(plan.Decisions(5).Reason = "adulthood-unknown", checks)
        Check(plan.Decisions(6).Reason = "inherited-categories-unresolved", checks)
        Check(plan.Decisions(7).Reason = "already-female-not-integrally-validated", checks)
        Check(plan.Decisions(8).Category = "special-human" AndAlso plan.Decisions(9).Reason = "race-age-flags-unresolved", checks)
        Check(plan.Decisions(10).Reason = "deleted-winning-record" AndAlso plan.Decisions(11).Reason = "unreadable-record", checks)
        Check(plan.Decisions(12).Reason = "unclassified-race", checks)
        Dim again = GlobalCampaign.Build(all.AsEnumerable().Reverse(), policy, "input-hash")
        Check(JsonSerializer.Serialize(plan) = JsonSerializer.Serialize(again), checks)
        policy.FemalePresets.Reverse()
        Check(GlobalCampaign.Build(all, policy, "input-hash").PlanSha256 = plan.PlanSha256, checks)
        Dim batch = GlobalCampaign.CreateBatch(New CampaignInput With {.DataRoot = "data", .LoadOrder = New List(Of String) From {"Base.esm"}, .ProtectedRoots = New List(Of String) From {"data"}}, plan, 0)
        Check(batch.Targets.Count = 10 AndAlso batch.RequireFaceGen AndAlso batch.Targets.All(Function(t) t.Age = AgeEvidence.Adult), checks)
        policy.MaxRecords = 100
        Reject(Sub() GlobalCampaign.Build(Array.Empty(Of DiscoveredNpc)(), policy, "hash"), checks)
        Dim savedEvidence = policy.AdultEvidence
        policy.AdultEvidence = New Dictionary(Of String, String)
        Reject(Sub() GlobalCampaign.Build(all, policy, "hash"), checks)
        policy.AdultEvidence = savedEvidence
        policy.MaxRecords = 100000 : policy.BatchSize = 26
        Reject(Sub() GlobalCampaign.Build(all, policy, "hash"), checks)
        policy.BatchSize = 10 : policy.RobotRaces.Add("base.esm|000801")
        Reject(Sub() GlobalCampaign.Build(all, policy, "hash"), checks)
        policy.RobotRaces.RemoveAt(policy.RobotRaces.Count - 1)
        Reject(Sub() GlobalCampaign.Build({all(0), all(0)}, policy, "hash"), checks)
        policy.CompanionFactions.Add("Companions|000123")
        all(13).Factions.Add("companions|000123")
        Check(GlobalCampaign.Build(all, policy, "hash").Decisions(13).Category = "companion", checks)
        Dim localized = GlobalCampaign.Build(all, policy, "hash", {"localized-load-order-export-not-integrated"})
        Reject(Sub() GlobalCampaign.CreateBatch(New CampaignInput, localized, 0), checks)
        policy.FemalePresets.Clear()
        Check(GlobalCampaign.Build(all, policy, "hash").Pages.Count = 0, checks)
        policy.FemalePresets = Enumerable.Range(0, 257).Select(Function(i) i.ToString() & ".json").ToList()
        Reject(Sub() GlobalCampaign.Build(Array.Empty(Of DiscoveredNpc)(), policy, "hash"), checks)
        Return checks
    End Function
    Private Sub Check(ok As Boolean, ByRef checks As Integer)
        If Not ok Then Throw New Exception("Global campaign assertion " & (checks + 1).ToString())
        checks += 1
    End Sub
    Private Sub Reject(action As Action, ByRef checks As Integer)
        Try
            action()
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidDataException OrElse TypeOf ex Is InvalidOperationException
            checks += 1 : Return
        End Try
        Throw New Exception("Unsafe campaign accepted")
    End Sub
End Module
