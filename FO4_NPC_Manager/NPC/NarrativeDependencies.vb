Option Strict On
Imports System.IO
Imports System.Linq
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    Public Class NarrativeEdge
        Public Property Source As String = ""
        Public Property Target As String = ""
        Public Property Signature As String = ""
        Public Property Field As String = ""
        Public Property TargetOutsideScannedView As Boolean
        Public Property TargetDeleted As Boolean
    End Class
    Public Class NpcNarrativeImpact
        Public Property Key As String = ""
        Public Property Category As String = ""
        Public Property ReviewRequired As Boolean
        Public Property Upstream As New List(Of String)
        Public Property Voice As String = ""
    End Class
    Public Class NarrativeReport
        Public Property Status As String = "EXPERIMENTAL-STATIC-IMPACT-NOT-QUEST-COMPATIBILITY"
        Public Property InputSha256 As String = ""
        Public Property RecordsParsed As Integer
        Public Property Edges As New List(Of NarrativeEdge)
        Public Property Findings As New List(Of String)
        Public Property Npcs As New List(Of NpcNarrativeImpact)
        Public Property Unvalidated As String() = {"runtime aliases and conditions", "Papyrus dynamic lookups", "dialogue/audio delivery", "quest/scene execution", "collection gameplay", "references outside scanned signatures are not proven missing"}
    End Class
    Public Module NarrativeDependencies
        Private ReadOnly signatures As New HashSet(Of String)(StringComparer.Ordinal) From {"NPC_", "QUST", "SCEN", "RELA", "VTYP", "FACT", "PACK", "INFO", "DIAL", "ACHR", "REFR", "CELL", "WRLD", "FLST", "RACE"}
        Public Function Analyze(input As CampaignInput, Optional maxEdges As Integer = 100000) As NarrativeReport
            If maxEdges < 1 OrElse maxEdges > 1000000 Then Throw New ArgumentOutOfRangeException(NameOf(maxEdges))
            Dim plan = GlobalNpcDiscovery.Scan(input)
            Dim prior = Config_App.Current.Game
            Try
                Config_App.Current.Game = Config_App.Game_Enum.Fallout4
                Using resources = LocalizedExportScope.Open(input.Localization, input.LoadOrder)
                    Dim pm As New PluginManager()
                    pm.LoadAllPlugins(input.DataRoot, input.LoadOrder, sigFilter:=signatures)
                    If input.LoadOrder.Any(Function(n) Not pm.IsLoaded(n)) Then Throw New InvalidDataException("Incomplete narrative load order.")
                    If pm.AllRecords.Count > input.Policy.MaxRecords * 10L Then Throw New IOException("Narrative record budget exceeded.")
                    Dim report As New NarrativeReport With {.InputSha256 = plan.InputSha256}
                    For Each raw In pm.AllRecords.Values.OrderBy(Function(r) r.Header.FormID)
                        If raw.Header.IsDeleted OrElse Not signatures.Contains(raw.Header.Signature) Then Continue For
                        Dim source = Identity(pm, raw.Header.FormID)
                        Try
                            Dim context As Canon.WbContext = Nothing
                            Dim tree = Canon.CanonBridge.Tree(raw, pm, context)
                            If tree Is Nothing Then Throw New InvalidDataException("No original canonical schema.")
                            If context IsNot Nothing Then
                                For Each finding In context.Findings
                                    report.Findings.Add(source & ": " & finding.Kind.ToString() & ": " & finding.Path & ": " & finding.Message)
                                Next
                            End If
                            report.RecordsParsed += 1
                            For Each node In Canon.WbFormIdWalker.Enumerate(tree)
                                Dim fid As UInteger = 0UI
                                ' Walker values already live in global FormID space. No raw-byte searching.
                                If node.Value IsNot Nothing Then fid = CUInt(Convert.ToInt64(node.Value) And &HFFFFFFFFL)
                                If fid = 0UI Then Continue For
                                If report.Edges.Count >= maxEdges Then Throw New IOException("Narrative edge budget exceeded.")
                                Dim target = pm.GetRecord(fid)
                                report.Edges.Add(New NarrativeEdge With {.Source = source, .Target = Identity(pm, fid), .Signature = raw.Header.Signature,
                                    .Field = node.Path, .TargetOutsideScannedView = target Is Nothing, .TargetDeleted = target IsNot Nothing AndAlso target.Header.IsDeleted})
                            Next
                        Catch ex As IOException
                            Throw
                        Catch ex As Exception
                            report.Findings.Add(source & ": " & ex.GetType().Name & ": " & ex.Message)
                        End Try
                    Next
                    Dim reverse = report.Edges.GroupBy(Function(e) e.Target).ToDictionary(Function(g) g.Key, Function(g) g.Select(Function(e) e.Source).Distinct().OrderBy(Function(k) k, StringComparer.Ordinal).ToList(), StringComparer.Ordinal)
                    Dim totalImpact As Integer = 0
                    For Each decision In plan.Decisions
                        Dim impact As New NpcNarrativeImpact With {.Key = decision.Npc.Key, .Category = decision.Category,
                            .ReviewRequired = decision.Category <> "generic-human" OrElse decision.Npc.HasScripts OrElse decision.Npc.Unique OrElse decision.Npc.Essential}
                        ' Verify actual category labels through the existing plan; incoming narrative links always require review.
                        Dim seen As New HashSet(Of String)(StringComparer.Ordinal) From {impact.Key}
                        Dim queue As New Queue(Of String)() : queue.Enqueue(impact.Key)
                        While queue.Count <> 0
                            Dim key = queue.Dequeue()
                            If Not reverse.ContainsKey(key) Then Continue While
                            For Each ancestor In reverse(key)
                                If Not seen.Add(ancestor) Then Continue For
                                totalImpact += 1
                                If totalImpact > maxEdges Then Throw New IOException("Narrative transitive impact budget exceeded.")
                                impact.Upstream.Add(ancestor) : queue.Enqueue(ancestor)
                            Next
                        End While
                        impact.Upstream.Sort(StringComparer.Ordinal)
                        If impact.Upstream.Count <> 0 OrElse report.Findings.Count <> 0 Then impact.ReviewRequired = True
                        Dim rawNpc = pm.GetRecord(pm.GlobalFormIDFromObjectID(decision.Npc.Origin, decision.Npc.ObjectId))
                        If rawNpc IsNot Nothing AndAlso Not rawNpc.Header.IsDeleted Then
                            Try
                                Dim npc = StrictTraitsMaterialization.Materialize(RecordParsers.ParseNPC(rawNpc, pm), pm)
                                If npc.Record.VoicePresente AndAlso npc.Record.Voice <> 0UI Then impact.Voice = Identity(pm, npc.Record.Voice)
                            Catch ex As Exception
                                report.Findings.Add(impact.Key & ": effective voice unavailable: " & ex.Message)
                                impact.ReviewRequired = True
                            End Try
                        End If
                        report.Npcs.Add(impact)
                    Next
                    resources.VerifyUnchanged()
                    If GlobalNpcDiscovery.Scan(input).InputSha256 <> plan.InputSha256 Then Throw New IOException("Narrative input changed during analysis.")
                    Return report
                End Using
            Finally
                Config_App.Current.Game = prior
            End Try
        End Function
        Private Function Identity(pm As PluginManager, fid As UInteger) As String
            Dim owner = pm.GetOriginatingPluginName(fid)
            If String.IsNullOrWhiteSpace(owner) Then Return "UNRESOLVED|" & fid.ToString("X8")
            Return owner.ToUpperInvariant() & "|" & PluginManager.ToFaceGenLocalFormID(fid).ToString("X6")
        End Function
    End Module
End Namespace
