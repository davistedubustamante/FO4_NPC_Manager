Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Security.Cryptography
Imports System.Text

Namespace ExperimentalTransformation
    Public Class CampaignPolicy
        Public Property HumanRaces As New List(Of String)
        Public Property RobotRaces As New List(Of String)
        Public Property CreatureRaces As New List(Of String)
        ' Reviewed per-record evidence, keyed by originating plugin|local object ID.
        Public Property AdultEvidence As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Public Property ChildKeys As New List(Of String)
        Public Property CompanionFactions As New List(Of String)
        Public Property CompanionKeys As New List(Of String)
        Public Property SpecialKeys As New List(Of String)
        Public Property FemalePresets As New List(Of String)
        Public Property BatchSize As Integer = 10
        Public Property MaxRecords As Integer = 100000
        Public Property MaxReportBytes As Long = 16L * 1024 * 1024
        Public Property MaxPluginBytes As Long = 4L * 1024 * 1024 * 1024
    End Class
    Public Class CampaignInput
        Public Property DataRoot As String = ""
        Public Property LoadOrder As New List(Of String)
        Public Property ProtectedRoots As New List(Of String)
        Public Property Policy As New CampaignPolicy
    End Class
    Public Class DiscoveredNpc
        Public Property Key As String = ""
        Public Property Origin As String = ""
        Public Property ObjectId As UInteger
        Public Property Winner As String = ""
        Public Property OverrideChain As New List(Of String)
        Public Property RaceKey As String = ""
        Public Property RaceOrigin As String = ""
        Public Property RaceObjectId As UInteger
        Public Property Unique As Boolean
        Public Property Essential As Boolean
        Public Property Female As Boolean
        Public Property RaceFlagsKnown As Boolean
        Public Property ChildRace As Boolean
        Public Property Deleted As Boolean
        Public Property TemplateFlags As UShort
        Public Property Factions As New List(Of String)
        Public Property HasScripts As Boolean
        Public Property ErrorDetail As String = ""
    End Class
    Public Class CampaignDecision
        Public Property Npc As DiscoveredNpc
        Public Property Category As String = "unknown"
        Public Property Route As String = "blocked"
        Public Property Reason As String = ""
        Public Property Preset As String = ""
        Public Property AdultEvidence As String = ""
    End Class
    Public Class CampaignPlan
        Public Property Version As String = "global-plan-v2-admission"
        Public Property Status As String = "EXPERIMENTAL-PLAN-NOT-CONVERTED"
        Public Property InputSha256 As String = ""
        Public Property PlanSha256 As String = ""
        Public Property BatchSize As Integer
        Public Property ExecutionBlockers As New List(Of String)
        Public Property Decisions As New List(Of CampaignDecision)
        Public Property Pages As New List(Of List(Of String))
        Public Property Coverage As New SortedDictionary(Of String, Integer)(StringComparer.Ordinal)
    End Class
    Public Module GlobalCampaign
        Public Function Build(inventory As IEnumerable(Of DiscoveredNpc), policy As CampaignPolicy, inputSha256 As String, Optional executionBlockers As IEnumerable(Of String) = Nothing) As CampaignPlan
            If policy Is Nothing OrElse policy.BatchSize < 1 OrElse policy.BatchSize > 25 OrElse policy.MaxRecords < 1 OrElse policy.MaxRecords > 100000 OrElse policy.MaxReportBytes < 1 OrElse policy.MaxReportBytes > 64L * 1024 * 1024 OrElse policy.MaxPluginBytes < 1 OrElse policy.MaxPluginBytes > 8L * 1024 * 1024 * 1024 OrElse policy.FemalePresets Is Nothing OrElse policy.FemalePresets.Count > 256 OrElse policy.AdultEvidence Is Nothing OrElse policy.AdultEvidence.Count > policy.MaxRecords Then Throw New ArgumentException("Invalid campaign resource limits.")
            Dim records = inventory.Take(policy.MaxRecords + 1).ToList()
            If records.Count > policy.MaxRecords OrElse records.Any(Function(n) n Is Nothing OrElse String.IsNullOrWhiteSpace(n.Key)) OrElse records.Select(Function(n) n.Key.ToUpperInvariant()).Distinct().Count() <> records.Count Then Throw New InvalidDataException("Inventory limit, null entry or duplicate identity.")
            Dim humans = Keys(policy.HumanRaces), robots = Keys(policy.RobotRaces), creatures = Keys(policy.CreatureRaces)
            If humans.Overlaps(robots) OrElse humans.Overlaps(creatures) OrElse robots.Overlaps(creatures) Then Throw New ArgumentException("Race classifications overlap.")
            Dim children = Keys(policy.ChildKeys), companions = Keys(policy.CompanionKeys), specials = Keys(policy.SpecialKeys)
            Dim companionFactions = Keys(policy.CompanionFactions)
            Dim adults As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each pair In policy.AdultEvidence
                If Not adults.TryAdd(pair.Key, pair.Value) Then Throw New ArgumentException("Duplicate adult evidence identity.")
            Next
            Dim presets = policy.FemalePresets.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(p) p, StringComparer.Ordinal).ToArray()
            Dim plan As New CampaignPlan With {.InputSha256 = inputSha256, .BatchSize = policy.BatchSize, .ExecutionBlockers = If(executionBlockers, Array.Empty(Of String)()).Distinct().OrderBy(Function(x) x, StringComparer.Ordinal).ToList()}
            For Each npc In records.OrderBy(Function(n) n.Key, StringComparer.OrdinalIgnoreCase)
                Dim d As New CampaignDecision With {.Npc = npc}
                plan.Decisions.Add(d)
                If npc.ChildRace OrElse children.Contains(npc.Key) Then
                    d.Category = "child" : d.Reason = "adult-redesign-not-verified"
                ElseIf npc.Deleted Then
                    d.Reason = "deleted-winning-record"
                ElseIf Not String.IsNullOrEmpty(npc.ErrorDetail) Then
                    d.Reason = "unreadable-record"
                ElseIf robots.Contains(npc.RaceKey) Then
                    d.Category = "robot" : d.Route = "robot-redesign-pending" : d.Reason = "requires-rig-voice-behavior-validation"
                ElseIf creatures.Contains(npc.RaceKey) Then
                    d.Category = "creature" : d.Route = "creature-redesign-pending" : d.Reason = "requires-species-rig-gameplay-validation"
                ElseIf Not humans.Contains(npc.RaceKey) Then
                    d.Reason = "unclassified-race"
                Else
                    d.Category = If(companions.Contains(npc.Key) OrElse npc.Factions.Any(Function(f) companionFactions.Contains(f)), "companion", If(specials.Contains(npc.Key) OrElse npc.Essential OrElse npc.HasScripts, "special-human", If(npc.Unique, "unique-human", "generic-human")))
                    Dim evidence As String = Nothing
                    If Not npc.RaceFlagsKnown Then
                        d.Reason = "race-age-flags-unresolved"
                    ElseIf Not adults.TryGetValue(npc.Key, evidence) OrElse String.IsNullOrWhiteSpace(evidence) Then
                        d.Reason = "adulthood-unknown"
                    ElseIf npc.TemplateFlags <> 0US Then
                        d.Reason = "inherited-categories-unresolved"
                    ElseIf npc.Female Then
                        d.Reason = "already-female-not-integrally-validated"
                    ElseIf d.Category <> "generic-human" Then
                        d.Route = "individual-regression-review" : d.Reason = "quests-scenes-relationships-voices"
                    ElseIf presets.Length = 0 Then
                        d.Reason = "female-preset-pool-required"
                    Else
                        d.Route = "adult-human-production-pipeline" : d.Reason = "planned-not-executed" : d.AdultEvidence = evidence
                        Dim hash = SHA256.HashData(Encoding.UTF8.GetBytes(npc.Key.ToUpperInvariant()))
                        Dim index = CInt((CUInt(hash(0)) * 256UI + CUInt(hash(1))) Mod CUInt(presets.Length))
                        d.Preset = presets(index)
                    End If
                End If
                Dim counter = d.Category & "/" & d.Route & "/" & d.Reason
                If Not plan.Coverage.ContainsKey(counter) Then plan.Coverage(counter) = 0
                plan.Coverage(counter) += 1
            Next
            Dim eligible = plan.Decisions.Where(Function(d) d.Route = "adult-human-production-pipeline").Select(Function(d) d.Npc.Key).ToArray()
            For offset = 0 To eligible.Length - 1 Step policy.BatchSize
                plan.Pages.Add(eligible.Skip(offset).Take(policy.BatchSize).ToList())
            Next
            plan.PlanSha256 = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(plan)))
            Return plan
        End Function
        Friend Function CreateBatch(input As CampaignInput, plan As CampaignPlan, pageIndex As Integer) As NpcBatchProfile
            If plan.ExecutionBlockers.Contains("localized-load-order-export-not-integrated") Then Throw New InvalidOperationException("Localized masters need verified string/export integration before preparing an executable page.")
            If pageIndex < 0 OrElse pageIndex >= plan.Pages.Count Then Throw New ArgumentOutOfRangeException(NameOf(pageIndex))
            Dim result As New NpcBatchProfile With {.DataRoot = input.DataRoot, .LoadOrder = input.LoadOrder.ToList(), .ProtectedRoots = input.ProtectedRoots.ToList(), .RequireFaceGen = True}
            For Each key In plan.Pages(pageIndex)
                Dim d = plan.Decisions.Single(Function(x) x.Npc.Key = key)
                If d.Route <> "adult-human-production-pipeline" Then Throw New InvalidDataException("Page contains ineligible target.")
                result.Targets.Add(New NpcBatchTarget With {.SourcePlugin = d.Npc.Origin, .ObjectId = d.Npc.ObjectId, .Age = AgeEvidence.Adult, .AdultEvidence = d.AdultEvidence,
                    .AppearancePreset = d.Preset, .HumanRacePlugin = d.Npc.RaceOrigin, .HumanRaceObjectId = d.Npc.RaceObjectId})
            Next
            Return result
        End Function
        Private Function Keys(values As IEnumerable(Of String)) As HashSet(Of String)
            If values Is Nothing Then Throw New ArgumentException("Null policy list.")
            Return New HashSet(Of String)(values, StringComparer.OrdinalIgnoreCase)
        End Function
    End Module
End Namespace
